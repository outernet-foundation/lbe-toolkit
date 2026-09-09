using System;
using Cysharp.Threading.Tasks;
using Outernet.LBEToolkit.Localization;
using Outernet.LBEToolkit.StateSynchronization;
using Outernet.LBEToolkit.Authorization;
using UnityEngine;

namespace Outernet.LBEToolkit
{
    public class LBESessionManager : MonoBehaviour
    {
        public bool initialized { get; private set; }

        public string apiUrl;
        public AuthorizationProvider authorizationProvider;
        public CameraProviderComponent cameraProvider;
        public LocalizationMapProviderComponent localizationMapProvider;
        public RealtimeClientComponent realtimeClient;
        public StateSyncManagerComponent stateSynchronizationManager;
        public float localizationInterval;
        public bool joinRoomAutomatically;

        [ToggleGroup(nameof(joinRoomAutomatically))]
        public string roomToJoin;

        private LBESession _session;

        private void Awake()
        {
            Initialize().Forget();
        }

        private async UniTask Initialize()
        {
            _session = new LBESession(apiUrl, cameraProvider, realtimeClient, localizationMapProvider, stateSynchronizationManager);

            await UniTask.WaitUntil(() => authorizationProvider.authorized);

            _session.InitializeVpsAndStartLocalizing(localizationInterval, authorizationProvider.httpMessageHandler);

            if (joinRoomAutomatically)
                await _session.ConnectToRoom(roomToJoin);

            initialized = true;
        }

        public UniTask JoinRoom(string room)
        {
            if (!initialized)
                throw new Exception("Session must be initialized before calling JoinRoom");

            return _session.ConnectToRoom(room);
        }
    }
}