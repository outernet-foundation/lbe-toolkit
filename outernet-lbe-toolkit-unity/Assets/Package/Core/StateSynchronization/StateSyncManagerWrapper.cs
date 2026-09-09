namespace Outernet.LBEToolkit.StateSynchronization
{
    public abstract class StateSyncManagerWrapper : StateSyncManagerComponent
    {
        public override bool synchronized => _instance.synchronized;
        public override string topic => _instance.topic;

        private IStateSyncManager _instance;

        private void Awake()
        {
            _instance = GetInstance();
            _instance.onInitialSynchronizationComplete += RaiseOnInitialSynchronizationComplete;
        }

        protected abstract IStateSyncManager GetInstance();
    }
}