using UnityEngine;
using UnityEditor;
using System;
using Cysharp.Threading.Tasks;
using Placeframe.Core;
using SimpleJSON;
using System.Linq;

namespace Outernet.LBEToolkit
{
    public class ReconstructionDownloadHelperWindow : EditorWindow
    {
        private string _placeframeUrl;
        private string _username;
        private string _password;
        private string _reconstructionID;
        private string _destination;
        private bool _initialized;

        [MenuItem("Window/Reconstruction Download Helper")]
        public static void ShowWindow()
        {
            GetWindow<ReconstructionDownloadHelperWindow>("Reconstruction Download Helper");
        }

        public void OnGUI()
        {
            _placeframeUrl = EditorGUILayout.TextField("Placeframe URL", _placeframeUrl);
            _username = EditorGUILayout.TextField("Username", _username);
            _password = EditorGUILayout.TextField("Password", _password);
            _reconstructionID = EditorGUILayout.TextField("Reconstruction ID", _reconstructionID);
            _destination = EditorGUILayout.TextField("Output", _destination);

            // if (GUILayout.Button("Download & Save"))
            //     DownloadAndSave(_placeframeUrl, _username, _password, Guid.Parse(_reconstructionID), _destination).Forget();
        }

        // private async UniTask DownloadAndSave(string placeframeUrl, string username, string password, Guid reconstructionID, string outputPath)
        // {
        //     if (!Auth.Initialized)
        //     {
        //         VisualPositioningSystem.Initialize(
        //             new NoOpCameraProvider(),
        //             x => Debug.Log(x),
        //             x => Debug.LogWarning(x),
        //             x => Debug.LogError(x)
        //         );

        //         var serverInfo = await VisualPositioningSystem.Discover(placeframeUrl);
        //         await VisualPositioningSystem.Login(placeframeUrl, serverInfo, username, password);
        //     }

        //     var result = await VisualPositioningSystem.GetReconstructionPoints(reconstructionID);


        //     Mesh mesh = new Mesh();

        //     mesh.SetVertices(result.Select(x => x.position).ToArray());
        //     mesh.SetColors(result.Select(x => (Color)x.color).ToArray());

        //     var indices = result.Select((_, index) => index).ToArray();

        //     mesh.SetIndices(indices, MeshTopology.Points, 0);
        //     mesh.UploadMeshData(false);

        //     AssetDatabase.CreateAsset(mesh, $"Assets/{outputPath}");
        //     AssetDatabase.Refresh();
        // }
    }
}