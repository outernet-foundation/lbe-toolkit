using UnityEngine;
using UnityEditor;

using System;
using System.Buffers.Binary;
using System.Linq;
using System.Collections.Generic;
using System.Buffers;
using System.Net.Http;
using System.Threading;
using System.IO;

using Cysharp.Threading.Tasks;

using PlaceframeApiClient.Model;
using Placeframe.Auth;
using PlaceframeApiClient.Api;
using PlaceframeApiClient.Client;

using R3;

using System.Runtime.InteropServices;
using Vector3 = UnityEngine.Vector3;

namespace Outernet.LBEToolkit
{
    public class ReconstructionDownloadHelperWindow : EditorWindow
    {
        private string _apiUrl;
        private string _username;
        private string _password;

        private struct ReconstructionData
        {
            public string name;
            public Guid id;
            public ReconstructionStatus status;
            public DateTime createdAt;
        }

        private readonly static string API_URL_PREFS_KEY = "OUTERNET_RDHW_API_URL";
        private readonly static string USERNAME_PREFS_KEY = "OUTERNET_RDHW_USERNAME";
        private readonly static string PASSWORD_PREFS_KEY = "OUTERNET_RDHW_PASSWORD";

        private bool _loginSelected;
        private bool _loginError;
        private DefaultApi _api;
        private int _reconstructionIndex = 0;
        private List<ReconstructionData> _reconstructions = new List<ReconstructionData>();

        private bool _loggedIn => _api != null;
        private bool _loggingIn => _loginSelected && !_loggedIn && !_loginError;

        public void OnEnable()
        {
            _loginSelected = false;
            _loginError = false;
        }

        [MenuItem("Window/Reconstruction Download Helper")]
        public static void ShowWindow()
        {
            GetWindow<ReconstructionDownloadHelperWindow>("Reconstruction Download Helper");
        }

        private string PrefsTextField(string key, string label)
        {
            var value = EditorPrefs.HasKey(key) ? EditorPrefs.GetString(key) : "";
            var newValue = EditorGUILayout.TextField(label, value);

            if (value != newValue)
                EditorPrefs.SetString(key, newValue);

            return newValue;
        }

        public void OnGUI()
        {
            bool wasEnabled = GUI.enabled;

            GUI.enabled = wasEnabled && !_loggedIn && !_loggingIn;

            EditorGUILayout.LabelField("Login");

            _apiUrl = PrefsTextField(API_URL_PREFS_KEY, "Api Url");
            _username = PrefsTextField(USERNAME_PREFS_KEY, "Username");
            _password = PrefsTextField(PASSWORD_PREFS_KEY, "Password");

            GUI.enabled = wasEnabled && !_loggingIn;

            if (_loginError)
            {
                var customLabelStyle = new GUIStyle(EditorStyles.label);
                customLabelStyle.alignment = TextAnchor.MiddleCenter;
                customLabelStyle.normal.textColor = Color.red;
                EditorGUILayout.LabelField("Login encountered an error. See console for details.", customLabelStyle);
            }

            if (_loggedIn)
            {
                if (GUILayout.Button("Log Out"))
                {
                    _loginSelected = false;
                    _loginError = false;
                    _api?.Dispose();
                    _api = null;
                    _reconstructions.Clear();
                }

            }
            else if (_loggingIn)
            {
                GUILayout.Button("Logging In...");
            }
            else
            {
                if (GUILayout.Button("Log In"))
                {
                    _loginError = false;
                    _loginSelected = true;
                    Login(_apiUrl, _username, _password).ContinueWith(x => _api = x).ContinueWith(_ => RefreshReconstructions()).Forget();
                }
            }

            GUI.enabled = wasEnabled && _loggedIn;

            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Download");

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

            GUI.enabled = GUI.enabled && _loggedIn && _reconstructionIndex < _reconstructions.Count;

            if (GUILayout.Button("Download"))
                DownloadAndSave(_reconstructions[_reconstructionIndex].id, _reconstructions[_reconstructionIndex].name).Forget();

            GUI.enabled = wasEnabled;
        }

