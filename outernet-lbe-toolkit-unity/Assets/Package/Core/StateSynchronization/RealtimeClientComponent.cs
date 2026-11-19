using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace Outernet.LBEToolkit.StateSynchronization
{
    public abstract class RealtimeClientComponent : MonoBehaviour, IRealtimeClient
    {
        public bool connected => _client.connected;
        public int playerId => _client.playerId;
        public int hostId => _client.hostId;
        public IEnumerable<int> players => _client.players;

        public event Action<int> onHostChanged;
        public event Action<int> onPlayerEnteredRoom;
        public event Action<int> onPlayerLeftRoom;
        public event Action<int, string, byte[]> onEventReceived;
        public event Action<string> onDisconnectedUnexpectedly;
        public event Action onJoinedRoom;
        public event Action onLeftRoom;

        private IRealtimeClient _client;

        private IRealtimeClient GetClient()
        {
            if (_client == null)
                throw new Exception($"Client is not set! Call {nameof(SetClientInstance)} before invoking member methods!");

            return _client;
        }

        protected void SetClientInstance(IRealtimeClient client)
        {
            if (_client == null)
                throw new Exception("Client can only be set once");

            _client = client;

            _client.onHostChanged += host => onHostChanged?.Invoke(host);
            _client.onPlayerEnteredRoom += player => onPlayerEnteredRoom?.Invoke(player);
            _client.onPlayerLeftRoom += player => onPlayerLeftRoom?.Invoke(player);
            _client.onEventReceived += (sender, topic, data) => onEventReceived?.Invoke(sender, topic, data);
            _client.onDisconnectedUnexpectedly += reason => onDisconnectedUnexpectedly?.Invoke(reason);
            _client.onJoinedRoom += () => onJoinedRoom?.Invoke();
            _client.onLeftRoom += () => onLeftRoom?.Invoke();
        }

        public UniTask Connect(string roomID, CancellationToken cancellationToken = default)
            => GetClient().Connect(roomID, cancellationToken);

        public UniTask Disconnect()
            => GetClient().Disconnect();

        public void SendEvent(byte[] data, bool reliable, int[] targets, string topic = null)
            => GetClient().SendEvent(data, reliable, targets, topic);

        public void SendEvent(byte[] data, bool reliable, SyncTarget target, string topic = null)
            => GetClient().SendEvent(data, reliable, target, topic);

        public void Dispose()
            => GetClient().Dispose();
    }
}