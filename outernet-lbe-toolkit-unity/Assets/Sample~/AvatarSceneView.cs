using UnityEngine;
using ObserveThing;

namespace Outernet.LBEToolkit.Sample
{
    [RequireComponent(typeof(AvatarControl))]
    public class AvatarSceneView : SampleSceneView
    {
        private AvatarControl _avatar;

        public override void Setup(SampleState state, int objectId)
        {
            _avatar = GetComponent<AvatarControl>();

            var avatarState = state.avatars[objectId];
            var transformState = state.transforms[objectId];
            var isMine = state.objects[objectId].isMine;
            var smoothRate = isMine.ObservableSelect(x => x ? 0 : 16f);

            _avatar.Setup(
                layout: new()
                {
                    localPosition = transformState.localPosition.ObservableNetworkSmooth(smoothRate),
                    localRotation = transformState.localRotation.ObservableNetworkSmooth(smoothRate),
                },
                props: new()
                {
                    headLocalRotation = avatarState.headLocalRotation.ObservableNetworkSmooth(smoothRate),
                    leftHandTracked = avatarState.leftHandTracked,
                    leftHandLocalPosition = avatarState.leftHandLocalPosition.ObservableNetworkSmooth(smoothRate),
                    leftHandLocalRotation = avatarState.leftHandLocalRotation.ObservableNetworkSmooth(smoothRate),
                    rightHandTracked = avatarState.rightHandTracked,
                    rightHandLocalPosition = avatarState.rightHandLocalPosition.ObservableNetworkSmooth(smoothRate),
                    rightHandLocalRotation = avatarState.rightHandLocalRotation.ObservableNetworkSmooth(smoothRate),
                    maskActive = isMine.ObservableSelect(x => !x),
                    useMagicLeapMaskMaterial = App.State.platform.ObservableSelect(x => x == Platform.MagicLeap)
                }
            );
        }

        public override void Teardown()
        {
            _avatar.Dispose();
        }

        public override void WriteInitialStateValues(SampleState state, int objectId)
        {
            // avatars are never saved to the scene
            throw new System.NotImplementedException();
        }
    }
}