        private async UniTask<DefaultApi> Login(string apiUrl, string username, string password)
        {
            try
            {
                var tempApi = new DefaultApi(
                    new HttpClient(new HttpClientHandler())
                    {
                        BaseAddress = new Uri(apiUrl),
                        Timeout = TimeSpan.FromSeconds(15),
                    },
                    new Configuration { BasePath = apiUrl, Timeout = TimeSpan.FromSeconds(15) }
                );

                var serverInfo = await tempApi.GetServerInfoAsync();

                HttpMessageHandler httpHandler = default;

                if (serverInfo.AuthMode == ServerInfo.AuthModeEnum.Disabled)
                {
                    httpHandler = new AnonymousIdentityHttpHandler(SystemInfo.deviceUniqueIdentifier) { InnerHandler = new HttpClientHandler() };
                }
                else
                {
                    var tokenServerHttpHandler = new TokenServerHttpHandler(
                        logInfo: Debug.Log,
                        logWarning: Debug.LogWarning,
                        logError: Debug.LogError
                    );

                    Debug.Log(serverInfo);

                    await tokenServerHttpHandler.Login(serverInfo.TokenUrl, serverInfo.Audience, username, password);

                    httpHandler = tokenServerHttpHandler;
                }

                return new DefaultApi(
                    new HttpClient(httpHandler)
                    {
                        BaseAddress = new Uri(apiUrl),
                        Timeout = Timeout.InfiniteTimeSpan
                    },
                    new Configuration()
                    {
                        BasePath = apiUrl,
                        Timeout = Timeout.InfiniteTimeSpan
                    }
                );
            }
            catch
            {
                _loginError = true;
                throw;
            }
        }

        private async UniTask RefreshReconstructions()
        {
            var reconstructions = await _api.GetReconstructionsAsync();
            List<ReconstructionData> result = new List<ReconstructionData>();

            await UniTask.WhenAll(
                reconstructions
                    .Where(x => x.CaptureSessionId != null)
                    .Select(x => _api.GetCaptureSessionAsync(x.CaptureSessionId.Value)
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

        private async UniTask DownloadAndSave(Guid reconstructionID, string reconstructionName)
        {
            var outputPath = EditorUtility.SaveFilePanelInProject("Save Reconstruction To", reconstructionName, "asset", "");

            if (string.IsNullOrEmpty(outputPath))
                return;

            var result = await GetReconstructionPoints(reconstructionID);

            Mesh mesh = new Mesh();

            mesh.SetVertices(result.Select(x => x.position).ToArray());
            mesh.SetColors(result.Select(x => (Color)x.color).ToArray());

            var indices = result.Select((_, index) => index).ToArray();

            mesh.SetIndices(indices, MeshTopology.Points, 0);
            mesh.UploadMeshData(false);

            AssetDatabase.CreateAsset(mesh, outputPath);
            AssetDatabase.Refresh();
        }

        private async UniTask<ReconstructionPoint[]> GetReconstructionPoints(Guid reconstructionID, CancellationToken cancellationToken = default)
        {
            var pointPayload = await FetchPayloadAsync(
                _api.GetReconstructionPointsAsync(reconstructionID, AxisConvention.UNITY).AsUniTask(),
                bytesPerElement: (3 * sizeof(float)) + 3,
                cancellationToken
            );

            return ParseReconstructionPointPayload(pointPayload);
        }

        private struct ReconstructionPoint
        {
            public Vector3 position;
            public Color32 color;
        }

        private ReconstructionPoint[] ParseReconstructionPointPayload(byte[] pointPayload)
        {
            var pointCount = (int)BinaryPrimitives.ReadUInt32LittleEndian(pointPayload.AsSpan(0, 4));
            var positionsByteCount = pointCount * 3 * sizeof(float);
            var positions = MemoryMarshal.Cast<byte, float>(pointPayload.AsSpan(4, positionsByteCount));
            var colors = pointPayload.AsSpan(4 + positionsByteCount, pointCount * 3);
            var points = new ReconstructionPoint[pointCount];

            for (var i = 0; i < points.Length; i++)
            {
                var index = i * 3;
                points[i] = new()
                {
                    position = new Vector3(positions[index + 0], positions[index + 1], positions[index + 2]),
                    color = new Color32(colors[index + 0], colors[index + 1], colors[index + 2], 255),
                };
            }

            return points;
        }

        private async UniTask<byte[]> FetchPayloadAsync(UniTask<FileParameter> responseTask, int bytesPerElement, CancellationToken cancellationToken = default)
        {
            var response = await responseTask;
            var stream = response.Content;
            try
            {
                var header = new byte[4];
                await ReadExactlyAsync(stream, header, 0, 4, cancellationToken);

                var count = (int)BinaryPrimitives.ReadUInt32LittleEndian(header);
                var payloadByteCount = 4 + (count * bytesPerElement);

                var payload = ArrayPool<byte>.Shared.Rent(payloadByteCount);
                Buffer.BlockCopy(header, 0, payload, 0, 4);
                await ReadExactlyAsync(stream, payload, 4, payloadByteCount - 4, cancellationToken);

                return payload;
            }
            finally
            {
                stream.Dispose();
            }
        }

        private async UniTask ReadExactlyAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            while (count > 0)
            {
                var read = await stream.ReadAsync(buffer, offset, count, cancellationToken);
                if (read == 0)
                    throw new EndOfStreamException();
                offset += read;
                count -= read;
            }
        }
    }
}