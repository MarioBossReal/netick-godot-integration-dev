using Godot;
using Netick.GodotEngine;

namespace Netick.Samples.Bomberman
{
    [GlobalClass]
    public partial class Block : NetworkBehaviour<StaticBody2D>
    {
        // Networked properties
        [Networked]
        public bool Visible { get; set; } = true;

        [Export]
        private MeshInstance2D _meshInstance;

        [Export]
        private CollisionShape2D _collisionShape2D;

        public override void _Ready()
        {
            //InitializeBaseNode();
        }

        [OnChanged(nameof(Visible))]
        private void OnVisibleChanged(OnChangedData onChangedData)
        {
            _meshInstance.Visible = Visible;
            _collisionShape2D.Disabled = !Visible;
        }
    }
}

