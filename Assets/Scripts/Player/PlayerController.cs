using Fusion;
using UnityEngine;
using WebGLTest.Network;

namespace WebGLTest.Player
{
    /// <summary>
    /// サーバーから送られてくる入力情報（NetworkInputData）を元にプレイヤーを移動・回転させるクラス。
    /// Rigidbodyによる物理演算とNetworkRigidbody3Dによる同期を前提としています。
    /// </summary>
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 12f;
        public float jumpForce = 7f;

        [Header("Camera")]
        [Tooltip("頭に付いているカメラオブジェクトのTransformを指定してください")]
        public Transform cameraTransform;

        [Header("Visuals")]
        [Tooltip("色を変更する対象のRendererを指定してください")]
        public Renderer playerRenderer;

        // ランダムな色を同期するためのプロパティ
        [Networked, OnChangedRender(nameof(OnColorChanged))]
        public Color PlayerColor { get; set; }

        private Rigidbody _rigidbody;
        private NetworkButtons _previousButtons;
        private float _initialCameraYaw;
        private float _initialCameraRoll;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (cameraTransform != null)
            {
                _initialCameraYaw = cameraTransform.localEulerAngles.y;
                _initialCameraRoll = cameraTransform.localEulerAngles.z;
            }

            // プレイヤーが物理演算で勝手に転がったり（X,Z回転）、衝突で回ったり（Y回転）しないよう回転を固定
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            // 地震で飛んでくる机などに弾き飛ばされないように質量を重くする
            _rigidbody.mass = 1000f;
        }

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                // サーバー側でランダムな色を決定
                PlayerColor = new Color(Random.value, Random.value, Random.value, 1f);
            }

            // カメラの有効化/無効化設定
            if (HasInputAuthority)
            {
                // 自分のプレイヤーは予測した最新tickをそのまま描画（補間ディレイを排除）。
                // RTTを跨ぐ往復を待たずに自分の入力が即反映されるようになる。
                Object.RenderSource = RenderSource.Latest;

                // 自分のプレイヤーの場合：カメラとAudioListenerを有効化
                if (cameraTransform != null)
                {
                    var cam = cameraTransform.GetComponent<Camera>();
                    if (cam != null) cam.enabled = true;
                    var audioListener = cameraTransform.GetComponent<AudioListener>();
                    if (audioListener != null) audioListener.enabled = true;
                }
            }
            else
            {
                // 他のプレイヤーの場合：カメラとAudioListenerを無効化（画面が上書きされるのを防ぐ）
                if (cameraTransform != null)
                {
                    var cam = cameraTransform.GetComponent<Camera>();
                    if (cam != null) cam.enabled = false;
                    var audioListener = cameraTransform.GetComponent<AudioListener>();
                    if (audioListener != null) audioListener.enabled = false;
                }
            }
            
            // 色を適用
            ApplyColor(PlayerColor);
        }

        public override void FixedUpdateNetwork()
        {
            // GetInputは、自分自身の入力権限（Input Authority）があるクライアントと、
            // サーバー（State Authority）の両方でtrueを返します。
            if (GetInput(out NetworkInputData data))
            {
                // --- 視点回転 (Yaw: プレイヤー本体のY軸回転, Pitch: カメラのX軸回転) ---
                transform.rotation = Quaternion.Euler(0, data.yaw, 0);
                if (cameraTransform != null)
                {
                    cameraTransform.localRotation = Quaternion.Euler(data.pitch, _initialCameraYaw, _initialCameraRoll);
                }

                // --- 移動 (WASD) ---
                Vector3 forwardDir = transform.forward;
                Vector3 rightDir = transform.right;

                // カメラがある場合はカメラの向いている方向（水平）を基準にする
                if (cameraTransform != null)
                {
                    forwardDir = cameraTransform.forward;
                    forwardDir.y = 0;
                    forwardDir.Normalize();

                    rightDir = cameraTransform.right;
                    rightDir.y = 0;
                    rightDir.Normalize();
                }

                Vector3 moveDirection = forwardDir * data.direction.y + rightDir * data.direction.x;
                moveDirection.Normalize();

                // 地震の力を殺さないために、水平方向の目標速度との差分をVelocityChangeで加える
                Vector3 targetVelocity = moveDirection * moveSpeed;
                Vector3 currentVelocity = _rigidbody.linearVelocity;
                Vector3 velocityChange = targetVelocity - new Vector3(currentVelocity.x, 0, currentVelocity.z);
                
                // 接地している（Y軸の速度がほぼ0）場合のみ自由に移動可能とする簡易的な制限
                if (Mathf.Abs(currentVelocity.y) < 0.1f)
                {
                    _rigidbody.AddForce(velocityChange, ForceMode.VelocityChange);
                }
                else
                {
                    // 空中では移動制御を弱める
                    _rigidbody.AddForce(velocityChange * 0.1f, ForceMode.VelocityChange);
                }

                // --- ジャンプ (Space) ---
                var pressed = data.buttons.GetPressed(_previousButtons);
                if (pressed.IsSet(PlayerInputButtons.Jump))
                {
                    // 簡易接地判定
                    if (Mathf.Abs(currentVelocity.y) < 0.1f)
                    {
                        _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
                    }
                }

                _previousButtons = data.buttons;
            }
        }

        void OnColorChanged()
        {
            ApplyColor(PlayerColor);
        }

        private void ApplyColor(Color color)
        {
            if (playerRenderer != null)
            {
                // MaterialPropertyBlockを使って、マテリアルを複製せずに色だけ変更する
                var block = new MaterialPropertyBlock();
                playerRenderer.GetPropertyBlock(block);
                block.SetColor("_Color", color); // Standard Shader
                block.SetColor("_BaseColor", color); // URP/HDRP
                playerRenderer.SetPropertyBlock(block);
            }
        }
    }
}
