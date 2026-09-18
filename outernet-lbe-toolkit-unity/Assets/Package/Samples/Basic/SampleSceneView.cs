using UnityEngine;

namespace Outernet.LBEToolkit.Sample
{
    public abstract class SampleSceneView : MonoBehaviour
    {
        public abstract void Setup(SampleState state, int id);
        public abstract void Teardown();
    }
}