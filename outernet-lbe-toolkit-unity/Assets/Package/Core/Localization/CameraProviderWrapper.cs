using Placeframe.Core;
using PlaceframeApiClient.Model;
using R3;

namespace Outernet.LBEToolkit.Localization
{
    public abstract class CameraProviderWrapper : CameraProviderComponent
    {
        private ICameraProvider _instance;

        private void Awake()
        {
            _instance = GetInstance();
        }

        protected abstract ICameraProvider GetInstance();

        public override Observable<PinholeCameraConfig> CameraConfig()
            => _instance.CameraConfig();

        public override Observable<CameraFrame> Frames(float intervalSeconds, bool useCameraPoseAnchoring = false)
            => _instance.Frames(intervalSeconds, useCameraPoseAnchoring);
    }
}