using UnityEngine;
using Fusion;
using System.Threading.Tasks;

namespace WebGLTest.Network
{
    /// <summary>
    /// 起動環境を判定し、Photon FusionのNetworkRunnerをServerまたはClientとして起動するクラス
    /// </summary>
    public class GameStarter : MonoBehaviour
    {
        private NetworkRunner _runner;

        private bool _isStarting = false;

        private void Start()
        {
            Debug.Log("Ready. Press 'P' key to start Fusion...");
        }

        private void Update()
        {
            if (_isStarting) return;

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame)
            {
                _isStarting = true;
                StartSimulation();
            }
#else
            if (UnityEngine.Input.GetKeyDown(KeyCode.P))
            {
                _isStarting = true;
                StartSimulation();
            }
#endif
        }

        private async void StartSimulation()
        {
            // Runnerがなければアタッチ
            _runner = gameObject.GetComponent<NetworkRunner>();
            if (_runner == null)
            {
                _runner = gameObject.AddComponent<NetworkRunner>();
            }

            // ProvideInputはクライアントのみtrueにするのが一般的
            _runner.ProvideInput = true;

            // 起動モードの判定
            GameMode mode = GameMode.Client;

#if UNITY_SERVER || UNITY_EDITOR
            // 専用サーバービルド、またはエディタで特定の引数/設定がある場合はサーバーモードにする
            // ここでは簡易的に、エディタで実行時はサーバーとして起動する例（適宜変更可能）
            // 実際のWebGLビルドでは UNITY_WEBGL が定義され、必ず Client になります
            if (Application.isBatchMode || Application.platform == RuntimePlatform.LinuxServer || Application.platform == RuntimePlatform.WindowsServer)
            {
                mode = GameMode.Server;
                _runner.ProvideInput = false; // サーバー自身は入力を送信しない
            }
            else
            {
                // エディタデバッグ用: サーバーかクライアントか選択できるようにする場合はここをカスタマイズ
                // 一旦エディタではHostモード（またはClientモード）でテストすることも検討
                mode = GameMode.Host; 
            }
#elif UNITY_WEBGL
            // WebGLは必ずClient
            mode = GameMode.Client;
#endif

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
            }
            else
            {
                Debug.LogError($"Failed to start Fusion: {result.ShutdownReason}");
            }
        }
    }
}
