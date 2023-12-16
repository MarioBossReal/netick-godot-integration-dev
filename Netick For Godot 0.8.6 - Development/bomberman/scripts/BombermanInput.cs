using Godot;
using Netick;
using Netick.GodotEngine;

namespace Netick.Samples.Bomberman
{
    public struct BombermanInput : INetworkInput
    {
        public Vector2 Movement;
        public bool    PlantBomb;
    }
}