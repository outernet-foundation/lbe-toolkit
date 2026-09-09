using UnityEngine;

namespace Outernet.LBEToolkit.Sample
{
    [RequireComponent(typeof(XRGrabbableControl))]
    public class XRGrabbableSceneView : SampleSceneView
    {
        private XRGrabbableControl _grabbable;

        public override void Setup(SampleState state, int objectId)
        {
            _grabbable = GetComponent<XRGrabbableControl>();

            var objState = state.objects[objectId];
            var transformState = state.transforms[objectId];

            _grabbable.Setup(new()
            {
                onGrabbed = () =>
                {
                    Transactions.Execute(
                        new SetObjectOwnerAction(state, objectId, App.State.playerId.value),
                        new AddHighFrequencyPrimitiveAction(state, transformState.localPosition.nodePath, 16),
                        new AddHighFrequencyPrimitiveAction(state, transformState.localRotation.nodePath, 16)
                    );
                },
                onReleased = () =>
                {
                    Transactions.Execute(
                        new RemoveHighFrequencyPrimitiveAction(state, transformState.localPosition.nodePath),
                        new RemoveHighFrequencyPrimitiveAction(state, transformState.localRotation.nodePath)
                    );
                }
            });
        }

        public override void Teardown()
        {
            _grabbable.Dispose();
        }

        public override void WriteInitialStateValues(SampleState state, int objectId) { }
    }
}