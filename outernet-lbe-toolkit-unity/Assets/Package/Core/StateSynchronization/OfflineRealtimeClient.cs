using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Outernet.LBEToolkit.StateSynchronization
{
    public class OfflineRealtimeClient : IRealtimeClient
    {
        public bool connected { get; private set; }
        public int playerId { get; private set; }
        public int hostId { get; private set; }

        public IEnumerable<int> players { get; private set; }

        public event Action<int> onHostChanged;
        public event Action<int> onPlayerEnteredRoom;
        public event Action<int> onPlayerLeftRoom;
        public event Action<string> onDisconnectedUnexpectedly;
        public event Action<int, string, byte[]> onEventReceived;
        public event Action onJoinedRoom;
        public event Action onLeftRoom;

        public UniTask Connect(string roomID, CancellationToken cancellationToken = default)
        {
            hostId = 1;
            playerId = 1;
            players = new int[] { 1 };
            connected = true;
            return UniTask.CompletedTask;
        }

        public UniTask Disconnect()
        {
            hostId = 0;
            playerId = 0;
            connected = false;
            return UniTask.CompletedTask;
        }

        public void SendEvent(byte[] data, bool reliable, int[] targets, string topic = null)
        {
            if (targets.Contains(playerId))
                onEventReceived?.Invoke(playerId, topic, data);
        }

        public void SendEvent(byte[] data, bool reliable, SyncTarget target, string topic = null)
        {
            if (target == SyncTarget.All || target == SyncTarget.Server)
                onEventReceived?.Invoke(playerId, topic, data);
        }
    }
}