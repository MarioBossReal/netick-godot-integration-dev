using Godot;
using Netick;
using Netick.GodotEngine;

namespace Netick.Samples.Bomberman
{
  [GlobalClass]
  public partial class Block : NetworkBehaviour
  {
    // Networked properties
    [Networked]
    public bool              Visible { get; set; } = true;

    private MeshInstance2D   _meshInstance;
    private CollisionShape2D _collisionShape2D;

    public override void _Ready()
    {
      _meshInstance     = NetickGodotUtils.FindObjectOfType<MeshInstance2D>(Object);
      _collisionShape2D = NetickGodotUtils.FindObjectOfType<CollisionShape2D>(Object);
    }


    [OnChanged(nameof(Visible))]
    private void OnVisibleChanged(OnChangedData onChangedData)
    {
      _meshInstance.Visible      = Visible;
      _collisionShape2D.Disabled = !Visible;
    }
  }
}

