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
    public class SelfHostedLiveKitClient : RealtimeClientComponent
    {
        public override bool connected => _connected;
        public override int playerId => _playerId;
        public override int hostId => _hostId;

        public override IEnumerable<int> players => _idToParticipant.Keys.Append(playerId);

        public string endpointUrl;

        private bool _connected;
        private int _playerId;
        private int _hostId;

        private Room _room;
        private Participant _host;
        private Dictionary<Participant, int> _participantToId = new Dictionary<Participant, int>();
        private Dictionary<int, Participant> _idToParticipant = new Dictionary<int, Participant>();
        private bool _connectionComplete;

        private void HandleParticipantConnected(Participant participant)
        {
            if (participant != _room.LocalParticipant &&
                participant.Attributes.ContainsKey("completed-join") &&
                !_participantToId.ContainsKey(participant))
            {
                var id = int.Parse(participant.Attributes["id"]);
                _participantToId.Add(participant, id);
                _idToParticipant.Add(id, participant);
                RaiseOnPlayerEnteredRoom(id);
            }
        }

        private void HandleParticipantAttributesChanged(Participant participant)
        {
            if (participant != _room.LocalParticipant &&
                participant.Attributes.ContainsKey("completed-join") &&
                !_participantToId.ContainsKey(participant))
            {
                var id = int.Parse(participant.Attributes["id"]);
                _participantToId.Add(participant, id);
                _idToParticipant.Add(id, participant);
                RaiseOnPlayerEnteredRoom(id);
            }
        }

        private void HandleParticipantDiconnected(Participant participant)
        {
            if (!_participantToId.TryGetValue(participant, out var id))
                return;

            _participantToId.Remove(participant);
            _idToParticipant.Remove(id);

            RaiseOnPlayerLeftRoom(id);

            if (!TryGetHost(out var newHost, out var newHostID))
                throw new Exception("No host found");

            if (newHostID == hostId)
                return;

            _host = newHost;
            _hostId = newHostID;
            RaiseOnHostChanged(hostId);
        }

        private void HandleDataReceived(byte[] data, Participant participant, DataPacketKind kind, string topic)
        {
            if (!_connectionComplete)
                return;

            RaiseOnEventReceived(_participantToId[participant], topic, data);
        }

        private void HandleDisconnected(Room room)
        {
            if (_room != room)
                return;

            if (connected)
            {
                _room = null;
                DisconnectInternal(true);
            }
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

        private UniTask DisconnectInternal(bool unexpected)
        {
            if (!connected)
                return UniTask.CompletedTask;

            _connected = false;
            _connectionComplete = false;
            _playerId = 0;
            _hostId = 0;

            _room?.Disconnect();
            _room = null;
            _participantToId.Clear();
            _idToParticipant.Clear();

            if (unexpected)
                RaiseOnDisconnectedUnexpectedly("Disconnected Unexpectedly");

            RaiseOnLeftRoom();

            return UniTask.CompletedTask;
        }

        public override async UniTask Connect(string roomID, CancellationToken cancellationToken = default)
        {
            if (connected)
                throw new Exception("Already connected");

            _connected = true;

            var tokenProvider = new TokenSourceEndpoint(endpointUrl, null);
            var connectionDetails = await tokenProvider.FetchConnectionDetails(new()
            {
                RoomName = roomID,
                ParticipantIdentity = SystemInfo.deviceUniqueIdentifier,
                ParticipantName = SystemInfo.deviceUniqueIdentifier
            });

            _room = new Room();
            _room.ParticipantConnected += HandleParticipantConnected;
            _room.ParticipantAttributesChanged += HandleParticipantAttributesChanged;
            _room.ParticipantDisconnected += HandleParticipantDiconnected;
            _room.DataReceived += HandleDataReceived;
            _room.Disconnected += HandleDisconnected;

            var options = new LiveKit.RoomOptions();
            var connect = _room.Connect(connectionDetails.ServerUrl, connectionDetails.ParticipantToken, options);

            await UniTask.WaitUntil(() => _room.ConnectionState == LiveKit.Proto.ConnectionState.ConnConnected || connect.IsError, cancellationToken: cancellationToken);

            if (connect.IsError)
            {
                _connected = false;
                _connectionComplete = false;
                throw new Exception("Connection request failed.");
            }

            foreach (var participant in _room.RemoteParticipants.Values)
            {
                if (!participant.Attributes.ContainsKey("completed-join") ||
                    _participantToId.ContainsKey(participant))
                    continue;

                int id = int.Parse(participant.Attributes["id"]);

                _idToParticipant.Add(id, participant);
                _participantToId.Add(participant, id);
            }

            _playerId = int.Parse(_room.LocalParticipant.Attributes["id"]);
            _idToParticipant.Add(playerId, _room.LocalParticipant);
            _participantToId.Add(_room.LocalParticipant, playerId);

            // There are no participants in the room yet, we're the host
            if (!TryGetHost(out var host, out var currentHostID))
                throw new Exception("No host found");

            _host = host;
            _hostId = currentHostID;

            await _room.LocalParticipant.SetAttributes(new Dictionary<string, string>() { { "completed-join", "true" } });
            _connectionComplete = true;

            RaiseOnJoinedRoom();
        }

        public override UniTask Disconnect()
            => DisconnectInternal(false);

        public override void SendEvent(byte[] data, bool reliable, int[] targets, string topic = null)
        {
            if (!connected)
                throw new Exception("Not connected");

            _room.LocalParticipant.PublishData(data, targets.Select(x => _idToParticipant[x].Identity).ToArray(), reliable, topic);
        }

        public override void SendEvent(byte[] data, bool reliable, SyncTarget target, string topic = null)
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
    }
}