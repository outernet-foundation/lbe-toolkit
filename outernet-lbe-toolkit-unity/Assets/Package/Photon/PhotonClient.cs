using System;

using Cysharp.Threading.Tasks;

using Photon.Realtime;
using Photon.Client;

using System.Collections.Generic;
using System.Threading;

using PhotonRealtimeClient = Photon.Realtime.RealtimeClient;

namespace Outernet.LBEToolkit.StateSynchronization
{
    public class PhotonClient : IRealtimeClient, IInRoomCallbacks, IOnEventCallback, IConnectionCallbacks, IMatchmakingCallbacks
    {
        private const byte DATA_EVENT = 1;

        public bool connected => _client.InRoom;
        public int playerId => _client.LocalPlayer.ActorNumber;
        public int hostId => _client.CurrentRoom.MasterClientId;

        public IEnumerable<int> players => _client.CurrentRoom.Players.Keys;

        public event Action<int> onHostChanged;
        public event Action<int> onPlayerEnteredRoom;
        public event Action<int> onPlayerLeftRoom;
        public event Action<int, string, byte[]> onEventReceived;
        public event Action<string> onDisconnectedUnexpectedly;
        public event Action onJoinedRoom;
        public event Action onLeftRoom;

        private string _realtimeId;
        private CancellationTokenSource _updateCancellationTokenSource = new CancellationTokenSource();

        private PhotonRealtimeClient _client;
        private Dictionary<SyncTarget, ReceiverGroup> _syncTargetToReceiverGroup = new Dictionary<SyncTarget, ReceiverGroup>()
        {
            { SyncTarget.All, ReceiverGroup.All },
            { SyncTarget.Others, ReceiverGroup.Others },
            { SyncTarget.Server, ReceiverGroup.MasterClient }
        };

        public PhotonClient(string realtimeId)
        {
            AsyncSetup.Startup();

            _realtimeId = realtimeId;
            _client = new PhotonRealtimeClient(ConnectionProtocol.Tcp);
            _client.AddCallbackTarget(this);

            Update(_updateCancellationTokenSource.Token).Forget();
            LateUpdate(_updateCancellationTokenSource.Token).Forget();
        }

        private async UniTask Update(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                while (true)
                {
                    if (!_client.DispatchIncomingCommands())
                        break;
                }

                await UniTask.Yield(PlayerLoopTiming.Update);
            }
        }

        private async UniTask LateUpdate(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                while (true)
                {
                    if (!_client.DispatchIncomingCommands())
                        break;
                }

                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            }
        }

        // IInRoomCallbacks
        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            onPlayerEnteredRoom?.Invoke(newPlayer.ActorNumber);
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            onPlayerLeftRoom?.Invoke(otherPlayer.ActorNumber);
        }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
        {

        }

        public void OnMasterClientSwitched(Player newMasterClient)
        {
            onHostChanged?.Invoke(hostId);
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

            onEventReceived?.Invoke(photonEvent.Sender, topic == null ? null : (string)topic, (byte[])data);
        }

        // IConnectionCallbacks
        public void OnConnected() { }

        public void OnConnectedToMaster() { }

        public void OnDisconnected(DisconnectCause cause)
        {
            _client.Disconnect();

            if (connected)
                onDisconnectedUnexpectedly?.Invoke(cause.ToString());
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

        public async UniTask Connect(string roomID, CancellationToken cancellationToken = default)
        {
            if (connected)
                return;

            await _client.ConnectUsingSettingsAsync(new AppSettings() { AppIdRealtime = _realtimeId });

            cancellationToken.ThrowIfCancellationRequested();

            await _client.ConnectToRoomAsync(new MatchmakingArguments()
            {
                RoomName = roomID,
                PhotonSettings = _client.AppSettings
            });

            cancellationToken.ThrowIfCancellationRequested();

            await UniTask.SwitchToMainThread(cancellationToken: cancellationToken);
        }

        public async UniTask Disconnect()
        {
            if (!connected)
                return;

            await _client.LeaveRoomAsync();
            await _client.DisconnectAsync();
        }

        public void SendEvent(byte[] data, bool reliable, int[] targets, string topic = null)
        {
            var message = new object[] { data, topic };
            _client.OpRaiseEvent(
                DATA_EVENT,
                message,
                new RaiseEventArgs() { TargetActors = targets },
                new SendOptions() { Reliability = reliable }
            );
        }

        public void SendEvent(byte[] data, bool reliable, SyncTarget target, string topic = null)
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
            onJoinedRoom?.Invoke();
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {

        }

        public void OnJoinRandomFailed(short returnCode, string message)
        {

        }

        public void OnLeftRoom()
        {
            onLeftRoom?.Invoke();
        }

        public void Dispose()
        {
            _client.Disconnect();
            _updateCancellationTokenSource.Cancel();
            _updateCancellationTokenSource.Dispose();
            _updateCancellationTokenSource = null;
        }
    }
}