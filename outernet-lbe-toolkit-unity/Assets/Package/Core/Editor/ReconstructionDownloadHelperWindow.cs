using UnityEngine;
using UnityEditor;
using System;
using Cysharp.Threading.Tasks;
using Placeframe.Core;
using System.Linq;
using Outernet.LBEToolkit.Authorization;

namespace Outernet.LBEToolkit
{
    public class ReconstructionDownloadHelperWindow : EditorWindow
    {
        private string _placeframeUrl;
        private string _tokenUrl;
        private string _clientId;
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
            _tokenUrl = EditorGUILayout.TextField("Token URL", _tokenUrl);
            _clientId = EditorGUILayout.TextField("Client ID", _clientId);
            _username = EditorGUILayout.TextField("Username", _username);
            _password = EditorGUILayout.TextField("Password", _password);
            _reconstructionID = EditorGUILayout.TextField("Reconstruction ID", _reconstructionID);
            _destination = EditorGUILayout.TextField("Output", _destination);

            if (GUILayout.Button("Download & Save"))
                DownloadAndSave(_placeframeUrl, _tokenUrl, _clientId, _username, _password, Guid.Parse(_reconstructionID), _destination).Forget();
        }


        private async UniTask DownloadAndSave(string placeframeUrl, string tokenUrl, string clientId, string username, string password, Guid reconstructionID, string outputPath)
        {
            var httpHandler = new TokenServerHttpHandler(
                Debug.Log,
                Debug.LogWarning,
                Debug.LogError
            );

            await httpHandler.Login(tokenUrl, clientId, username, password);

            VisualPositioningSystem.Initialize(
                placeframeUrl,
                default,
                Debug.Log,
                Debug.LogWarning,
                Debug.LogError,
                httpHandler
            );

            var result = await VisualPositioningSystem.GetReconstructionPoints(reconstructionID);

            Mesh mesh = new Mesh();

            mesh.SetVertices(result.Select(x => x.position).ToArray());
            mesh.SetColors(result.Select(x => (Color)x.color).ToArray());

            var indices = result.Select((_, index) => index).ToArray();

            mesh.SetIndices(indices, MeshTopology.Points, 0);
            mesh.UploadMeshData(false);

            AssetDatabase.CreateAsset(mesh, $"Assets/{outputPath}");
            AssetDatabase.Refresh();
        }
    }
}