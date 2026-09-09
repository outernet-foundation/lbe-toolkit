using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Outernet.LBEToolkit.Sample
{
    [InitializeOnLoad]
    public class SampleSceneViewManifestHelper
    {
        static SampleSceneViewManifestHelper()
        {
            EditorSceneManager.sceneSaving += HandleSceneSaving;
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        }

        private static void HandlePlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
                UpdateSceneObjectViewManifests(SceneManager.GetActiveScene());
        }

        private static void HandleSceneSaving(Scene scene, string path)
        {
            UpdateSceneObjectViewManifests(scene);
        }

        private static void UpdateSceneObjectViewManifests(Scene scene)
        {
            SampleSceneViewManager sceneViewManager = null;

            foreach (var root in scene.GetRootGameObjects())
            {
                sceneViewManager = root.GetComponentInChildren<SampleSceneViewManager>(true);
                if (sceneViewManager != null)
                    break;

            }

            if (sceneViewManager != null)
                sceneViewManager.UpdateViewList();
        }
    }
}