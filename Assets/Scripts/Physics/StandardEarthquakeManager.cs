using UnityEngine;
using Fusion;

namespace WebGLTest.Physics
{
    public enum EarthquakePattern
    {
        Epicentral, // 直下型
        Megathrust, // 海溝型（巨大地震）
    }

    /// <summary>
    /// Unity標準の物理演算（Rigidbody + NetworkRigidbody3D）を用いたリアルな地震シミュレーション。
    /// 震度と階層を指定することで、実際の地震に近い不規則な揺れ（長周期地震動など）を再現します。
    /// </summary>
    public class StandardEarthquakeManager : NetworkBehaviour
    {
        [Header("Realistic Earthquake Settings")]
        [Tooltip("Eキーで発生する地震のパターン")]
        public EarthquakePattern earthquakePattern = EarthquakePattern.Epicentral;

        [Tooltip("震度 (1.0 〜 7.0)。数値が大きいほど指数関数的に力が強くなります。")]
        [Range(1f, 7f)]
        public float seismicIntensity = 5.0f;
        
        [Tooltip("建物の階層。高層階ほど揺れがゆっくりになり（長周期）、振幅が大きくなります。")]
        [Range(1, 50)]
        public int buildingFloor = 1;

        [Header("Runtime State (Sync)")]
        [Tooltip("Eキーでパターン地震発生")]
        [Networked] public NetworkBool IsQuaking { get; set; }
        
        [Networked] public float QuakeStartTime { get; set; }

        [Tooltip("TキーでON/OFFを切り替える単純な横揺れモード")]
        [Networked] public NetworkBool IsSimpleQuaking { get; set; }

        private struct InitialState
        {
            public Vector3 position;
            public Quaternion rotation;
        }

        private Rigidbody[] _rigidbodies;
        private InitialState[] _initialStates;
        private bool _triggerPatternEarthquake = false;
        private bool _triggerReset = false;

        public override void Spawned()
        {
            // WebGLのフレームレートを明示的に60に固定する
            Application.targetFrameRate = 60;

            if (Object.HasStateAuthority)
            {
                // シーン内のすべてのRigidbodyを取得し、初期状態を保存する
                _rigidbodies = FindObjectsOfType<Rigidbody>();
                _initialStates = new InitialState[_rigidbodies.Length];
                
                for (int i = 0; i < _rigidbodies.Length; i++)
                {
                    _initialStates[i] = new InitialState
                    {
                        position = _rigidbodies[i].position,
                        rotation = _rigidbodies[i].rotation
                    };
                }
                Debug.Log($"StandardEarthquakeManager: Found {_rigidbodies.Length} Rigidbodies for simulation. Initial states saved.");
            }
        }

        private void Update()
        {
            if (Object != null && Object.HasStateAuthority)
            {
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    if (UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame) IsSimpleQuaking = !IsSimpleQuaking;
                    if (UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame) _triggerPatternEarthquake = true;
                    if (UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame) _triggerReset = true;
                }
#else
                if (Input.GetKeyDown(KeyCode.T)) IsSimpleQuaking = !IsSimpleQuaking;
                if (Input.GetKeyDown(KeyCode.E)) _triggerPatternEarthquake = true;
                if (Input.GetKeyDown(KeyCode.R)) _triggerReset = true;
#endif
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority || _rigidbodies == null) return;

            // Rキー入力の処理（リセット）
            if (_triggerReset)
            {
                IsQuaking = false;
                IsSimpleQuaking = false;
                _triggerReset = false;

                for (int i = 0; i < _rigidbodies.Length; i++)
                {
                    if (_rigidbodies[i] == null) continue;
                    
                    var rb = _rigidbodies[i];
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.position = _initialStates[i].position;
                    rb.rotation = _initialStates[i].rotation;
                }
                return; // リセットしたフレームは地震の力を加えない
            }

            // Eキー入力の処理
            if (_triggerPatternEarthquake)
            {
                IsQuaking = true; 
                QuakeStartTime = Runner.SimulationTime; // 開始時刻をリセット
                _triggerPatternEarthquake = false;
            }

