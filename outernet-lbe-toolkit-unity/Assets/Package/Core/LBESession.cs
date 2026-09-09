using System;
using System.Net.Http;
using Cysharp.Threading.Tasks;
using Placeframe.Core;
using Outernet.LBEToolkit.Localization;
using Outernet.LBEToolkit.StateSynchronization;
using System.Linq;

namespace Outernet.LBEToolkit
{
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Synchronized,
        Disconnecting
    }

    public class LBESession
    {
        public string apiUrl { get; }
        public ICameraProvider cameraProvider { get; }
        public IRealtimeClient realtimeClient { get; }
        public ILocalizationMapProvider localizationMapProvider { get; }
        public IStateSyncManager stateSynchronizationManager { get; }

        public ConnectionStatus connectionStatus { get; private set; } = ConnectionStatus.Disconnected;
        public event Action<ConnectionStatus> onConnectionStatusChanged;

        private bool _initialized;

        public LBESession(string apiUrl, ICameraProvider cameraProvider, IRealtimeClient realtimeClient, ILocalizationMapProvider localizationMapProvider, IStateSyncManager stateSynchronizationManager)
        {
            this.apiUrl = apiUrl;
            this.cameraProvider = cameraProvider;
            this.realtimeClient = realtimeClient;
            this.localizationMapProvider = localizationMapProvider;
            this.stateSynchronizationManager = stateSynchronizationManager;
        }

        private void SetConnectionStatus(ConnectionStatus connectionStatus)
        {
            if (this.connectionStatus == connectionStatus)
                return;

            this.connectionStatus = connectionStatus;
            onConnectionStatusChanged.Invoke(connectionStatus);
        }

        public void InitializeVpsAndStartLocalizing(float localizationInterval = 1f, HttpMessageHandler httpMessageHandler = default)
        {
            if (_initialized)
                throw new Exception("Initialize should only be called once");

            if (!VisualPositioningSystem.Localizing)
            {
                VisualPositioningSystem.Initialize(apiUrl, cameraProvider, httpMessageHandler: httpMessageHandler);
                VisualPositioningSystem.StartLocalizing(localizationInterval);
            }

            VisualPositioningSystem.SetLocalizationMaps(localizationMapProvider.maps.ToArray());

            localizationMapProvider.onMapAdded += VisualPositioningSystem.AddLocalizationMap;
            localizationMapProvider.onMapRemoved += VisualPositioningSystem.RemoveLocalizationMap;

            _initialized = true;
        }

        public async UniTask ConnectToRoom(string roomName)
        {
            if (connectionStatus == ConnectionStatus.Connecting || connectionStatus == ConnectionStatus.Connected)
                return;

            SetConnectionStatus(ConnectionStatus.Connecting);

            await realtimeClient.Connect(roomName);

            SetConnectionStatus(ConnectionStatus.Connected);

            await UniTask.WaitUntil(() => stateSynchronizationManager.synchronized);

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

        // protected virtual void AddSerializers()
        // {
        //     JSONSerialization.AddSerializer(
        //         new SerializationPair<double2>(
        //             JSONSerializers.ToDouble2,
        //             JSONSerializers.ToJSON
        //         )
        //     );

        //     JSONSerialization.AddSerializer(
        //         new SerializationPair<double3>(
        //             JSONSerializers.ToDouble3,
        //             JSONSerializers.ToJSON
        //         )
        //     );

        //     JSONSerialization.AddSerializer(
        //         new SerializationPair<Vector2>(
        //             JSONSerializers.ToVector2,
        //             JSONSerializers.ToJSON
        //         )
        //     );

        //     JSONSerialization.AddSerializer(
        //         new SerializationPair<Vector3>(
        //             JSONSerializers.ToVector3,
        //             JSONSerializers.ToJSON
        //         )
        //     );

        //     JSONSerialization.AddSerializer(
        //         new SerializationPair<Vector4>(
        //             JSONSerializers.ToVector4,
        //             JSONSerializers.ToJSON
        //         )
        //     );

        //     JSONSerialization.AddSerializer(
        //         new SerializationPair<Quaternion>(
        //             JSONSerializers.ToQuaternion,
        //             JSONSerializers.ToJSON
        //         )
        //     );


        //     JSONSerialization.AddSerializer(
        //         new SerializationPair<Color>(
        //             JSONSerializers.ToColor,
        //             JSONSerializers.ToJSON
        //         )
        //     );

        //     JSONSerialization.AddSerializer(
        //         new SerializationPair<DateTime>(
        //             json => DateTime.Parse(json.Value),
        //             value => value.ToUniversalTime().ToString("O")
        //         )
        //     );

        //     JSONSerialization.AddSerializer(
        //         new SerializationPair<quaternion>(
        //             x => JSONSerializers.ToQuaternion(x),
        //             x => JSONSerializers.ToJSON((Quaternion)x)
        //         )
        //     );
        // }
    }
}