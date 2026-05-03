using Fusion;
using UnityEngine;

namespace WebGLTest.Network
{
    public enum PlayerInputButtons
    {
        Jump = 0,
    }

    public struct NetworkInputData : INetworkInput
    {
        public NetworkButtons buttons;
        public Vector2 direction;
        public float yaw;
        public float pitch;
    }
}
