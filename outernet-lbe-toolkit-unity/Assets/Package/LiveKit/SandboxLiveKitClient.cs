using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LiveKit;
using LiveKit.Proto;
using UnityEngine;

namespace Outernet.LBEToolkit.StateSynchronization
{
    public class SandboxLiveKitClient : IRealtimeClient
    {
        public bool connected => (_room?.IsConnected ?? false) && _connectionComplete;
        public int playerId { get; private set; }
        public int hostId { get; private set; }

        public IEnumerable<int> players => _idToParticipant.Keys;

        public event Action<int> onHostChanged;
        public event Action<int> onPlayerEnteredRoom;
        public event Action<int> onPlayerLeftRoom;
        public event Action<int, string, byte[]> onEventReceived;
        public event Action<string> onDisconnectedUnexpectedly;
        public event Action onJoinedRoom;
        public event Action onLeftRoom;

        private string _sandboxId;
        private Room _room;
        private Participant _host;
        private int _nextPlayerID = 1;
        private Dictionary<Participant, int> _participantToId = new Dictionary<Participant, int>();
        private Dictionary<int, Participant> _idToParticipant = new Dictionary<int, Participant>();
        private HashSet<Participant> _pendingJoins = new HashSet<Participant>();
        private bool _connectionComplete;

        public SandboxLiveKitClient(string sandboxId)
        {
            _sandboxId = sandboxId;
        }

        private void HandleParticipantConnected(Participant participant)
        {
            _pendingJoins.Add(participant);

            if (hostId != playerId)
                return;

            var assignedID = _nextPlayerID;
            _nextPlayerID++;
            _room.LocalParticipant.PublishData(BitConverter.GetBytes(assignedID), new string[] { participant.Identity }, true, "handleConnect");
        }

        private void HandleParticipantDiconnected(Participant participant)
        {
            _pendingJoins.Remove(participant);

            if (!_participantToId.TryGetValue(participant, out var id))
                return;

            _participantToId.Remove(participant);
            _idToParticipant.Remove(id);

            onPlayerLeftRoom?.Invoke(id);

            if (!TryGetHost(out var newHost, out var newHostID))
            {
                //we are now the host and we're not synced- pretend this room just started
                _host = _room.LocalParticipant;
                _idToParticipant.Add(1, _room.LocalParticipant);
                _participantToId.Add(_room.LocalParticipant, 1);

                playerId = 1;
                hostId = 1;

                _room.LocalParticipant.SetAttributes(new Dictionary<string, string>() { { "id", "1" } });
                onHostChanged?.Invoke(1);
                return;
            }

            if (newHostID == hostId)
                return;

            _host = newHost;
            hostId = newHostID;
            _nextPlayerID = _idToParticipant.Keys.Max() + 1;
            onHostChanged?.Invoke(hostId);
        }

        private void HandleParticipantAttributesChanged(Participant participant)
        {
            if (!_pendingJoins.Contains(participant) ||
                !participant.Attributes.TryGetValue("id", out var playerID))
            {
                return;
            }

            _pendingJoins.Remove(participant);

            var id = int.Parse(playerID);

            _participantToId.Add(participant, id);
            _idToParticipant.Add(id, participant);

            onPlayerEnteredRoom?.Invoke(id);

            if (hostId != 0 && id > hostId)
                return;

            hostId = id;
            onHostChanged?.Invoke(id);
        }

        private void HandleDataReceived(byte[] data, Participant participant, DataPacketKind kind, string topic)
        {
            if (topic == "handleConnect")
            {
                playerId = BitConverter.ToInt32(data);
                _room.LocalParticipant.SetAttributes(new Dictionary<string, string>() { { "id", playerId.ToString() } });
                _idToParticipant.Add(playerId, _room.LocalParticipant);
                _participantToId.Add(_room.LocalParticipant, playerId);

                onJoinedRoom?.Invoke();

                return;
            }

            if (playerId == 0)
                return;

            onEventReceived?.Invoke(_participantToId[participant], topic, data);
        }

        private void HandleDisconnected(Room room, DisconnectReason reason)
        {
            if (_room != room)
                return;

            _connectionComplete = false;
            playerId = 0;
            hostId = 0;
            _nextPlayerID = 0;

            _room.Dispose();
            _room = null;
            _participantToId.Clear();
            _idToParticipant.Clear();
            _pendingJoins.Clear();

            if (reason != DisconnectReason.ClientInitiated)
                onDisconnectedUnexpectedly?.Invoke(reason.ToString());

            onLeftRoom?.Invoke();
        }

