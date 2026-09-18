using UnityEngine;
using System;
using System.Collections.Generic;
using ObserveThing;

namespace Outernet.LBEToolkit.Sample
{
    public class SampleSceneViewManager : SampleManager
    {
        public Transform viewParent;

        private SampleState _state;
        private IDisposable _subscription;
        private Dictionary<int, GameObject> _views = new Dictionary<int, GameObject>();

        private void SetupView(int id, string viewPrefab)
        {
            var prefab = Resources.Load<GameObject>(viewPrefab);
            var instance = Instantiate(prefab, viewParent);

            foreach (var viewComponent in instance.GetComponents<SampleSceneView>())
                viewComponent.Setup(_state, id);

            _views.Add(id, instance);
        }

        private void TeardownView(int id)
        {
            var instance = _views[id];
            _views.Remove(id);

            foreach (var viewComponent in instance.GetComponents<SampleSceneView>())
                viewComponent.Teardown();

            Destroy(instance.gameObject);
        }

        public override void Setup(SampleState state)
        {
            _subscription = state.objects.Subscribe(
                onAdd: kvp => SetupView(kvp.Key, kvp.Value.viewPrefab.value),
                onRemove: kvp => TeardownView(kvp.Key)
            );
        }

        public override void WriteInitialStateValues(SampleState state)
        {

        }

        public override void Teardown()
        {
            _subscription?.Dispose();

            foreach (var instance in _views.Values)
            {
                foreach (var viewComponent in instance.GetComponents<SampleSceneView>())
                    viewComponent.Teardown();

                Destroy(instance.gameObject);
            }
        }
    }
}