using System;
using FofX.Stateful;
using ObserveThing;
using Outernet.LBEToolkit.Localization;
using Outernet.LBEToolkit.StateSynchronization;
using Outernet.LBEToolkit.Authorization;
using UnityEngine;
using Placeframe.Core;
using Cysharp.Threading.Tasks;

namespace Outernet.LBEToolkit.Example
{
    public class ExampleState : StateObject
    {

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

        private PeerToPeerStateSyncManager<ExampleState> _stateSyncManager;
        private LBESession _lbeSession;

        private void Awake()
        {
            AddSerializers();

            state = new ExampleState();
            state.Initialize(Settings.DefaultObservationContext, new DefaultLogger());

            _stateSyncManager = new PeerToPeerStateSyncManager<ExampleState>(state, realtimeClient, "Example");
            _lbeSession = new LBESession(apiUrl, authorizationProvider, cameraProvider, realtimeClient, localizationMapProvider, _stateSyncManager, 1f);

            InitAndJoinRoom().Forget();
        }

        private async UniTask InitAndJoinRoom()
        {
            await _lbeSession.InitializeVpsAndStartLocalizing();
            await _lbeSession.ConnectToRoom("test room");
        }

        protected virtual void AddSerializers()
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
    }
}