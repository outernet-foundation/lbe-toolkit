using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using FofX.Stateful;
using ObserveThing;
using SimpleJSON;

using UnityEngine;

namespace Outernet.LBEToolkit.StateSynchronization
{
    public class DoNotSync : Attribute, IInheritableStateAttribute
    {
        public bool inherit { get; } = true;
    }

    public class PeerToPeerStateSyncManager<T> : IStateSyncManager where T : class, IStateNode, new()
    {
        private readonly byte INITIAL_SYNC_EVENT = 1;
        private readonly byte INCREMENTAL_SYNC_EVENT = 2;
        private readonly byte HIGH_FREQUENCY_SYNC_EVENT = 3;
        private readonly byte HIGH_FREQUENCY_ADD_ID_EVENT = 4;
        private readonly byte HIGH_FREQUENCY_REMOVE_ID_EVENT = 5;
        private readonly byte PLAYER_SYNCHRONIZED_EVENT = 6;

        public T synchronizationTarget { get; }
        public string topic { get; }
        public bool synchronized { get; private set; }

        public event Action onInitialSynchronizationComplete;

        private IRealtimeClient _client;

        private MemoryStream _initSyncStream = new MemoryStream();
        private MemoryStream _incrementalSyncStream = new MemoryStream();
        private MemoryStream _highFrequencySyncStream = new MemoryStream();
        private PathIDCache<T> _pathIDCache = new PathIDCache<T>();
        private List<int> _synchronizedPlayers = new List<int>();
        private bool _applyingRemoteChanges = false;

        private uint _nextLocalFrequencyPrimitiveId = 0;
        private Dictionary<string, (int ownerId, uint primitiveId)> _localHighFrequencyPrimitives = new Dictionary<string, (int ownerId, uint primitiveId)>();
        private Dictionary<(int ownerId, uint primitiveId), HighFrequencyPrimitiveData> _highFrequencyPrimitives = new();
        private HashSet<HighFrequencyPrimitiveData> _dirtyHighFrequencyPrimitives = new();
        private HashSet<HighFrequencyPrimitiveData> _syncedHighFrequencyPrimitives = new();

        private IDisposable _subscriptions;

        private class HighFrequencyPrimitiveData
        {
            public int ownerId;
            public uint primitiveId;
            public IStateValue target;
            public Serializer serializer;
            public IDisposable subscription;
            public byte syncRate;
            public float lastSyncTime;
        }

        public PeerToPeerStateSyncManager(T synchronizationTarget, IRealtimeClient client, string topic = default)
        {
            this.synchronizationTarget = synchronizationTarget;
            this.topic = topic;

            _client = client;
            _client.onEventReceived += HandleEventReceived;
            _client.onJoinedRoom += HandleJoinedRoom;
            _client.onHostChanged += HandleHostChanged;
            _client.onPlayerEnteredRoom += HandlePlayerJoined;
            _client.onPlayerLeftRoom += HandlePlayerLeft;

            _subscriptions = new ComposedDisposable(

                synchronizationTarget
                    .ObservableChildrenRecursive()
                    .ObservableWhere(x => !ShouldSync(x))
                    .ObservableDistinct()
                    .ObservableCombine()
                    .Subscribe(HandleSceneChanged)

            );

            _incrementalSyncStream.WriteByte(INCREMENTAL_SYNC_EVENT);
            _highFrequencySyncStream.WriteByte(HIGH_FREQUENCY_SYNC_EVENT);

            if (_client.connected)
                HandleJoinedRoom();
        }

        private void HandlePlayerJoined(int playerId)
        {
            SendInitialSyncToPendingPlayers(playerId);
        }

        private void HandlePlayerLeft(int playerId)
        {
            _synchronizedPlayers.Remove(playerId);

            foreach (var toRemove in _highFrequencyPrimitives.Where(primitive => primitive.Key.ownerId == playerId).ToArray())
            {
                toRemove.Value.subscription.Dispose();
                _highFrequencyPrimitives.Remove(toRemove.Key);
            }
        }

