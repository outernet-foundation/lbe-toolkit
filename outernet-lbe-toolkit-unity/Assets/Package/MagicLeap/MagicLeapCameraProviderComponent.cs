using Outernet.LBEToolkit.Localization;
using Placeframe.Core;
using Placeframe.Core.MagicLeap;

namespace Outernet.LBEToolkit.MagicLeap
{
    public class MagicLeapCameraProviderComponent : CameraProviderWrapper
    {
        protected override ICameraProvider GetInstance()
            => new MagicLeapCameraProvider();
    }
}