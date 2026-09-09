using UnityEngine;

namespace Outernet.LBEToolkit.Sample
{
    public abstract class SampleSceneView : MonoBehaviour
    {
        public abstract void Setup(SampleState state, int objectId);
        public abstract void WriteInitialStateValues(SampleState state, int objectId);
        public abstract void Teardown();
    }
}