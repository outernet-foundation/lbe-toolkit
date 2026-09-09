using UnityEngine;
using ObserveThing;

namespace Outernet.LBEToolkit.Sample
{
    [RequireComponent(typeof(TransformControl))]
    public class TransformSceneView : SampleSceneView
    {
        private TransformControl _transform;

        private Vector3 GetLocalOrReferenceFramePosition(Vector3 localPosition)
            => transform.parent == null ? App.ReferenceFrame.InverseTransformPoint(localPosition) : localPosition;

        private Quaternion GetLocalOrReferenceFrameRotation(Quaternion localRotation)
            => transform.parent == null ? Quaternion.Inverse(App.ReferenceFrame.rotation) * localRotation : localRotation;

        public override void Setup(SampleState state, int objectId)
        {
            _transform = GetComponent<TransformControl>();

            var objectState = state.objects[objectId];
            var transformState = state.transforms[objectId];

            var smoothRate = objectState.isMine.ObservableSelect(x => x ? -1 : 16f);

            _transform.Setup(
                layout: new()
                {
                    localPosition = transformState.localPosition
                        .ObservableSelect(x => transform.parent == null ? App.ReferenceFrame.TransformPoint(x) : x)
                        .ObservableNetworkSmooth(smoothRate),
                    localRotation = transformState.localRotation
                        .ObservableSelect(x => transform.parent == null ? App.ReferenceFrame.rotation * x : x)
                        .ObservableNetworkSmooth(smoothRate),
                    localScale = transformState.localScale.ObservableNetworkSmooth(smoothRate), //assume uniform scale for App.ReferenceFrame
                },
                props: new()
                {
                    onLocalPositionChanged = x =>
                    {
                        if (objectState.isMine.value)
                            Transactions.Execute(silentLogGroups: LogGroup.Stateful, () => transformState.localPosition.value = GetLocalOrReferenceFramePosition(x));
                    },
                    onLocalRotationChanged = x =>
                    {
                        if (objectState.isMine.value)
                            Transactions.Execute(silentLogGroups: LogGroup.Stateful, () => transformState.localRotation.value = GetLocalOrReferenceFrameRotation(x));
                    },
                    onLocalScaleChanged = x =>
                    {
                        if (objectState.isMine.value)
                            Transactions.Execute(silentLogGroups: LogGroup.Stateful, () => transformState.localScale.value = x); //assume uniform scale for App.ReferenceFrame
                    }
                }
            );
        }

        public override void Teardown()
        {
            _transform.Dispose();
        }

        public override void WriteInitialStateValues(SampleState state, int objectId)
        {
            var transformState = state.transforms.GetOrAdd(objectId);
            transformState.localPosition.value = GetLocalOrReferenceFramePosition(transform.localPosition);
            transformState.localRotation.value = GetLocalOrReferenceFrameRotation(transform.localRotation);
            transformState.localScale.value = transform.localScale;
        }
    }
}