        private void HandleHostChanged(int hostId)
        {
            if (_client.playerId == hostId && _client.connected)
                SendInitialSyncToPendingPlayers(_client.players.Except(_synchronizedPlayers).Where(x => x != _client.playerId).ToArray());
        }

        private void HandleJoinedRoom()
        {
            if (_client.playerId == _client.hostId)
            {
                synchronized = true;
                onInitialSynchronizationComplete?.Invoke();

                SendInitialSyncToPendingPlayers(_client.players.Except(_synchronizedPlayers).Where(x => x != _client.playerId).ToArray());
            }
        }

        private void HandleSceneChanged(StateOperation op)
        {
            if (_applyingRemoteChanges)
                return;

            if (!_client.connected)
                return;

            if (op.opType == OpType.None || op.source.isView)
                return;

            if (op.source is IStateValue valueSource &&
                _localHighFrequencyPrimitives.ContainsKey(valueSource.nodePath))
            {
                return;
            }

            WriteChange(_incrementalSyncStream, op);
        }

        private void SendInitialSyncToPendingPlayers(params int[] targets)
        {
            if (targets.Length == 0)
                return;

            using (var writer = new BinaryWriter(_initSyncStream, Encoding.UTF8, true))
            {
                writer.Write(INITIAL_SYNC_EVENT);
                writer.Write(synchronizationTarget.ToJSON(x => !x.isView && !ShouldSync(x)).ToString());

                foreach (var highFrequencyPrimitive in _highFrequencyPrimitives.Values)
                {
                    writer.Write(highFrequencyPrimitive.ownerId);
                    writer.Write(highFrequencyPrimitive.primitiveId);
                    writer.Write(highFrequencyPrimitive.target.nodePath);
                }
            }

            _client.SendEvent(_initSyncStream.ToArray(), true, targets, topic);
            _initSyncStream.SetLength(0);
        }

        private void WriteChange(MemoryStream stream, StateOperation change)
        {
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                long startPosition = writer.BaseStream.Position;
                writer.Write(default(long)); // this will be the length of the message, after we know what it is
                _pathIDCache.WritePath(change.source, synchronizationTarget, writer);
                writer.Write(change.opType == OpType.Remove);

                if (change.source is IStateValue prim)
                {
                    Serialization.GetSerializer(prim.valueType).Serialize(writer, change.param, false);
                }
                else if (change.source is IStateValueArray array)
                {
                    Serialization.GetSerializer(array.elementType).Serialize(writer, change.param, true);
                }
                else if (change.source is IStateDictionary dict)
                {
                    Serialization.GetSerializer(dict.keyType).Serialize(writer, change.param, false);
                }
                else if (change.source is IStateList list)
                {
                    Serialization.GetSerializer(typeof(int)).Serialize(writer, change.param, false);
                }
                else if (change.source is IStateValueSet set)
                {
                    Serialization.GetSerializer(set.elementType).Serialize(writer, change.param, false);
                }

                var length = writer.BaseStream.Position - startPosition;
                var endPosition = writer.BaseStream.Position;
                writer.BaseStream.Position = startPosition;
                writer.Write(length);
                writer.BaseStream.Position = endPosition;
            }
        }

