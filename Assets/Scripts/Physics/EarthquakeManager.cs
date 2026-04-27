using UnityEngine;
using Fusion;

namespace WebGLTest.Physics
{
    /// <summary>
    /// 地震の揺れを発生させるサーバー専用スクリプト
    /// </summary>
    public class EarthquakeManager : NetworkBehaviour
    {
        [Header("Earthquake Settings")]
        [Tooltip("地震発生中かどうか。このプロパティは全クライアントに同期されます。")]
        [Networked] public NetworkBool IsQuaking { get; set; }

        [Tooltip("揺れの強さ")]
        public float intensity = 10f;

        [Tooltip("力を加える間隔（秒）")]
        public float forceInterval = 0.1f;

        private TickTimer _forceTimer;
        private bool _toggleEarthquake;
        private System.Collections.Generic.List<Rigidbody> _targetRigidbodies = new System.Collections.Generic.List<Rigidbody>();

        public override void Spawned()
        {
            if (!HasStateAuthority) return;

            // サーバー側で、揺らす対象となるRigidbodyを最初に一度だけ検索してキャッシュする
            // （FindObjectsOfTypeやGetComponentを毎チック呼ぶと重くなるため）
            var rigidbodies = FindObjectsOfType<Rigidbody>(); 
            foreach (var rb in rigidbodies)
            {
                var networkObj = rb.GetComponent<NetworkObject>();
                if (networkObj != null && !rb.isKinematic)
                {
                    _targetRigidbodies.Add(rb);
                }
            }
        }


        private void Update()
        {
            if (!HasStateAuthority) return;

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
            {
                _toggleEarthquake = true;
            }
#else
            if (UnityEngine.Input.GetKeyDown(KeyCode.T))
            {
                _toggleEarthquake = true;
            }
#endif
        }

        // サーバー（State Authority）のみで実行される毎チック処理
        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;

            // デバッグ用: Tキーで地震のOn/Offを切り替え（サーバー/ホスト実行時のみ）
            if (_toggleEarthquake)
            {
                IsQuaking = !IsQuaking;
                _toggleEarthquake = false;
            }

            if (IsQuaking)
            {
                if (_forceTimer.ExpiredOrNotRunning(Runner))
                {
                    ApplyEarthquakeForces();
                    _forceTimer = TickTimer.CreateFromSeconds(Runner, forceInterval);
                }
            }
        }

        private void ApplyEarthquakeForces()
        {
            if (_targetRigidbodies.Count == 0) return;

            foreach (var rb in _targetRigidbodies)
            {
                if (rb == null) continue;

                    // 地震らしい「横揺れ」メインの力を計算
                    Vector3 force = new Vector3(
                        UnityEngine.Random.Range(-1f, 1f),
                        UnityEngine.Random.Range(-0.1f, 0.2f), // 縦揺れ（Y軸）は極力控えめに
                        UnityEngine.Random.Range(-1f, 1f)
                    ).normalized * (intensity * rb.mass * 0.2f); // 質量を考慮し、全体的な強さを抑える

                    rb.AddForce(force, ForceMode.Impulse);

                    // 回転力（ガタガタ揺れる感じ）
                    Vector3 torque = new Vector3(
                        UnityEngine.Random.Range(-1f, 1f),
                        UnityEngine.Random.Range(-1f, 1f),
                        UnityEngine.Random.Range(-1f, 1f)
                    ).normalized * (intensity * rb.mass * 0.05f);

                    rb.AddTorque(torque, ForceMode.Impulse);
            }
        }
    }
}