        private bool TryGetHost(out Participant host, out int hostID)
        {
            if (_idToParticipant.Count == 0)
            {
                host = default;
                hostID = default;
                return false;
            }

            int foundID = int.MaxValue;
            Participant foundHost = default;

            foreach (var kvp in _idToParticipant)
            {
                if (kvp.Key < foundID)
                {
                    foundID = kvp.Key;
                    foundHost = kvp.Value;
                }
            }

            host = foundHost;
            hostID = foundID;
            return true;
        }

        public async UniTask Connect(string roomID, CancellationToken cancellationToken = default)
        {
            if (_room != null)
                throw new Exception("Already connected or connecting");

            _room = new Room();
            _room.ParticipantConnected += HandleParticipantConnected;
            _room.ParticipantDisconnected += HandleParticipantDiconnected;
            _room.ParticipantAttributesChanged += HandleParticipantAttributesChanged;
            _room.DataReceived += HandleDataReceived;
            _room.DisconnectedWithReason += HandleDisconnected;

            var identity = Guid.NewGuid();
            var tokenProvider = TokenSource.DevelopmentTokenServer(_sandboxId);
            var fetchConnectionDetailsTask = tokenProvider.FetchConnectionDetails(new()
            {
                RoomName = roomID,
                ParticipantIdentity = identity.ToString(),
                ParticipantName = identity.ToString()
            });

            await fetchConnectionDetailsTask;

            var connectionDetails = fetchConnectionDetailsTask.Result;

            var options = new LiveKit.RoomOptions();
            var connect = _room.Connect(connectionDetails.ServerUrl, connectionDetails.ParticipantToken, options);

            await UniTask.WaitUntil(() => _room.ConnectionState == ConnectionState.ConnConnected || connect.IsError, cancellationToken: cancellationToken);

            if (connect.IsError)
            {
                _connectionComplete = false;
                throw new Exception("Connection request failed.");
            }

            foreach (var participant in _room.RemoteParticipants.Values)
            {
                if (participant.Attributes.TryGetValue("id", out var playerID))
                {
                    int id = int.Parse(playerID);
                    _idToParticipant.Add(id, participant);
                    _participantToId.Add(participant, id);
                }
                else
                {
                    _pendingJoins.Add(participant);
                }
            }

            // There are no participants in the room yet, we're the host
            if (!TryGetHost(out var host, out var currentHostID))
            {
                _host = _room.LocalParticipant;
                _idToParticipant.Add(1, _room.LocalParticipant);
                _participantToId.Add(_room.LocalParticipant, 1);

                playerId = 1;
                hostId = 1;
                _nextPlayerID++;

                await _room.LocalParticipant.SetAttributes(new Dictionary<string, string>() { { "id", "1" } });

                return;
            }

            _host = host;
            hostId = currentHostID;
            await UniTask.WaitUntil(() => playerId != 0, cancellationToken: cancellationToken);

            _connectionComplete = true;
            onJoinedRoom?.Invoke();
        }

        public UniTask Disconnect()
        {
            _room?.Disconnect();
            return UniTask.CompletedTask;
        }

        public void SendEvent(byte[] data, bool reliable, int[] targets, string topic = null)
        {
            if (!connected)
                throw new Exception("Not connected");

            _room.LocalParticipant.PublishData(data, targets.Select(x => _idToParticipant[x].Identity).ToArray(), reliable, topic);
        }

        public void SendEvent(byte[] data, bool reliable, SyncTarget target, string topic = null)
        {
            if (!connected)
                throw new Exception("Not connected");

            string[] targets;

            switch (target)
            {
                case SyncTarget.All:
                    targets = _participantToId.Keys.Select(x => x.Identity).ToArray();
                    break;

                case SyncTarget.Others:
                    targets = _participantToId.Keys.Where(x => x.Identity != _room.LocalParticipant.Identity).Select(x => x.Identity).ToArray();
                    break;

                case SyncTarget.Server:
                    targets = new string[] { _host.Identity };
                    break;

                default:
                    throw new Exception($"Unhandled sync target {target}");
            }

            _room.LocalParticipant.PublishData(data, targets, reliable, topic);
        }

        public void Dispose()
        {
            _room?.Dispose();
            _room = null;
        }
    }
}