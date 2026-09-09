using System;

namespace Outernet.LBEToolkit.StateSynchronization
{
    public interface IStateSyncManager : IDisposable
    {
        bool synchronized { get; }
        string topic { get; }

        event Action onInitialSynchronizationComplete;
    }
}