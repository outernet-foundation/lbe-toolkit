using UnityEngine;

using System;
using System.Linq;

using Cysharp.Threading.Tasks;

using Photon.Realtime;
using Photon.Client;

using System.Collections.Generic;
using System.Threading;

using PhotonRealtimeClient = Photon.Realtime.RealtimeClient;

namespace Outernet.LBEToolkit.StateSynchronization
{
    public class PhotonClient : RealtimeClientComponent, IInRoomCallbacks, IOnEventCallback, IConnectionCallbacks, IMatchmakingCallbacks
    {
        private const byte DATA_EVENT = 1;

        public override bool connected => _client.InRoom;
        public override int playerId => _client.LocalPlayer.ActorNumber;
        public override int hostId => _client.CurrentRoom.MasterClientId;

        public override IEnumerable<int> players => _client.CurrentRoom.Players.Keys;

        public string realtimeId;
        public bool selfHosted;

        [ToggleGroup(nameof(selfHosted))]
        public string selfHostedIp;

        [ToggleGroup(nameof(selfHosted))]
        public ushort selfHostedPort;

        private PhotonRealtimeClient _client;
        private Dictionary<SyncTarget, ReceiverGroup> _syncTargetToReceiverGroup = new Dictionary<SyncTarget, ReceiverGroup>()
        {
            { SyncTarget.All, ReceiverGroup.All },
            { SyncTarget.Others, ReceiverGroup.Others },
            { SyncTarget.Server, ReceiverGroup.MasterClient }
        };

        private void Awake()
        {
            AsyncSetup.Startup();

            _client = new PhotonRealtimeClient(ConnectionProtocol.Tcp);
            _client.AddCallbackTarget(this);
        }

        private void Update()
        {
            while (true)
            {
                if (!_client.DispatchIncomingCommands())
                    break;
            }
        }

        private void LateUpdate()
        {
            while (true)
            {
                if (!_client.SendOutgoingCommands())
                    break;
            }
        }

        private void OnDestroy()
        {
            _client.Disconnect();
        }

        // IInRoomCallbacks
        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            RaiseOnPlayerEnteredRoom(newPlayer.ActorNumber);
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            RaiseOnPlayerLeftRoom(otherPlayer.ActorNumber);
        }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
        {

        }

        public void OnMasterClientSwitched(Player newMasterClient)
        {
            RaiseOnHostChanged(hostId);
        }

        public void OnRoomPropertiesUpdate(PhotonHashtable propertiesThatChanged) { }

        // IOnEventCallback
        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != DATA_EVENT)
                return;

            var message = (object[])photonEvent.CustomData;
            var topic = message[0];
            var data = message[1];

            RaiseOnEventReceived(photonEvent.Sender, topic == null ? null : (string)topic, (byte[])data);
        }

        // IConnectionCallbacks
        public void OnConnected() { }

        public void OnConnectedToMaster() { }

        public void OnDisconnected(DisconnectCause cause)
        {
            _client.Disconnect();

            if (connected)
                RaiseOnDisconnectedUnexpectedly(cause.ToString());
        }

        public void OnRegionListReceived(RegionHandler regionHandler)
        {

        }

        public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
        {

        }

        public void OnCustomAuthenticationFailed(string debugMessage)
        {

        }

        public override async UniTask Connect(string roomID, CancellationToken cancellationToken = default)
        {
            if (connected)
                return;

            if (selfHosted)
            {
                await _client.ConnectUsingSettingsAsync(new AppSettings() { AppIdRealtime = realtimeId, Server = selfHostedIp, Port = selfHostedPort });
            }
            else
            {
                await _client.ConnectUsingSettingsAsync(new AppSettings() { AppIdRealtime = realtimeId });
            }

            cancellationToken.ThrowIfCancellationRequested();

            await _client.ConnectToRoomAsync(new MatchmakingArguments()
            {
                RoomName = roomID,
                PhotonSettings = _client.AppSettings
            });

            cancellationToken.ThrowIfCancellationRequested();

            await UniTask.SwitchToMainThread(cancellationToken: cancellationToken);
        }

        public override async UniTask Disconnect()
        {
            if (!connected)
                return;

            await _client.LeaveRoomAsync();
            await _client.DisconnectAsync();
        }

        public override void SendEvent(byte[] data, bool reliable, int[] targets, string topic = null)
        {
            var message = new object[] { data, topic };
            _client.OpRaiseEvent(
                DATA_EVENT,
                message,
                new RaiseEventArgs() { TargetActors = targets },
                new SendOptions() { Reliability = reliable }
            );
        }

        public override void SendEvent(byte[] data, bool reliable, SyncTarget target, string topic = null)
        {
            var message = new object[] { data, topic };
            _client.OpRaiseEvent(
                DATA_EVENT,
                message,
                new RaiseEventArgs() { Receivers = _syncTargetToReceiverGroup[target] },
                new SendOptions() { Reliability = reliable }
            );
        }

        public void OnFriendListUpdate(List<FriendInfo> friendList)
        {

        }

        public void OnCreatedRoom()
        {

        }

        public void OnCreateRoomFailed(short returnCode, string message)
        {

        }

        public void OnJoinedRoom()
        {
            RaiseOnJoinedRoom();
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {

        }

        public void OnJoinRandomFailed(short returnCode, string message)
        {

        }

        public void OnLeftRoom()
        {
            RaiseOnLeftRoom();
        }
    }
}