using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Outernet.LBEToolkit.StateSynchronization
{
    public abstract class RealtimeClientWrapper : RealtimeClientComponent
    {
        public override bool connected => _instance.connected;
        public override int playerId => _instance.playerId;
        public override int hostId => _instance.hostId;
        public override IEnumerable<int> players => _instance.players;
        private IRealtimeClient _instance;

        private void Awake()
        {
            _instance = GetInstance();
            _instance.onHostChanged += RaiseOnHostChanged;
            _instance.onPlayerEnteredRoom += RaiseOnPlayerEnteredRoom;
            _instance.onPlayerLeftRoom += RaiseOnPlayerLeftRoom;
            _instance.onEventReceived += RaiseOnEventReceived;
            _instance.onDisconnectedUnexpectedly += RaiseOnDisconnectedUnexpectedly;
            _instance.onJoinedRoom += RaiseOnJoinedRoom;
            _instance.onLeftRoom += RaiseOnLeftRoom;
        }

        protected abstract IRealtimeClient GetInstance();

        public override UniTask Connect(string roomID, CancellationToken cancellationToken = default)
        {
            return _instance.Connect(roomID, cancellationToken);
        }

        public override UniTask Disconnect()
        {
            return _instance.Disconnect();
        }

        public override void SendEvent(byte[] data, bool reliable, int[] targets, string topic = null)
        {
            _instance.SendEvent(data, reliable, targets, topic);
        }

        public override void SendEvent(byte[] data, bool reliable, SyncTarget target, string topic = null)
        {
            _instance.SendEvent(data, reliable, target, topic);
        }
    }
}