using System;

using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace Outernet.LBEToolkit.StateSynchronization
{
    public enum SyncTarget
    {
        All,
        Others,
        Server
    }

    public interface IRealtimeClient
    {
        bool connected { get; }

        int playerId { get; }
        int hostId { get; }
        IEnumerable<int> players { get; }

        event Action onJoinedRoom;
        event Action onLeftRoom;
        event Action<int> onHostChanged;
        event Action<int> onPlayerEnteredRoom;
        event Action<int> onPlayerLeftRoom;
        event Action<string> onDisconnectedUnexpectedly;

        event Action<int, string, byte[]> onEventReceived;

        UniTask Connect(string roomID, CancellationToken cancellationToken = default);
        UniTask Disconnect();

        void SendEvent(byte[] data, bool reliable, int[] targets, string topic = null);
        void SendEvent(byte[] data, bool reliable, SyncTarget target, string topic = null);
    }
}