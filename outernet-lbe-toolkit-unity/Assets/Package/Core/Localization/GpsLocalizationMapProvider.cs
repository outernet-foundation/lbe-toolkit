using System.Threading;
using Cysharp.Threading.Tasks;
using Placeframe.Core;
using UnityEngine;
using System.Linq;

namespace Outernet.LBEToolkit.Localization
{
    public class GpsLocalizationMapProvider : LocalizationMapProviderComponent
    {
        public float mapLoadRadius = 1000;
        private CancellationTokenSource _updateMapsTaskCancellationTokenSource;

        private void Awake()
        {
            Input.location.Start();
            UpdateMaps(_updateMapsTaskCancellationTokenSource.Token).Forget();
        }

        private void OnDestroy()
        {
            _updateMapsTaskCancellationTokenSource?.Cancel();
            _updateMapsTaskCancellationTokenSource?.Dispose();
            _updateMapsTaskCancellationTokenSource = null;
        }

        private async UniTask UpdateMaps(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Convert cartographic coordinates to ECEF coordinates, and use the ENU frame at that location for orientation
                var ecefPosition = WGS84.CartographicToEcef(
                    CartographicCoordinates.FromLongitudeLatitudeHeight(
                        Input.location.lastData.longitude,
                        Input.location.lastData.latitude,
                        Input.location.lastData.altitude
                    )
                );

                var maps = await VisualPositioningSystem.GetLocalizationMaps(
                    positionX: ecefPosition.x,
                    positionY: ecefPosition.y,
                    positionZ: ecefPosition.z,
                    radius: mapLoadRadius,
                    cancellationToken: cancellationToken
                );

                if (cancellationToken.IsCancellationRequested)
                    break;

                SetMaps(maps.Select(x => x.Id).ToArray());
            }
        }
    }
}