using System;
using UnityEngine;

namespace Outernet.LBEToolkit.StateSynchronization
{
    public abstract class StateSyncManagerComponent : MonoBehaviour, IStateSyncManager
    {
        public abstract bool synchronized { get; }
        public abstract string topic { get; }

        public event Action onInitialSynchronizationComplete;

        private void OnDestroy()
        {
            OnDispose();
        }

        protected void RaiseOnInitialSynchronizationComplete()
        {
            onInitialSynchronizationComplete?.Invoke();
        }

        protected virtual void OnDispose() { }

        public void Dispose()
        {
            Destroy(this);
        }
    }
}