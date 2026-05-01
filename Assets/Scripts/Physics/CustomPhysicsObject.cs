using System.Collections.Generic;
using UnityEngine;
using Fusion;

namespace WebGLTest.Physics
{
    /// <summary>
    /// 各オブジェクト（机・椅子等）にアタッチし、インスペクターから物理演算設定を行うクラス。
    /// 旧Rigidbody・Colliderの代わりとなります。実際の物理演算は CustomPhysicsManager に委譲します。
    /// </summary>
    public class CustomPhysicsObject : MonoBehaviour
    {
        [Header("Physics Settings")]
        [Tooltip("衝突判定（XZ平面）として使用するローポリメッシュを設定してください。")]
        public Mesh collisionMesh;
        
        [Tooltip("オブジェクトの質量（重いほど他のオブジェクトを弾き飛ばします）")]
        public float mass = 10f;

        [Tooltip("倒れやすさのしきい値（高いほど揺れで倒れやすくなります）")]
        public float toppleThreshold = 5f;

        [HideInInspector]
        public int networkIndex = -1;

        // 抽出されたXZ平面の頂点リスト（ローカル座標）
        public Vector2[] LocalXZVertices { get; private set; }
        
        // 簡易衝突判定（円判定）用の半径
        public float BoundingRadius { get; private set; }

        private void Awake()
        {
            ExtractXZVertices();
        }

        /// <summary>
        /// 割り当てられたメッシュから、XZ平面での当たり判定用の頂点（Bounds）を抽出する
        /// </summary>
        private void ExtractXZVertices()
        {
            if (collisionMesh == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Collision Mesh が設定されていません。物理演算から除外されます。");
                LocalXZVertices = new Vector2[0];
                return;
            }

            // 複雑なメッシュの全頂点を計算するのは無駄なので、メッシュのBounds（外枠）から
            // XZ平面上の四角形（OBB）として近似する頂点を抽出します。
            // オブジェクトの実際のスケール（大きさ）を反映させる
            Bounds bounds = collisionMesh.bounds;
            Vector3 scale = transform.lossyScale;
            float extX = bounds.extents.x * scale.x;
            float extZ = bounds.extents.z * scale.z;

            float minX = -extX;
            float maxX = extX;
            float minZ = -extZ;
            float maxZ = extZ;

            List<Vector2> xzPoints = new List<Vector2>
            {
                new Vector2(minX, minZ),
                new Vector2(minX, maxZ),
                new Vector2(maxX, maxZ),
                new Vector2(maxX, minZ)
            };

            LocalXZVertices = xzPoints.ToArray();

            // 中心からの最大距離をBounding Radiusとする（簡易円判定用）
            BoundingRadius = Mathf.Max(extX, extZ);
        }

        /// <summary>
        /// サーバーの計算結果を受け取って描画用のTransformを更新する
        /// </summary>
        public void UpdateVisuals(Vector3 pos, Quaternion rot)
        {
            transform.position = pos;
            transform.rotation = rot;
        }
    }
}
