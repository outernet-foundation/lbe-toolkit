using System;
using System.Collections.Generic;
using System.Linq;
using ObserveThing;
using UnityEngine;

namespace Outernet.LBEToolkit.Sample
{
    public class SampleSceneViewManager : SampleManager
    {
        [SerializeField]
        private List<GameObject> _sceneViews = new List<GameObject>();

        private Dictionary<int, GameObject> _views = new Dictionary<int, GameObject>();
        private IDisposable _subscriptions;

        public override void Setup(SampleState state)
        {
            int id = -1;
            foreach (var view in _sceneViews)
            {
                if (view == null)
                {
                    id--;
                    continue;
                }

                foreach (var viewComponent in view.GetComponents<SampleSceneView>())
                    viewComponent.Setup(state, id);

                id--;
            }

            _subscriptions = state.objects
                .ObservableSelect(x => x.Value)
                .ObservableWhere(x => x.viewPrefab.ObservableSelect(x => !string.IsNullOrEmpty(x)))
                .ObservableSelect(obj => obj.viewPrefab.ObservableSelect(x => (id: obj.objectId, prefab: x)))
                .Subscribe(
                    onAdd: x =>
                    {
                        var view = AllocateView(x.prefab);
                        _views.Add(x.id, view);

                        foreach (var viewComponent in view.GetComponents<SampleSceneView>())
                            viewComponent.Setup(state, x.id);
                    },
                    onRemove: x =>
                    {
                        var view = _views[x.id];
                        _views.Remove(id);

                        foreach (var viewComponent in view.GetComponents<SampleSceneView>())
                            viewComponent.Teardown();

                        DeallocateView(view);
                    },
                    onDispose: () =>
                    {
                        foreach (var view in _views.Values)
                            DeallocateView(view);
                    }
                );
        }

        private GameObject AllocateView(string viewPrefab)
        {
            return Instantiate(Resources.Load<GameObject>(viewPrefab), transform);
        }

        private void DeallocateView(GameObject instance)
        {
            Destroy(instance);
        }

        public override void WriteInitialStateValues(SampleState state)
        {
            int id = -1;
            foreach (var view in _sceneViews)
            {
                if (view == null)
                {
                    id--;
                    continue;
                }

                state.objects.Add(id);

                foreach (var viewComponent in view.GetComponents<SampleSceneView>())
                    viewComponent.WriteInitialStateValues(state, id);

                id--;
            }
        }

        public override void Teardown()
        {
            _subscriptions?.Dispose();
        }

        public void UpdateViewList()
        {
            if (_sceneViews == null)
                _sceneViews = new List<GameObject>();

            var objects = gameObject.scene.GetRootGameObjects()
                .Where(x => x.GetComponentsInChildren<SampleSceneView>(true).Length > 0)
                .Select(x => x.gameObject)
                .Distinct();

            foreach (var view in objects)
            {
                if (_sceneViews.Contains(view))
                    continue;

                _sceneViews.Add(view);
            }
        }
    }
}