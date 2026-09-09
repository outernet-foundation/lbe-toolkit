using UnityEngine;

namespace Outernet.LBEToolkit.Sample
{
    public abstract class SampleManager : MonoBehaviour
    {
        public abstract void Setup(SampleState state);
        public abstract void WriteInitialStateValues(SampleState state);
        public abstract void Teardown();
    }
}