            // どちらの地震もOFFなら何もしない
            if (!IsQuaking && !IsSimpleQuaking) return;

            float time = Runner.SimulationTime;
            float elapsedTime = time - QuakeStartTime;

            // 階数による減衰・増幅の計算
            float floorMultiplier = 1f + (buildingFloor * 0.1f); 
            float frequencyMultiplier = Mathf.Clamp(1f - (buildingFloor * 0.02f), 0.2f, 1f);
            float baseForce = Mathf.Pow(seismicIntensity, 2f) * 10f; 

            float verticalMultiplier = 0f;
            float pWaveMultiplier = 0f;
            float sWaveMultiplier = 0f;

            if (IsQuaking)
            {
                // --- Eキーによるフェーズ計算（パターンごと） ---
                switch (earthquakePattern)
                {
                    case EarthquakePattern.Epicentral: // 直下型
                        // 0〜0.2秒: ﾄﾞﾝｯ!! という非常に強く鋭い突き上げ
                        if (elapsedTime < 0.2f) verticalMultiplier = 7f;
                        // 0.2〜3秒: P波（初期微動・カタカタ）
                        if (elapsedTime >= 0.2f && elapsedTime < 3f) pWaveMultiplier = 1.5f;
                        // 2秒〜12秒: S波（主要動・激しい横揺れ）
                        if (elapsedTime >= 2f && elapsedTime < 12f)
                        {
                            float normalized = (elapsedTime - 2f) / 10f;
                            sWaveMultiplier = Mathf.Sin(normalized * Mathf.PI); 
                        }
                        break;

                    case EarthquakePattern.Megathrust: // 海溝型
                        // 0〜8秒: 長いP波（遠くから来るカタカタ）
                        if (elapsedTime < 8f) pWaveMultiplier = 0.3f;
                        // 5〜30秒: 巨大で長いS波
                        if (elapsedTime >= 5f && elapsedTime < 30f)
                        {
                            float normalized = (elapsedTime - 5f) / 25f;
                            sWaveMultiplier = Mathf.Sin(normalized * Mathf.PI);
                        }
                        break;
                }

                // 終了判定
                if (elapsedTime > 35f)
                {
                    IsQuaking = false;
                }
            }
            
            if (IsSimpleQuaking)
            {
                // --- Tキーによる単純な横揺れ（常に一定の波） ---
                sWaveMultiplier = 1f; // S波をフルで適用し続ける
            }

            // --- 波の生成 ---
            // P波（速い微振動）
            float pWaveX = (Mathf.PerlinNoise(time * 25f, 0) - 0.5f) * pWaveMultiplier;
            float pWaveZ = (Mathf.PerlinNoise(0, time * 25f) - 0.5f) * pWaveMultiplier;
            
            // S波（大きな横揺れ）
            float sWaveX = (Mathf.Sin(time * 12f * frequencyMultiplier) * 0.5f + Mathf.Sin(time * 5f * frequencyMultiplier) * 1.0f) * sWaveMultiplier;
            float sWaveZ = (Mathf.Cos(time * 11f * frequencyMultiplier) * 0.5f + Mathf.Sin(time * 4f * frequencyMultiplier) * 1.0f) * sWaveMultiplier;

            // 縦揺れ（ﾄﾞﾝｯ!! という鋭い突き上げの波形を作るため高周波のSin波を使用）
            float sharpJolt = Mathf.Sin(elapsedTime * 80f) * verticalMultiplier;
            float verticalY = (Mathf.PerlinNoise(time * 15f, time * 15f) - 0.5f) * (pWaveMultiplier * 0.2f) + sharpJolt;

            Vector3 earthquakeForce = new Vector3(pWaveX + sWaveX, verticalY, pWaveZ + sWaveZ) * (baseForce * floorMultiplier);

            foreach (var rb in _rigidbodies)
            {
                if (rb == null) continue;

                if (!rb.isKinematic)
                {
                    // 重いオブジェクトも軽いオブジェクトと同じ加速度で揺れるようにmassを掛ける
                    rb.AddForce(earthquakeForce * rb.mass, ForceMode.Force);
                }
            }
        }
    }
}
