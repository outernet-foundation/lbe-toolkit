using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FofX.Stateful;
using NUnit.Framework;
using UnityEngine;

namespace Outernet.LBEToolkit.StateSynchronization.Tests
{
    public class StateSynchronizationTests
    {
        public class TestState : StateObject
        {
            public StateValue<string> value { get; private set; }
            public StateDictionary<int, StateValue<string>> valueDict { get; private set; }
            public StateList<StateValue<string>> valueList { get; private set; }
            public StateValueSet<string> set { get; private set; }

            public StateDictionary<string, NestedTestState> stateDict { get; private set; }
            public StateList<NestedTestState> stateList { get; private set; }

            public NestedTestState nestedState { get; private set; }
        }

        public class NestedTestState : StateObject
        {
            public StateValue<string> value { get; private set; }
            public StateDictionary<int, StateValue<string>> valueDict { get; private set; }
            public StateList<StateValue<string>> valueList { get; private set; }
            public StateValueSet<string> set { get; private set; }

            public StateDictionary<Vector3, DoubleNestedState> stateDict { get; private set; }
            public StateList<DoubleNestedState> stateList { get; private set; }

            public DoubleNestedState nestedState { get; private set; }
        }

        public class DoubleNestedState : StateObject
        {
            public StateValue<string> value { get; private set; }
            public StateDictionary<int, StateValue<string>> valueDict { get; private set; }
            public StateList<StateValue<string>> valueList { get; private set; }
            public StateValueSet<string> set { get; private set; }
        }

        [Test]
        public void TestPathCacheGeneration()
        {
            var idCache = new PathIDCache<TestState>();
            var state = new TestState();
            state.Initialize(new ObserveThing.ObservationContext(), new DefaultLogger() { logLevel = LogLevel.None });
            PopulateCollections(state);

            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true))
                    WriteStatePathsRecursive(idCache, state, state, writer);

                stream.Position = 0;

                using (var reader = new BinaryReader(stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true))
                    ReadStatePathsRecursive(idCache, state, state, reader);

                stream.Position = 0;

                using (var reader = new BinaryReader(stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true))
                    TryReadStatePathsRecursive(idCache, state, state, reader);

                idCache.ClearCaches();

                stream.Position = 0;

                using (var reader = new BinaryReader(stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true))
                    TryReadStatePathsRecursive(idCache, state, state, reader);

                stream.Position = 0;

