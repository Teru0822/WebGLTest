using UnityEngine;
using Fusion;

namespace WebGLTest.Physics
{
    /// <summary>
    /// WebGLクライアント環境でのパフォーマンス低下を防ぐため、
    /// State Authorityを持たない（＝クライアントの）場合にColliderを無効化するスクリプト。
    /// これにより、Unityの物理エンジンが毎フレームMeshColliderのツリーを再計算するのを防ぎます。
    /// </summary>
    public class ClientColliderDisabler : NetworkBehaviour
    {
        public override void Spawned()
        {
            // 自分がサーバー（State Authority）であれば、物理演算を行うのでColliderは残す
            if (Object.HasStateAuthority) return;

            // クライアントであれば、物理演算はサーバーから送られてくる座標の補間のみで良いため、
            // 重たいColliderを無効化する。
            var colliders = GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                // Colliderを無効化することで、PhysXのSyncTransformsによる負荷を完全にカットする
                col.enabled = false;
            }
            
            // Note: RigidbodyのisKinematicはNetworkRigidbody3Dが自動的にtrueにしてくれますが、
            // 念のためここでも確実に物理挙動を切っておく
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }
    }
}
