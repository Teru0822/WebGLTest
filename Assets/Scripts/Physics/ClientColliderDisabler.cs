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
            
            // Note: 以前はここで一度だけ isKinematic = true にしていましたが、
            // NetworkRigidbody3D がサーバーから isKinematic=false を同期して上書きしてしまうため、
            // Update等で継続的に無効化する必要があります。
            _rb = GetComponent<Rigidbody>();
        }

        private Rigidbody _rb;

        // 毎フレーム強制的にKinematicを維持し、Colliderがない状態で重力落下するのを防ぐ
        private void Update()
        {
            if (Object != null && !Object.HasStateAuthority && _rb != null)
            {
                if (!_rb.isKinematic)
                {
                    _rb.isKinematic = true;
                }
            }
        }
    }
}
