using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;

namespace Outernet.LBEToolkit.StateSynchronization
{
    public abstract class RealtimeClientComponent : MonoBehaviour, IRealtimeClient
    {
        public abstract int playerId { get; }
        public abstract int hostId { get; }
        public abstract IEnumerable<int> players { get; }
        public abstract bool connected { get; }

        public event Action<int> onHostChanged;
        public event Action<int> onPlayerEnteredRoom;
        public event Action<int> onPlayerLeftRoom;
        public event Action<int, string, byte[]> onEventReceived;
        public event Action<string> onDisconnectedUnexpectedly;
        public event Action onJoinedRoom;
        public event Action onLeftRoom;

        public abstract UniTask Connect(string roomID, CancellationToken cancellationToken = default);
        public abstract UniTask Disconnect();
        public abstract void SendEvent(byte[] data, bool reliable, int[] targets, string topic = null);
        public abstract void SendEvent(byte[] data, bool reliable, SyncTarget target, string topic = null);

        protected void RaiseOnHostChanged(int hostId)
            => onHostChanged?.Invoke(hostId);

        protected void RaiseOnPlayerEnteredRoom(int playerId)
            => onPlayerEnteredRoom?.Invoke(playerId);

        protected void RaiseOnPlayerLeftRoom(int playerId)
            => onPlayerLeftRoom?.Invoke(playerId);

        protected void RaiseOnEventReceived(int senderId, string topic, byte[] data)
            => onEventReceived?.Invoke(senderId, topic, data);

        protected void RaiseOnDisconnectedUnexpectedly(string cause)
            => onDisconnectedUnexpectedly?.Invoke(cause);

        protected void RaiseOnJoinedRoom()
            => onJoinedRoom?.Invoke();

        protected void RaiseOnLeftRoom()
            => onLeftRoom?.Invoke();
    }
}