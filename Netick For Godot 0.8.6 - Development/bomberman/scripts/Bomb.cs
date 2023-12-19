using Godot;
using Netick.GodotEngine;

namespace Netick.Samples.Bomberman
{
    [GlobalClass]
    public partial class Bomb : NetworkBehaviour<Node2D>
    {
        [Signal]
        public delegate void ExplodedEventHandler();

        //public BombermanController Bomber;
        public float ExplosionDelay = 3.0f;

        [Export]
        private Sprite2D _sprite2D;

        private readonly Vector2[] _directionsAroundBomb = new Vector2[4] { Vector2.Right, Vector2.Left, Vector2.Up, Vector2.Down };

        public override void _Ready()
        {
            //InitializeBaseNode();
        }

        public override void NetworkStart()
        {
        }
        public override void NetworkDestroy()
        {
        }

        public override void NetworkReset()
        {
            _sprite2D.Visible = true;
        }

        public override void NetworkFixedUpdate()
        {
            if (Sandbox.TickToTime(Sandbox.Tick - Object.SpawnTick) >= ExplosionDelay)
                Explode();
        }

        private void Explode()
        {
            // hide bomb after delay
            _sprite2D.Visible = false;

            // dealing damage is done on the server only
            if (IsServer)
                DamageTargetsAroundBomb(_sprite2D.GlobalPosition);

            // only the server can destroy the bomb
            if (IsServer || Id == -1)
                Sandbox.Destroy(Object);

            if (IsServer)
                EmitSignal(SignalName.Exploded);
        }

        private void DamageTargetsAroundBomb(Vector2 pos)
        {
            // find all objects around the bomb position
            foreach (var dir in _directionsAroundBomb)
            {
                var result = _sprite2D.GetWorld2D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters2D.Create(pos, pos + dir));
                if (result.Count == 0)
                    continue;
                var collider = (CollisionObject2D)GodotObject.InstanceFromId(result["collider_id"].AsUInt64());
                if (collider != null)
                    Damage(collider);
            }
        }

        private void Damage(Node target)
        {
            // fixme later

            /*var bomber = target.GetNodeOrNull<BombermanController>("../../../BombermanController");
            var block = target.GetNodeOrNull<Block>("../../../Block");

            // check if this target is a block
            if (block != null)
                block.Visible = false;
            // check if this target is a player (bomber)
            if (bomber != null)
                bomber.Die();*/
        }
    }
}