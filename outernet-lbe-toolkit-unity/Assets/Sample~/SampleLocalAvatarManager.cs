using UnityEngine;
using System;
using ObserveThing;

namespace Outernet.LBEToolkit.Sample
{
    public class SampleLocalAvatarManager : SampleManager
    {
        private Transform _camera;
        private SceneAvatarState _avatar;
        private SceneTransformState _transform;
        private IDisposable _subscriptions;

        public override void Setup(SampleState state)
        {
            _camera = Camera.main.transform;

            var avatarId = state.players
                .ObservableTrack(App.State.playerId)
                .ObservableSelect(x => x.keyPresent ? x.value.avatarId.AsObservable() : new ObservableValue<int>(0));

            _subscriptions = new ComposedDisposable(
                state.avatars.ObservableTrack(avatarId).Subscribe(x => _avatar = x.value),
                state.transforms.ObservableTrack(avatarId).Subscribe(x => _transform = x.value)
            );
        }

        public override void Teardown()
        {
            _subscriptions?.Dispose();
        }

        public override void WriteInitialStateValues(SampleState state) { }

        private void LateUpdate()
        {
            if (_avatar == null || _transform == null)
                return;

            var forward = _camera.forward;
            forward.y = 0;
            forward = forward.normalized;

            var avatarPosition = _camera.position;
            var avatarRotation = Quaternion.LookRotation(forward, Vector3.up);

            var headRotation = Quaternion.Inverse(avatarRotation) * _camera.rotation;

            var localTo = Matrix4x4.TRS(avatarPosition, avatarRotation, Vector3.one);

            Transform leftHand = default;

            if (SceneReferences.LeftController.gameObject.activeInHierarchy)
            {
                leftHand = SceneReferences.LeftController;
            }
            else if (SceneReferences.LeftPalm.gameObject.activeInHierarchy)
            {
                leftHand = SceneReferences.LeftPalm;
            }

            var leftHandActive = leftHand != null;
            var leftHandLocalPosition = leftHandActive ? localTo.inverse.MultiplyPoint3x4(leftHand.position) : default;
            var leftHandLocalRotation = leftHandActive ? Quaternion.Inverse(avatarRotation) * leftHand.rotation : default;

            Transform rightHand = default;

            if (SceneReferences.RightController.gameObject.activeInHierarchy)
            {
                rightHand = SceneReferences.RightController;
            }
            else if (SceneReferences.RightPalm.gameObject.activeInHierarchy)
            {
                rightHand = SceneReferences.RightPalm;
            }

            var rightHandActive = rightHand != null;
            var rightHandLocalPosition = rightHandActive ? localTo.inverse.MultiplyPoint3x4(rightHand.position) : default;
            var rightHandLocalRotation = rightHandActive ? Quaternion.Inverse(avatarRotation) * rightHand.rotation : default;

            Transactions.Execute(
                silentLogGroups: LogGroup.Stateful,
                () =>
                {
                    _transform.localPosition.value = avatarPosition;
                    _transform.localRotation.value = avatarRotation;
                    _avatar.headLocalRotation.value = headRotation;
                    _avatar.leftHandTracked.value = leftHandActive;
                    _avatar.leftHandLocalPosition.value = leftHandLocalPosition;
                    _avatar.leftHandLocalRotation.value = leftHandLocalRotation;
                    _avatar.rightHandTracked.value = rightHandActive;
                    _avatar.rightHandLocalPosition.value = rightHandLocalPosition;
                    _avatar.rightHandLocalRotation.value = rightHandLocalRotation;
                }
            );
        }
    }
}