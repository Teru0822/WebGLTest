using UnityEngine;
using Fusion;

namespace WebGLTest.Physics
{
    /// <summary>
    /// サーバー上で処理される各オブジェクト（机・椅子等）の物理演算用の状態を保持する構造体
    /// INetworkStruct を実装することで、Photon FusionのNetworkArrayで同期可能になります。
    /// </summary>
    public struct CustomPhysicsData : INetworkStruct
    {
        public Vector3 Position;          // XZ平面での位置
        public Quaternion Rotation;       // 現在の角度
        public byte State;                // 0: 立っている (Upright), 1: 倒れかけ (Toppling), 2: 完全に倒れた (Fallen)
    }
}
