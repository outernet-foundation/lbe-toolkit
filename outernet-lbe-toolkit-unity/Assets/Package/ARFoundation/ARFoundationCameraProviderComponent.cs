using Outernet.LBEToolkit.Localization;
using Placeframe.Core;
using Placeframe.Core.ARFoundation;
using UnityEngine.XR.ARFoundation;

namespace Outernet.LBEToolkit.ARFoundation
{
    public class ARFoundationCameraProviderComponent : CameraProviderWrapper
    {
        public ARCameraManager cameraManager;
        public ARAnchorManager anchorManager;

        protected override ICameraProvider GetInstance()
            => new CameraProvider(cameraManager, anchorManager);
    }
}