                using (var reader = new BinaryReader(stream, encoding: System.Text.Encoding.UTF8, leaveOpen: true))
                    ReadStatePathsRecursive(idCache, state, state, reader);
            }
        }

        private void PopulateCollections(IStateNode stateNode)
        {
            if (stateNode is IStateList list)
            {
                list.Add();
            }
            else if (stateNode is IStateDictionary dictionary)
            {
                if (dictionary.keyType == typeof(string))
                {
                    dictionary.Add("DEFAULT");
                }
                else
                {
                    dictionary.Add(Activator.CreateInstance(dictionary.keyType));
                }
            }

            foreach (var child in stateNode.children)
                PopulateCollections(child);
        }

        private void WriteStatePathsRecursive<T>(PathIDCache<T> idCache, IStateNode state, T relativeTo, BinaryWriter writer) where T : class, IStateNode, new()
        {
            if (!(state is StateObject))
                idCache.WritePath(state, relativeTo, writer);

            foreach (var child in state.children.OrderBy(x => x.nodeName))
                WriteStatePathsRecursive(idCache, child, relativeTo, writer);
        }

        private void ReadStatePathsRecursive<T>(PathIDCache<T> idCache, T root, IStateNode state, BinaryReader reader) where T : class, IStateNode, new()
        {
            if (!(state is StateObject))
            {
                var result = idCache.ReadPath(root, reader);
                Assert.AreEqual(state, result);
            }

            foreach (var child in state.children.OrderBy(x => x.nodeName))
                ReadStatePathsRecursive(idCache, root, child, reader);
        }

        private void TryReadStatePathsRecursive<T>(PathIDCache<T> idCache, T root, IStateNode state, BinaryReader reader) where T : class, IStateNode, new()
        {
            if (!(state is StateObject))
            {
                var found = idCache.TryReadPath(root, reader, out var result);
                Assert.IsTrue(found);
                Assert.AreEqual(state, result);
            }

            foreach (var child in state.children.OrderBy(x => x.nodeName))
                ReadStatePathsRecursive(idCache, root, child, reader);
        }

        public class InitialSyncState : StateObject
        {
            public StateValue<int> someValue { get; private set; }
            public StateValue<float> someOtherValue { get; private set; }
        }

        public class TestRealtimeClient : IRealtimeClient
        {
            public bool connected { get; private set; }
            public int playerId { get; private set; }
            public int hostId { get; private set; }

            public IEnumerable<int> players => _players;

            private HashSet<int> _players = new HashSet<int>();

            public event Action onJoinedRoom;
            public event Action onLeftRoom;
            public event Action<int> onHostChanged;
            public event Action<int> onPlayerEnteredRoom;
            public event Action<int> onPlayerLeftRoom;
            public event Action<string> onDisconnectedUnexpectedly;
            public event Action<int, string, byte[]> onEventReceived;

            public Action<byte[], bool, int[], string> mockSendEvent;

            public void MockHostChanged(int hostId)
            {
                this.hostId = hostId;
                onHostChanged?.Invoke(hostId);
            }

            public void MockPlayerJoined(int playerId)
            {
                if (_players.Add(playerId))
                    onPlayerEnteredRoom?.Invoke(playerId);
            }

            public void MockPlayerLeft(int playerId)
            {
                if (_players.Remove(playerId))
                    onPlayerLeftRoom?.Invoke(playerId);
            }

            public void MockDisconnectedUnexpectedly(string reason)
            {
                onDisconnectedUnexpectedly?.Invoke(reason);
            }

            public void MockEventReceived(int sender, string topic, byte[] data)
            {
                onEventReceived?.Invoke(sender, topic, data);
            }

            public async UniTask MockConnect(string roomId, int playerId, int hostId, int[] players, CancellationToken cancellationToken = default)
            {
                this.playerId = playerId;
                this.hostId = hostId;

                _players.Add(playerId);
                _players.Add(hostId);

                foreach (var player in players)
                    _players.Add(player);

                connected = true;

                onJoinedRoom?.Invoke();
            }

            public UniTask Connect(string roomId, CancellationToken cancellationToken = default)
                => MockConnect(roomId, 1, 1, new int[] { 1 }, cancellationToken);

            public UniTask Disconnect()
            {
                connected = false;
                playerId = default;
                hostId = default;
                _players.Clear();
                onLeftRoom?.Invoke();

                return UniTask.CompletedTask;
            }

            public void SendEvent(byte[] data, bool reliable, int[] targets, string topic = null)
                => mockSendEvent?.Invoke(data, reliable, targets, topic);

            public void SendEvent(byte[] data, bool reliable, SyncTarget target, string topic = null)
            {
                if (!connected)
                    throw new Exception("Not connected");

                int[] targets;

                switch (target)
                {
                    case SyncTarget.All:
                        targets = _players.ToArray();
                        break;

                    case SyncTarget.Others:
                        targets = _players.Where(x => x != playerId).ToArray();
                        break;

                    case SyncTarget.Server:
                        targets = new int[] { hostId };
                        break;

                    default:
                        throw new Exception($"Unhandled sync target {target}");
                }

                SendEvent(data, reliable, targets, topic);
            }
        }

        [Test]
        public void TestInitialSync()
        {
            var s1 = new InitialSyncState();
            var s2 = new InitialSyncState();

            s1.Initialize(new ObserveThing.ObservationContext(), new DefaultLogger(), "s1");
            s2.Initialize(new ObserveThing.ObservationContext(), new DefaultLogger(), "s2");

            s1.someValue.value = 2;
            s1.someOtherValue.value = 1.33f;

            var rtc1 = new TestRealtimeClient();
            var rtc2 = new TestRealtimeClient();

            rtc1.mockSendEvent = (data, reliable, targets, topic) => rtc2.MockEventReceived(rtc1.playerId, topic, data);
            rtc2.mockSendEvent = (data, reliable, targets, topic) => rtc1.MockEventReceived(rtc2.playerId, topic, data);

            var sync1 = new PeerToPeerStateSyncManager<InitialSyncState>(s1, rtc1, "test");
            var sync2 = new PeerToPeerStateSyncManager<InitialSyncState>(s2, rtc2, "test");

            rtc1.MockConnect("test-room", 1, 1, new int[] { 1 }).Forget();
            rtc1.MockPlayerJoined(2);

            rtc2.MockConnect("test-room", 2, 1, new int[] { 1, 2 }).Forget();

            Assert.IsTrue(sync2.synchronized);
            Assert.AreEqual(s1.someValue.value, s2.someValue.value);
            Assert.AreEqual(s1.someOtherValue.value, s2.someOtherValue.value);
        }
    }
}