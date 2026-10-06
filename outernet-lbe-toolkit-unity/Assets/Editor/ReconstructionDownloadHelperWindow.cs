using UnityEngine;
using UnityEditor;
using System;
using Cysharp.Threading.Tasks;
using Placeframe.Core;
// using SimpleJSON;
using System.Linq;
using System.Collections.Generic;
using PlaceframeApiClient.Model;
using Placeframe.Auth;

namespace Outernet.LBEToolkit
{
    public class ReconstructionDownloadHelperWindow : EditorWindow
    {
        private string _authUrl;
        private string _apiUrl;
        private string _clientId;
        private string _username;
        private string _password;
        private string _destination;

        private struct ReconstructionData
        {
            public string name;
            public Guid id;
            public ReconstructionStatus status;
            public DateTime createdAt;
        }

        private int _reconstructionIndex = 0;
        private List<ReconstructionData> _reconstructions = new List<ReconstructionData>();

        [MenuItem("Window/Reconstruction Download Helper")]
        public static void ShowWindow()
        {
            GetWindow<ReconstructionDownloadHelperWindow>("Reconstruction Download Helper");
        }

        public void OnGUI()
        {
            _apiUrl = EditorGUILayout.TextField("Api Url", _apiUrl);
            _authUrl = EditorGUILayout.TextField("Auth Url", _authUrl);
            _clientId = EditorGUILayout.TextField("Client Id", _clientId);
            _username = EditorGUILayout.TextField("Username", _username);
            _password = EditorGUILayout.TextField("Password", _password);

            EditorGUILayout.BeginHorizontal();

            _reconstructionIndex = EditorGUILayout.Popup(
                "Reconstruction",
                _reconstructionIndex,
                _reconstructions.Where(x => x.status == ReconstructionStatus.Succeeded)
                    .Select(x => $"{x.name} [{x.id.ToString().Substring(0, 5)}]")
                    .ToArray()
            );

            if (GUILayout.Button("Refresh", GUILayout.MaxWidth(100)))
                RefreshReconstructions().Forget();

            EditorGUILayout.EndHorizontal();

            _destination = EditorGUILayout.TextField("Output", _destination);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = GUI.enabled && _reconstructionIndex < _reconstructions.Count;

            if (GUILayout.Button("Download & Save"))
                DownloadAndSave(_reconstructions[_reconstructionIndex].id, _destination).Forget();

            GUI.enabled = wasEnabled;
        }

        private async UniTask RefreshReconstructions()
        {
            await InitVPS();
            var reconstructions = await VisualPositioningSystem.Api.GetReconstructionsAsync();
            List<ReconstructionData> result = new List<ReconstructionData>();

            await UniTask.WhenAll(
                reconstructions
                    .Where(x => x.CaptureSessionId != null)
                    .Select(x => VisualPositioningSystem.Api.GetCaptureSessionAsync(x.CaptureSessionId.Value)
                        .AsUniTask()
                        .ContinueWith(c => result.Add(new()
                        {
                            id = x.Id,
                            name = c.Name,
                            status = x.Status,
                            createdAt = x.CreatedAt
                        }))
                    )
            );

            _reconstructions = result.OrderByDescending(x => x.createdAt).ToList();

            await UniTask.SwitchToMainThread();

            Repaint();
        }

        private async UniTask InitVPS()
        {
            if (VisualPositioningSystem.Initialized)
                return;

            var httpHandler = new TokenServerHttpHandler();
            await httpHandler.Login(_authUrl, _clientId, _username, _password);
            VisualPositioningSystem.Initialize(_apiUrl, new NoOpCameraProvider(), httpMessageHandler: httpHandler);
        }

        private async UniTask DownloadAndSave(Guid reconstructionID, string outputPath)
        {
            await InitVPS();

            var result = await VisualPositioningSystem.GetReconstructionPoints(reconstructionID);

            Mesh mesh = new Mesh();

            mesh.SetVertices(result.Select(x => x.position).ToArray());
            mesh.SetColors(result.Select(x => (Color)x.color).ToArray());

            var indices = result.Select((_, index) => index).ToArray();

            mesh.SetIndices(indices, MeshTopology.Points, 0);
            mesh.UploadMeshData(false);

            AssetDatabase.CreateAsset(mesh, $"Assets/{outputPath}.asset");
            AssetDatabase.Refresh();
        }
    }
}