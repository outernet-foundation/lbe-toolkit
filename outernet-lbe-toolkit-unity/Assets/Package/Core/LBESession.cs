using System;
using System.Net.Http;
using Cysharp.Threading.Tasks;
using Placeframe.Core;
using Outernet.LBEToolkit.Localization;
using Outernet.LBEToolkit.StateSynchronization;
using System.Linq;
using Outernet.LBEToolkit.Authorization;
using System.Threading.Tasks;

namespace Outernet.LBEToolkit
{
    public enum ConnectionStatus
    {
        Disconnected,
        Authorizing,
        Authorized,
        InitializingVps,
        VpsInitialized,
        Connecting,
        Connected,
        Synchronized,
        Disconnecting
    }

    public class LBESession : IDisposable
    {
        public string apiUrl { get; }
        public IAuthorizationProvider authorizationProvider { get; }
        public ICameraProvider cameraProvider { get; }
        public IRealtimeClient realtimeClient { get; }
        public ILocalizationMapProvider localizationMapProvider { get; }
        public IStateSyncManager stateSynchronizationManager { get; }
        public float localizationInterval { get; }

        public ConnectionStatus connectionStatus { get; private set; } = ConnectionStatus.Disconnected;
        public event Action<ConnectionStatus> onConnectionStatusChanged;

        private bool _initialized;

        public LBESession(string apiUrl, IAuthorizationProvider authorizationProvider, ICameraProvider cameraProvider, IRealtimeClient realtimeClient, ILocalizationMapProvider localizationMapProvider, IStateSyncManager stateSynchronizationManager, float localizationInterval = 1f)
        {
            this.apiUrl = apiUrl;
            this.authorizationProvider = authorizationProvider;
            this.cameraProvider = cameraProvider;
            this.realtimeClient = realtimeClient;
            this.localizationMapProvider = localizationMapProvider;
            this.stateSynchronizationManager = stateSynchronizationManager;
            this.localizationInterval = localizationInterval;
        }

        private void SetConnectionStatus(ConnectionStatus connectionStatus)
        {
            if (this.connectionStatus == connectionStatus)
                return;

            this.connectionStatus = connectionStatus;
            onConnectionStatusChanged.Invoke(connectionStatus);
        }

        public async UniTask InitializeVpsAndStartLocalizing()
        {
            if (_initialized)
                throw new Exception("Initialize should only be called once");

            _initialized = true;

            SetConnectionStatus(ConnectionStatus.Authorizing);

            var httpMessageHandler = await authorizationProvider.Authorize();

            SetConnectionStatus(ConnectionStatus.Authorized);

            if (!VisualPositioningSystem.Localizing)
            {
                SetConnectionStatus(ConnectionStatus.InitializingVps);

                VisualPositioningSystem.Initialize(apiUrl, cameraProvider, httpMessageHandler: httpMessageHandler);
                VisualPositioningSystem.StartLocalizing(localizationInterval);
            }

            VisualPositioningSystem.SetLocalizationMaps(localizationMapProvider.maps.ToArray());

            localizationMapProvider.onMapAdded += VisualPositioningSystem.AddLocalizationMap;
            localizationMapProvider.onMapRemoved += VisualPositioningSystem.RemoveLocalizationMap;

            SetConnectionStatus(ConnectionStatus.VpsInitialized);
        }

        public async UniTask ConnectToRoom(string roomName)
        {
            if (connectionStatus == ConnectionStatus.Connecting || connectionStatus == ConnectionStatus.Connected)
                return;

            SetConnectionStatus(ConnectionStatus.Connecting);

            await realtimeClient.Connect(roomName);

            SetConnectionStatus(ConnectionStatus.Connected);

            await stateSynchronizationManager.PerformInitialSync();

            SetConnectionStatus(ConnectionStatus.Synchronized);
        }

        public async UniTask DisconnectFromRoom()
        {
            if (connectionStatus == ConnectionStatus.Disconnecting || connectionStatus == ConnectionStatus.Disconnected)
                return;

            SetConnectionStatus(ConnectionStatus.Disconnecting);

            await realtimeClient.Disconnect();

            SetConnectionStatus(ConnectionStatus.Disconnected);
        }

        public void Dispose()
        {
            if (VisualPositioningSystem.Localizing)
                VisualPositioningSystem.StopLocalizing();

            localizationMapProvider.onMapAdded -= VisualPositioningSystem.AddLocalizationMap;
            localizationMapProvider.onMapRemoved -= VisualPositioningSystem.RemoveLocalizationMap;
        }
    }
}