using System;
using Cysharp.Threading.Tasks;

namespace Outernet.LBEToolkit.StateSynchronization
{
    public interface IStateSyncManager : IDisposable
    {
        bool synchronized { get; }
        string topic { get; }

        UniTask PerformInitialSync();
    }
}