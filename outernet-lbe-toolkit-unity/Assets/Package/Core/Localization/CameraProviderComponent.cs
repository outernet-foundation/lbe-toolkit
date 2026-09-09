using UnityEngine;
using Placeframe.Core;
using PlaceframeApiClient.Model;
using R3;

namespace Outernet.LBEToolkit.Localization
{
    public abstract class CameraProviderComponent : MonoBehaviour, ICameraProvider
    {
        public abstract Observable<PinholeCameraConfig> CameraConfig();
        public abstract Observable<CameraFrame> Frames(float intervalSeconds, bool useCameraPoseAnchoring = false);
    }
}