        private void HandleEventReceived(int senderId, string topic, byte[] data)
        {
            var eventCode = data[0];

            if (eventCode == INITIAL_SYNC_EVENT)
            {
                using (var stream = new MemoryStream(data, 1, data.Length - 1))
                using (var reader = new BinaryReader(stream))
                {
                    var json = reader.ReadString();

                    _applyingRemoteChanges = true;

                    ApplyInitialSync(synchronizationTarget, JSONNode.Parse(json));

                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        var ownerId = reader.ReadInt32();
                        var primitiveId = reader.ReadUInt32();
                        var path = reader.ReadString();
                        AddHighFrequencyPrimitiveInternal(ownerId, primitiveId, path, 0, false, false);
                    }

                    _applyingRemoteChanges = false;
                }

                var synchronizedMessage = new byte[5];
                synchronizedMessage[0] = PLAYER_SYNCHRONIZED_EVENT;
                var idBytes = BitConverter.GetBytes(_client.playerId);

                for (int i = 0; i < 4; i++)
                    synchronizedMessage[i + 1] = idBytes[i];

                _client.SendEvent(synchronizedMessage, true, SyncTarget.Others, topic);

                synchronized = true;
                onInitialSynchronizationComplete?.Invoke();
            }
            else if (eventCode == INCREMENTAL_SYNC_EVENT)
            {
                _applyingRemoteChanges = true;
                ApplyIncrementalSync(synchronizationTarget, _pathIDCache, data, 1);
                _applyingRemoteChanges = false;
            }
            else if (eventCode == HIGH_FREQUENCY_ADD_ID_EVENT)
            {
                var primitiveId = BitConverter.ToUInt32(data, 1);
                var path = Encoding.UTF8.GetString(data, 5, data.Length - 5);
                AddHighFrequencyPrimitiveInternal(senderId, primitiveId, path, 0, false, false);
            }
            else if (eventCode == HIGH_FREQUENCY_SYNC_EVENT)
            {
                _applyingRemoteChanges = true;
                ApplyHighFrequencySync(synchronizationTarget, _highFrequencyPrimitives, senderId, data, 1);
                _applyingRemoteChanges = false;
            }
            else if (eventCode == HIGH_FREQUENCY_REMOVE_ID_EVENT)
            {
                var primitiveId = BitConverter.ToUInt32(data, 1);
                RemoveHighFrequencyPrimitiveInternal(senderId, primitiveId, false);
            }
            else if (eventCode == PLAYER_SYNCHRONIZED_EVENT)
            {
                var id = BitConverter.ToInt32(data, 1);
                _synchronizedPlayers.Add(id);
            }
            else
            {
                throw new Exception($"Unhandled event code {eventCode}");
            }
        }



        private void AddHighFrequencyPrimitiveInternal(int ownerId, uint primitiveId, string path, byte syncRate, bool isLocal, bool sendEvent)
        {
            var id = (ownerId, primitiveId);
            var primitive = (IStateValue)synchronizationTarget.GetChild(path);

            var highFrequencyPrimitiveData = new HighFrequencyPrimitiveData()
            {
                ownerId = ownerId,
                primitiveId = primitiveId,
                target = primitive,
                serializer = Serialization.GetSerializer(primitive.valueType),
                syncRate = syncRate
            };

            _highFrequencyPrimitives.Add(id, highFrequencyPrimitiveData);

            highFrequencyPrimitiveData.subscription = primitive.Subscribe(
                onNext: !isLocal ? null : _ =>
                {
                    if (!_applyingRemoteChanges)
                        _dirtyHighFrequencyPrimitives.Add(highFrequencyPrimitiveData);
                },
                onDispose: () => RemoveHighFrequencyPrimitiveInternal(ownerId, primitiveId, false)
            );

            if (!sendEvent)
                return;

            var data = new byte[Encoding.UTF8.GetByteCount(primitive.nodePath) + 5]; // +5 for the size of id + size of event id

            data[0] = HIGH_FREQUENCY_ADD_ID_EVENT;
            data[1] = (byte)(highFrequencyPrimitiveData.primitiveId & 0xFF);         // Lowest byte
            data[2] = (byte)((highFrequencyPrimitiveData.primitiveId >> 8) & 0xFF);
            data[3] = (byte)((highFrequencyPrimitiveData.primitiveId >> 16) & 0xFF);
            data[4] = (byte)((highFrequencyPrimitiveData.primitiveId >> 24) & 0xFF); // Highest byte

            Encoding.UTF8.GetBytes(primitive.nodePath, 0, primitive.nodePath.Length, data, 5);

            _client.SendEvent(data, true, SyncTarget.Others, topic);
        }

        private void RemoveHighFrequencyPrimitiveInternal(int ownerId, uint primitiveId, bool sendEvent)
        {
            var id = (ownerId, primitiveId);

            if (!_highFrequencyPrimitives.TryGetValue(id, out var highFrequencyPrimitiveData))
                return;

            _highFrequencyPrimitives.Remove(id);
            _dirtyHighFrequencyPrimitives.Remove(highFrequencyPrimitiveData);

            highFrequencyPrimitiveData.subscription.Dispose();

            if (!sendEvent)
                return;

            var data = new byte[5];

            data[0] = HIGH_FREQUENCY_REMOVE_ID_EVENT;
            data[1] = (byte)(highFrequencyPrimitiveData.primitiveId & 0xFF);         // Lowest byte
            data[2] = (byte)((highFrequencyPrimitiveData.primitiveId >> 8) & 0xFF);
            data[3] = (byte)((highFrequencyPrimitiveData.primitiveId >> 16) & 0xFF);
            data[4] = (byte)((highFrequencyPrimitiveData.primitiveId >> 24) & 0xFF); // Highest byte

            _client.SendEvent(data, true, SyncTarget.Others, topic);
        }

        private (int ownerId, uint primitiveId) AllocateHighFrequencyPathId()
        {
            var initialNextId = _nextLocalFrequencyPrimitiveId;
            var ownerId = _client.playerId;

            while (_nextLocalFrequencyPrimitiveId <= uint.MaxValue)
            {
                (int ownerId, uint primitiveId) id = new(ownerId, _nextLocalFrequencyPrimitiveId);
                _nextLocalFrequencyPrimitiveId++;

                if (_highFrequencyPrimitives.ContainsKey(id))
                    continue;

                return id;
            }

            _nextLocalFrequencyPrimitiveId = 0;

            while (_nextLocalFrequencyPrimitiveId < initialNextId)
            {
                (int ownerId, uint primitiveId) id = new(ownerId, _nextLocalFrequencyPrimitiveId);
                _nextLocalFrequencyPrimitiveId++;

                if (_highFrequencyPrimitives.ContainsKey(id))
                    continue;

                return id;
            }

            throw new Exception("No unused high frequency primitive id could be found.");
        }

        private void ApplyInitialSync(T state, JSONNode json)
        {
            state.context.ExecuteBatchOperation(() => state.FromJSON(json));
        }

        private void ApplyIncrementalSync(T state, PathIDCache<T> pathIDCache, byte[] data, int index = -1, int length = -1)
        {
            state.context.ExecuteBatchOperation(() =>
            {
                index = index == -1 ? 0 : index;
                length = length == -1 ? data.Length : length;

                using (var stream = new MemoryStream(data))
                using (var reader = new BinaryReader(stream))
                {
                    stream.Position = index;

                    while (stream.Position < length)
                    {
                        var startPosition = stream.Position;
                        var messageLength = reader.ReadInt64();

                        if (!pathIDCache.TryReadPath(state, reader, out var dest))
                        {
                            Debug.LogError($"Target state not found!");
                            stream.Position = startPosition + messageLength;
                            continue;
                        }

                        var isRemove = reader.ReadBoolean();

                        if (dest is IStateValue prim)
                        {
                            prim.value = Serialization.GetSerializer(prim.valueType).Deserialize(reader, false);
                        }
                        else if (dest is IStateValueArray array)
                        {
                            array.SetValue((IEnumerable)Serialization.GetSerializer(array.elementType).Deserialize(reader, true));
                        }
                        else if (dest is IStateDictionary dict)
                        {
                            var key = Serialization.GetSerializer(dict.keyType).Deserialize(reader, false);
                            if (isRemove)
                            {
                                dict.Remove(key);
                            }
                            else
                            {
                                dict.Add(key);
                            }
                        }
                        else if (dest is IStateList list)
                        {
                            var elementIndex = (int)Serialization.GetSerializer(typeof(int)).Deserialize(reader, false);
                            if (isRemove)
                            {
                                list.RemoveAt(elementIndex);
                            }
                            else
                            {
                                list.Insert(elementIndex);
                            }
                        }
                        else if (dest is IStateValueSet set)
                        {
                            var item = Serialization.GetSerializer(set.elementType).Deserialize(reader, false);
                            if (isRemove)
                            {
                                set.Remove(item);
                            }
                            else
                            {
                                set.Add(item);
                            }
                        }
                    }
                }
            });
        }

        private void ApplyHighFrequencySync(T state, IReadOnlyDictionary<(int ownerId, uint primitiveId), HighFrequencyPrimitiveData> highFrequencyPrimitiveLookup, int ownerId, byte[] data, int index = -1, int length = -1)
        {
            state.context.ExecuteBatchOperation(() =>
            {
                index = index == -1 ? 0 : index;
                length = length == -1 ? data.Length : length;

                using (var stream = new MemoryStream(data))
                using (var reader = new BinaryReader(stream))
                {
                    stream.Position = index;

                    while (stream.Position < length)
                    {
                        var startPosition = stream.Position;
                        var messageLength = reader.ReadInt64();
                        var primitiveId = reader.ReadUInt32();

                        if (!highFrequencyPrimitiveLookup.TryGetValue(new(ownerId, primitiveId), out var highFrequencyPrimitiveData))
                        {
                            Debug.LogError($"Target state not found. Owner ID: {ownerId} Primitive ID: {primitiveId}");
                            stream.Position = startPosition + messageLength;
                            continue;
                        }

                        highFrequencyPrimitiveData.target.value = highFrequencyPrimitiveData.serializer.Deserialize(reader, highFrequencyPrimitiveData.target is IStateValueArray);
                    }
                }
            });
        }

        private bool ShouldSync(IStateNode state)
            => state.attributes.All(x => x is not DoNotSync);

        public void SendHighFrequencySync()
        {
            using (var writer = new BinaryWriter(_highFrequencySyncStream, Encoding.UTF8, true))
            {
                foreach (var primitive in _dirtyHighFrequencyPrimitives)
                {
                    float timeBetweenSyncs = 1f / primitive.syncRate;

                    if (Time.time - primitive.lastSyncTime >= timeBetweenSyncs)
                    {
                        _syncedHighFrequencyPrimitives.Add(primitive);

                        primitive.lastSyncTime = Time.time;
                        var startPosition = writer.BaseStream.Position;

                        writer.Write(default(long)); // this will be the length of the message, after we know what it is
                        writer.Write(primitive.primitiveId);
                        primitive.serializer.Serialize(writer, primitive.target.value, primitive.target is IStateValueArray);

                        var endPosition = writer.BaseStream.Position;
                        writer.BaseStream.Position = startPosition;
                        writer.Write(endPosition - startPosition);
                        writer.BaseStream.Position = endPosition;
                    }
                }

                _dirtyHighFrequencyPrimitives.ExceptWith(_syncedHighFrequencyPrimitives);
                _syncedHighFrequencyPrimitives.Clear();

                writer.Flush();
            }

            if (_highFrequencySyncStream.Length > 1)
            {
                _client.SendEvent(_highFrequencySyncStream.ToArray(), false, SyncTarget.Others, topic);
                _highFrequencySyncStream.SetLength(0);
                _highFrequencySyncStream.WriteByte(HIGH_FREQUENCY_SYNC_EVENT);
            }
        }

        public void SendIncrementalSync()
        {
            if (_incrementalSyncStream.Length > 1)
            {
                _client.SendEvent(_incrementalSyncStream.ToArray(), true, SyncTarget.Others, topic);
                _incrementalSyncStream.SetLength(0);
                _incrementalSyncStream.WriteByte(INCREMENTAL_SYNC_EVENT);
            }
        }

        public void AddHighFrequencyPrimitive(string path, byte syncRate)
        {
            if (_localHighFrequencyPrimitives.TryGetValue(path, out var id))
            {
                var primitive = _highFrequencyPrimitives[id];
                primitive.syncRate = syncRate;
                return;
            }

            id = AllocateHighFrequencyPathId();
            _localHighFrequencyPrimitives.Add(path, id);
            AddHighFrequencyPrimitiveInternal(id.ownerId, id.primitiveId, path, syncRate, true, true);
        }

        public void RemoveHighFrequencyPrimitive(string path)
        {
            var id = _localHighFrequencyPrimitives[path];
            _localHighFrequencyPrimitives.Remove(path);
            RemoveHighFrequencyPrimitiveInternal(id.ownerId, id.primitiveId, true);
        }

        public void Dispose()
        {
            synchronized = false;
            _subscriptions?.Dispose();

            foreach (var highFrequencyPrimitive in _highFrequencyPrimitives.Values)
                highFrequencyPrimitive.subscription.Dispose();
        }
    }
}