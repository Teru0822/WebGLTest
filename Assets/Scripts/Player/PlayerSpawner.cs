using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System;
using System.Collections.Generic;
using WebGLTest.Network;

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

        private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
        
        private Vector3[] _spawnPositions = new Vector3[4];
        private int _spawnIndex = 0;

        // Input state
        private Vector2 _moveInput;
        private bool _jumpInput;
        private float _yaw;
        private float _pitch;

        private void Awake()
        {
            // Spawn1〜4の座標を取得
            for (int i = 0; i < 4; i++)
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

            // WebGLやエディタ上でのマウスクリック時にカーソルをロックする
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            // --- 入力の収集 (ローカルクライアントでのみ行われる) ---

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
                _yaw += delta.x * 0.1f;
                _pitch -= delta.y * 0.1f;
            }
#else
            _moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _jumpInput = true;
            }

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            _yaw += mouseX * 2f;
            _pitch -= mouseY * 2f;
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

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer)
            {
                // 接続順でSpawn座標を決定
                Vector3 spawnPos = _spawnPositions[_spawnIndex % 4];
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
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
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
