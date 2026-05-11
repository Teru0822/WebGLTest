using UnityEngine;
using UnityEngine.UI;
using Fusion;
using System.Threading.Tasks;
using WebGLTest.Platform;

namespace WebGLTest.Network
{
    /// <summary>
    /// 起動環境を判定し、Photon FusionのNetworkRunnerをServerまたはClientとして起動するクラス。
    /// 編集時はHost、WebGLは自動でClientとしてサーバーへ接続。失敗時はリトライ。
    /// </summary>
    public class GameStarter : MonoBehaviour
    {
        [Tooltip("クライアントが接続失敗した時のリトライ間隔（秒）")]
        public float clientRetrySeconds = 3f;

        private NetworkRunner _runner;
        private bool _isStarting = false;
        private Text _statusText;

        private void Start()
        {
            ApplyPlatformResolution();
            CreateStatusUI();
            StartSimulation();
        }

        private void ApplyPlatformResolution()
        {
            // PCはデフォルトの横画面解像度のまま。スマホはブラウザのウィンドウサイズに合わせる。
            if (PlatformDetect.IsMobile())
            {
                // CSSピクセル基準でcanvasリサイズ（高DPI端末で過剰な負荷を避けるため）。
                int w = PlatformDetect.WindowWidthCss();
                int h = PlatformDetect.WindowHeightCss();
                if (w > 0 && h > 0)
                {
                    Screen.SetResolution(w, h, false);
                    Debug.Log($"[Mobile] Resolution set to {w}x{h} (CSS pixels)");
                }
            }
        }

        private async void StartSimulation()
        {
            if (_isStarting) return;
            _isStarting = true;

            // Runnerがなければアタッチ
            _runner = gameObject.GetComponent<NetworkRunner>();
            if (_runner == null)
            {
                _runner = gameObject.AddComponent<NetworkRunner>();
            }

            // RunnerSimulatePhysics3D をアタッチ（無ければ追加）。
            // 重要: 既定では ClientPhysicsSimulation = Disabled でクライアント側の物理が一切走らない。
            // 全ピア決定論シミュレーション設計に合わせて SimulateAlways を強制セットする。
            var physicsSim = gameObject.GetComponent<Fusion.Addons.Physics.RunnerSimulatePhysics3D>();
            if (physicsSim == null)
            {
                physicsSim = gameObject.AddComponent<Fusion.Addons.Physics.RunnerSimulatePhysics3D>();
            }
            physicsSim.ClientPhysicsSimulation = Fusion.Addons.Physics.ClientPhysicsSimulation.SimulateAlways;

            // パケットロスや通信帯域、Pingなどを画面に詳細表示するためのFusionStatisticsを追加
            // 注意: FusionStatisticsはEventSystemが無いと自動で古いStandaloneInputModuleを作ってしまいエラーになるため、
            // 新しいInput System用のモジュールを先に追加しておきます。
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }

            var stats = gameObject.GetComponent<Fusion.Statistics.FusionStatistics>();
            if (stats == null)
            {
                stats = gameObject.AddComponent<Fusion.Statistics.FusionStatistics>();

                // 動的追加時はデフォルトでグラフが1つも表示されない（0）設定になっているため、
                // リフレクションを使って必要なグラフ（RTT, Bandwidth, Packets等）をオンにします
                var field = stats.GetType().GetField("_statsEnabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    // RTT(4) | InBandwidth(8) | OutBandwidth(16) = 28
                    // Enum: InPackets=1, OutPackets=2, RTT=4, InBandwidth=8, OutBandwidth=16
                    field.SetValue(stats, 1 | 2 | 4 | 8 | 16);
                }
            }

            // ProvideInputはクライアントのみtrueにするのが一般的
            _runner.ProvideInput = true;

            // 起動モードの判定
            GameMode mode = GameMode.Client;

#if UNITY_SERVER || UNITY_EDITOR
            // 専用サーバービルド、またはエディタで特定の引数/設定がある場合はサーバーモードにする
            // 実際のWebGLビルドでは UNITY_WEBGL が定義され、必ず Client になります
            if (Application.isBatchMode || Application.platform == RuntimePlatform.LinuxServer || Application.platform == RuntimePlatform.WindowsServer)
            {
                mode = GameMode.Server;
                _runner.ProvideInput = false; // サーバー自身は入力を送信しない
            }
            else
            {
                // エディタはHostとして起動（=サーバー兼プレイヤー）。クライアントはこのHostへ接続する。
                mode = GameMode.Host;
            }
#elif UNITY_WEBGL
            mode = GameMode.Client;
#endif

            UpdateStatus($"Connecting as {mode}...");
            Debug.Log($"Starting Fusion in mode: {mode}");

            var sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(SceneRef.FromIndex(gameObject.scene.buildIndex), UnityEngine.SceneManagement.LoadSceneMode.Additive);

            // 起動
            var sceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>();
            if (sceneManager == null) sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

            var result = await _runner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = "EarthquakeSimRoom", // 固定ルーム名
                Scene = sceneInfo,
                SceneManager = sceneManager
            });

            if (result.Ok)
            {
                Debug.Log("Fusion started successfully.");
                UpdateStatus("Connected.");
                // 接続できたらしばらくして状態テキストを消す
                Invoke(nameof(HideStatus), 2f);
            }
            else
            {
                Debug.LogWarning($"Failed to start Fusion: {result.ShutdownReason}. Retrying in {clientRetrySeconds:0.0}s.");
                UpdateStatus($"Server not available ({result.ShutdownReason}). Retrying in {clientRetrySeconds:0.0}s...");

                // クライアントは再試行。サーバー/Hostは再試行しない（恐らく設定エラーなので）。
                if (mode == GameMode.Client)
                {
                    // 古いRunnerを破棄してリトライ
                    if (_runner != null) Destroy(_runner);
                    _runner = null;
                    _isStarting = false;
                    Invoke(nameof(StartSimulation), clientRetrySeconds);
                }
            }
        }

        private void CreateStatusUI()
        {
            var canvasObj = new GameObject("ConnectionStatusCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            var textObj = new GameObject("StatusText");
            textObj.transform.SetParent(canvasObj.transform, false);
            _statusText = textObj.AddComponent<Text>();
            _statusText.font = Font.CreateDynamicFontFromOSFont("Arial", 36);
            _statusText.fontSize = 36;
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.color = Color.white;
            _statusText.raycastTarget = false;
            var outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;

            var rt = _statusText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(1200, 200);
        }

        private void UpdateStatus(string msg)
        {
            if (_statusText != null) _statusText.text = msg;
        }

        private void HideStatus()
        {
            if (_statusText != null) _statusText.text = string.Empty;
        }
    }
}
