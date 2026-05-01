using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;

namespace WebGLTest.Physics
{
    /// <summary>
    /// 全てのCustomPhysicsObjectを一元管理し、サーバー側で数学的な物理演算を実行して配列同期を行うマネージャー。
    /// </summary>
    public class CustomPhysicsManager : NetworkBehaviour
    {
        private const int MAX_OBJECTS = 1000;

        // 全オブジェクトの座標・回転情報を同期する巨大な配列 (クライアントへ同期)
        [Networked, Capacity(MAX_OBJECTS)]
        public NetworkArray<CustomPhysicsData> physicsObjects { get; }

        [Networked]
        public int objectCount { get; set; }

        [Header("Earthquake Settings (Prototype)")]
        [Tooltip("揺れの強さ（加速度の最大値）")]
        public float shakeIntensity = 15f;
        
        [Tooltip("揺れの速さ（周波数）")]
        public float shakeFrequency = 5f;
        
        [Tooltip("オブジェクトが倒れ始める加速度のしきい値")]
        public float globalToppleThreshold = 10f;

        // クライアントには不要な、サーバー専用の物理演算パラメータ群
        private struct ServerPhysicsData
        {
            public Vector3 Velocity;
            public Vector3 AngularVelocity;
            public float Mass;
            public float BoundingRadius;
            public int ShapeIndex;
        }

        // サーバー上でのみ保持される物理データ配列
        private ServerPhysicsData[] _serverData = new ServerPhysicsData[MAX_OBJECTS];

