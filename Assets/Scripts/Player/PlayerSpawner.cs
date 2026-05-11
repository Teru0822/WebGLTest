using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System;
using System.Collections.Generic;
using WebGLTest.Network;
using WebGLTest.Platform;

namespace WebGLTest.Player
{
    /// <summary>
    /// クライアントの入力を収集し、サーバー側でプレイヤーをSpawnするクラス。
    /// NetworkRunnerと同じオブジェクトにアタッチされることを想定しています。
    /// </summary>
    public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
    {
        [Tooltip("プレイヤーのPrefab 'p' を割り当てます")]
        public NetworkPrefabRef playerPrefab;

        private const int MaxPlayers = 16;

        private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

        private Vector3[] _spawnPositions = new Vector3[MaxPlayers];
        private int _spawnIndex = 0;

        // Input state
        private Vector2 _moveInput;
        private bool _jumpInput;
        private float _yaw;
        private float _pitch;

        private bool _isMobile;
        private MobileTouchControls _mobileControls;

        private void Awake()
        {
            _isMobile = PlatformDetect.IsMobile();
            if (_isMobile)
            {
                var go = new GameObject("MobileTouchControls");
                go.transform.SetParent(transform, false);
                _mobileControls = go.AddComponent<MobileTouchControls>();
            }

            // Spawn1〜16の座標を取得
            for (int i = 0; i < MaxPlayers; i++)
            {
                GameObject spawnObj = GameObject.Find($"Spawn{i + 1}");
                if (spawnObj != null)
                {
                    _spawnPositions[i] = spawnObj.transform.position;
                }
                else
                {
                    // 見つからない場合のフォールバック座標
                    _spawnPositions[i] = new Vector3(i * 2f, 2f, 0);
                    Debug.LogWarning($"Spawn{i + 1} が見つかりません。デフォルトの座標を使用します。");
                }
            }

            // WebGLではユーザージェスチャ無しにPointer Lockを要求するとブラウザに拒否されるため、
            // 起動時には何もせず Update() 内のクリック検知でロックする。
        }

        private void Update()
        {
            // --- 入力の収集 (ローカルクライアントでのみ行われる) ---

            if (_isMobile)
            {
                // スマホ: タッチコントロールから読み取る
                if (_mobileControls != null)
                {
                    _moveInput = _mobileControls.MoveInput;
                    var look = _mobileControls.LookDelta;
                    _yaw += look.x;
                    _pitch -= look.y;
                }
                _pitch = Mathf.Clamp(_pitch, -85f, 85f);
                return;
            }

            // --- 以下はPC（キーボード+マウス）---
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                float x = 0; float y = 0;
                if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed) x += 1;
                if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed) x -= 1;
                if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed) y += 1;
                if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed) y -= 1;
                _moveInput = new Vector2(x, y).normalized;

                if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    _jumpInput = true;
                }
            }
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                var delta = UnityEngine.InputSystem.Mouse.current.delta.ReadValue();
                _yaw += delta.x * 0.25f;
                _pitch -= delta.y * 0.25f;

                // クリック（=ユーザージェスチャ）でカーソルロック。WebGLのPointer Lock制約対応。
                if (Cursor.lockState != CursorLockMode.Locked &&
                    UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
#else
            _moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _jumpInput = true;
            }

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            _yaw += mouseX * 4f;
            _pitch -= mouseY * 4f;

            if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
#endif

            // カメラの上下の回転を制限
            _pitch = Mathf.Clamp(_pitch, -85f, 85f);
            
            // ESCキーなどでカーソルロック解除
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
#else
            if (Input.GetKeyDown(KeyCode.Escape))
#endif
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        // サーバー側で各プレイヤーの最後に受信した入力を保持。OnInputMissing で再利用する。
        private Dictionary<PlayerRef, NetworkInputData> _lastReceivedInputs = new Dictionary<PlayerRef, NetworkInputData>();

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            var data = new NetworkInputData();
            data.direction = _moveInput;
            data.buttons.Set(PlayerInputButtons.Jump, _jumpInput);
            data.yaw = _yaw;
            data.pitch = _pitch;

            input.Set(data);

            // 送信したらジャンプ入力をリセット
            _jumpInput = false;
        }

        // 重要: tickごとの入力が届かなかった場合、Fusion はデフォルト(=ゼロ)の入力で進めてしまい、
        // サーバーから見たプレイヤーが半分の速度で動くなどの症状が出る。
        // 直前に受信した入力を再利用することで欠落耐性を持たせる。
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
            if (_lastReceivedInputs.TryGetValue(player, out var lastData))
            {
                input.Set(lastData);
            }
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer)
            {
                // 接続順でSpawn座標を決定
                Vector3 spawnPos = _spawnPositions[_spawnIndex % MaxPlayers];
                _spawnIndex++;

                // プレイヤーを生成
                NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
                _spawnedCharacters.Add(player, networkPlayerObject);
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
            {
                runner.Despawn(networkObject);
                _spawnedCharacters.Remove(player);
            }
        }

        // --- 使わないコールバック群 ---
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    }
}
