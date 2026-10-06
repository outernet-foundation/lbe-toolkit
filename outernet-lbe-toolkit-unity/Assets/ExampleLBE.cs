using System;
using FofX.Stateful;
using ObserveThing;
using Outernet.LBEToolkit.Localization;
using Outernet.LBEToolkit.StateSynchronization;
using Outernet.LBEToolkit.Authorization;
using UnityEngine;
using Placeframe.Core;
using Cysharp.Threading.Tasks;
using System.Linq;

namespace Outernet.LBEToolkit.Example
{
    public class ExampleState : StateObject
    {
        public StateValue<bool> inRoomAndSynchronized { get; private set; }
        public SynchronizedState synchronizedState { get; private set; }
    }

    public class SynchronizedState : StateObject
    {
        public StateDictionary<int, PlayerData> players { get; private set; }
    }

    public class PlayerData : StateObject
    {
        public int playerId { get; private set; }
        public StateValue<string> name { get; private set; }
        public StateValue<Color> color { get; private set; }
        public StateValue<Vector3> position { get; private set; }
        public StateValue<Quaternion> rotation { get; private set; }
    }

    public class ExampleLBE : MonoBehaviour
    {
        public static ExampleState state { get; private set; }

        public string apiUrl;
        public AuthorizationProvider authorizationProvider;
        public CameraProviderComponent cameraProvider;
        public LocalizationMapProviderComponent localizationMapProvider;
        public RealtimeClientComponent realtimeClient;
        public float localizationInterval;

        private PeerToPeerStateSyncManager<SynchronizedState> _stateSyncManager;

        private void Awake()
        {
            AddSerializers();

            state = new ExampleState();
            state.Initialize(Settings.DefaultObservationContext, new DefaultLogger());
        }

        private void LateUpdate()
        {
            if (_stateSyncManager != null)
            {
                _stateSyncManager.SendHighFrequencySync();
                _stateSyncManager.SendIncrementalSync();
            }
        }

        private void AddSerializers()
        {
            JSONSerialization.AddSerializer(
                JSONSerializers.ToDouble2,
                JSONSerializers.ToJSON
            );

            JSONSerialization.AddSerializer(
                JSONSerializers.ToDouble3,
                JSONSerializers.ToJSON
            );

            JSONSerialization.AddSerializer(
                JSONSerializers.ToVector2,
                JSONSerializers.ToJSON
            );

            JSONSerialization.AddSerializer(
                JSONSerializers.ToVector3,
                JSONSerializers.ToJSON
            );

            JSONSerialization.AddSerializer(
                JSONSerializers.ToVector4,
                JSONSerializers.ToJSON
            );

            JSONSerialization.AddSerializer(
                JSONSerializers.ToQuaternion,
                JSONSerializers.ToJSON
            );

            JSONSerialization.AddSerializer(
                JSONSerializers.ToColor,
                JSONSerializers.ToJSON
            );

            JSONSerialization.AddSerializer(
                json => DateTime.Parse(json.Value),
                value => value.ToUniversalTime().ToString("O")
            );

            JSONSerialization.AddSerializer(
                x => JSONSerializers.ToQuaternion(x),
                x => JSONSerializers.ToJSON(x)
            );
        }

        public async UniTask InitializeVPS()
        {
            if (VisualPositioningSystem.Initialized)
                throw new Exception("VisualPositioningSystem has already been initialized!");

            if (!authorizationProvider.authorized)
                await authorizationProvider.Authorize();

            VisualPositioningSystem.Initialize(apiUrl, cameraProvider, httpMessageHandler: authorizationProvider.httpMessageHandler);
            VisualPositioningSystem.StartLocalizing(localizationInterval);
            VisualPositioningSystem.SetLocalizationMaps(localizationMapProvider.maps.ToArray());

            localizationMapProvider.onMapAdded += VisualPositioningSystem.AddLocalizationMap;
            localizationMapProvider.onMapRemoved += VisualPositioningSystem.RemoveLocalizationMap;
        }

        public async UniTask ConnectToRoom(string room)
        {
            await realtimeClient.Connect(room);
            _stateSyncManager = new PeerToPeerStateSyncManager<SynchronizedState>(state.synchronizedState, realtimeClient, "example");
            await _stateSyncManager.PerformInitialSync();
            state.inRoomAndSynchronized.value = true;
        }

        public async UniTask LeaveRoom()
        {
            state.inRoomAndSynchronized.value = false;
            _stateSyncManager.Dispose();
            await realtimeClient.Disconnect();
        }
    }
}