        private CustomPhysicsObject[] _localVisualObjects;
        private List<Vector2[]> _shapeTemplates = new List<Vector2[]>();

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                InitializePhysicsObjects();
            }
        }

        private void InitializePhysicsObjects()
        {
            // シーン内のすべてのCustomPhysicsObjectを取得
            var objects = FindObjectsOfType<CustomPhysicsObject>();
            
            // クライアント間で配列のインデックスがずれないよう、名前でソートして一意性を担保する
            _localVisualObjects = objects.OrderBy(o => o.gameObject.name).ToArray();
            objectCount = Mathf.Min(_localVisualObjects.Length, MAX_OBJECTS);

            for (int i = 0; i < objectCount; i++)
            {
                var obj = _localVisualObjects[i];
                obj.networkIndex = i;

                // 形状テンプレートの登録・再利用（同じメッシュなら頂点データを共有）
                int shapeIdx = GetOrRegisterShape(obj.LocalXZVertices);

                // 同期データ（軽量化）
                var data = new CustomPhysicsData
                {
                    Position = obj.transform.position,
                    Rotation = obj.transform.rotation,
                    State = 0 // 立っている状態
                };
                physicsObjects.Set(i, data);

                // サーバー計算用データ（同期不要）
                _serverData[i] = new ServerPhysicsData
                {
                    Velocity = Vector3.zero,
                    AngularVelocity = Vector3.zero,
                    ShapeIndex = shapeIdx,
                    Mass = obj.mass,
                    BoundingRadius = obj.BoundingRadius
                };
            }
        }

        private int GetOrRegisterShape(Vector2[] vertices)
        {
            for (int i = 0; i < _shapeTemplates.Count; i++)
            {
                // 頂点数が同じで、形状がほぼ一致していれば既存のテンプレートを再利用
                if (_shapeTemplates[i].Length == vertices.Length && 
                    vertices.Length > 0 &&
                    Vector2.Distance(_shapeTemplates[i][0], vertices[0]) < 0.01f)
                {
                    return i;
                }
            }
            _shapeTemplates.Add(vertices);
            return _shapeTemplates.Count - 1;
        }

        [Networked]
        public NetworkBool isSpinMode { get; set; }

        private void Update()
        {
            if (Object != null && Object.HasStateAuthority)
            {
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
                {
                    isSpinMode = !isSpinMode;
                }
#else
                if (Input.GetKeyDown(KeyCode.T))
                {
                    isSpinMode = !isSpinMode;
                }
#endif
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;

            float dt = Runner.DeltaTime;
            
            // 通常モードの共通の地震加速度
            Vector3 earthquakeAccel = Vector3.zero;
            if (!isSpinMode)
            {
                earthquakeAccel = new Vector3(Mathf.Sin(Runner.SimulationTime * shakeFrequency) * shakeIntensity, 0, 0);
            }

            for (int i = 0; i < objectCount; i++)
            {
                var data = physicsObjects[i];
                var sData = _serverData[i];

                if (isSpinMode)
                {
                    // ぐるぐるモード：個々のオブジェクトがその場でY軸回転（コマのように回る）
                    sData.AngularVelocity = new Vector3(0, shakeIntensity * 30f, 0);
                    
                    // ランダムに少し動かして衝突演算をテストしやすくする
                    float noiseX = Mathf.PerlinNoise(data.Position.x * 10f, Runner.SimulationTime * shakeFrequency) - 0.5f;
                    float noiseZ = Mathf.PerlinNoise(data.Position.z * 10f, Runner.SimulationTime * shakeFrequency) - 0.5f;
                    sData.Velocity += new Vector3(noiseX, 0, noiseZ) * (shakeIntensity * 2f) * dt;
                }
                else
                {
                    sData.Velocity += earthquakeAccel * dt;
                }
                
                // 床との摩擦による減衰
                sData.Velocity = Vector3.Lerp(sData.Velocity, Vector3.zero, dt * 2f); 

                // 2. 位置の更新 (地面をすり抜けないよう、Y軸の速度は絶対に反映させない)
                data.Position += new Vector3(sData.Velocity.x, 0, sData.Velocity.z) * dt;

                // 3. 回転と倒れ判定
                if (isSpinMode)
                {
                    // ぐるぐるモード中はY軸で回転し続ける
                    Quaternion rotChange = Quaternion.Euler(sData.AngularVelocity * dt);
                    data.Rotation = data.Rotation * rotChange;
                }
                else
                {
                    if (data.State == 0)
                    {
                        // 加速度がしきい値を超えたら倒れ始める
                        if (earthquakeAccel.magnitude > globalToppleThreshold && Random.value < 0.05f) 
                        {
                            data.State = 1; // 倒れかけ
                            sData.AngularVelocity = new Vector3(180f, 0, 0); // X軸に回転して倒れる
                        }
                    }

                    if (data.State == 1)
                    {
                        Quaternion rotChange = Quaternion.Euler(sData.AngularVelocity * dt);
                        data.Rotation = data.Rotation * rotChange;

                        // Z軸（上方向）から約90度傾いたら完全に倒れたと判定
                        if (Vector3.Angle(Vector3.up, data.Rotation * Vector3.up) >= 85f)
                        {
                            data.State = 2; // 完全に倒れた
                            sData.AngularVelocity = Vector3.zero; // 回転停止
                        }
                    }
                }

                physicsObjects.Set(i, data);
                _serverData[i] = sData;
            }

            // 5. XZ平面の衝突・めり込み解消 (プロトタイプとして円判定による簡易版SAT)
            ResolveCollisions();
        }

        private void ResolveCollisions()
        {
            for (int i = 0; i < objectCount; i++)
            {
                var dataA = physicsObjects[i];
                var sDataA = _serverData[i];

                for (int j = i + 1; j < objectCount; j++)
                {
                    var dataB = physicsObjects[j];
                    var sDataB = _serverData[j];

                    Vector2 posA = new Vector2(dataA.Position.x, dataA.Position.z);
                    Vector2 posB = new Vector2(dataB.Position.x, dataB.Position.z);
                    
                    float dist = Vector2.Distance(posA, posB);
                    float minDist = sDataA.BoundingRadius + sDataB.BoundingRadius;

                    // めり込んでいたら
                    if (dist < minDist && dist > 0)
                    {
                        float overlap = minDist - dist;
                        Vector2 dir = (posA - posB).normalized;

                        // 質量に応じた押し返し比率
                        float totalMass = sDataA.Mass + sDataB.Mass;
                        float ratioA = sDataB.Mass / totalMass;
                        float ratioB = sDataA.Mass / totalMass;

                        dataA.Position += new Vector3(dir.x * overlap * ratioA, 0, dir.y * overlap * ratioA);
                        dataB.Position -= new Vector3(dir.x * overlap * ratioB, 0, dir.y * overlap * ratioB);

                        // 反発係数による速度の交換（簡易的）
                        Vector3 tempVel = sDataA.Velocity;
                        sDataA.Velocity = sDataB.Velocity * 0.8f;
                        sDataB.Velocity = tempVel * 0.8f;

                        physicsObjects.Set(i, dataA);
                        physicsObjects.Set(j, dataB);
                        _serverData[i] = sDataA;
                        _serverData[j] = sDataB;
                    }
                }
            }
        }

        public override void Render()
        {
            // クライアント側でNetworkArrayの内容を読み取り、対応するオブジェクトのTransformを更新する
            if (_localVisualObjects == null || _localVisualObjects.Length == 0)
            {
                var objects = FindObjectsOfType<CustomPhysicsObject>();
                _localVisualObjects = objects.OrderBy(o => o.gameObject.name).ToArray();
            }

            for (int i = 0; i < objectCount; i++)
            {
                if (i < _localVisualObjects.Length)
                {
                    var data = physicsObjects[i];
                    _localVisualObjects[i].UpdateVisuals(data.Position, data.Rotation);
                }
            }
        }
    }
}
