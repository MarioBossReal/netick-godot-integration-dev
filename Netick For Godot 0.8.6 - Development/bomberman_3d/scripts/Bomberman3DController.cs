using Godot;
using Netick;
using Netick.GodotEngine;

public partial class Bomberman3DController : NetworkBehaviour
{
    [Export]
    public float Speed { get; set; }

    [Export]
    public Material Red { get; private set; }

    [Export]
    public Material Green { get; private set; }

    [Export]
    public Material Blue { get; private set; }

    [Export]
    public Material Yellow { get; private set; }

    [Export]
    private MeshInstance3D _meshInstance;

    [Networked]
    public int MaterialIndex { get; set; } = 0;

    private CharacterBody3D Body { get; set; }

    private StringName _moveLeft = "move_left";
    private StringName _moveRight = "move_right";
    private StringName _moveUp = "move_up";
    private StringName _moveDown = "move_down";
    private StringName _placeBomb = "place_bomb";

    public override void _Ready()
    {
        Body = GetBaseNode<CharacterBody3D>();
    }

    public override void NetworkUpdate()
    {
        var input = Sandbox.GetInput<Bomberman3DInput>();

        var dir = Input.GetVector(_moveLeft, _moveRight, _moveDown, _moveUp);

        if (dir != Vector2.Zero)
            input.WishDirection = dir;

        if (Input.IsActionJustPressed(_placeBomb))
            input.WishPlaceBomb = true;

        Sandbox.SetInput(input);
    }

    public override void NetworkFixedUpdate()
    {
        if (FetchInput<Bomberman3DInput>(out var input))
        {
            var direction = Vector3.Right * input.WishDirection.X + Vector3.Forward * input.WishDirection.Y;
            Body.MoveAndCollide(direction * Speed * Sandbox.FixedDeltaTime);
        }

        //Sandbox.SetInput(new Bomberman3DInput());
    }

    public override void OnInputSourceLeft()
    {
        Sandbox.Destroy(Object);
    }

    [OnChanged(nameof(MaterialIndex))]
    private void SetMaterial(OnChangedData data)
    {
        var m = MaterialIndex;
        var material = m == 0 ? Red : m == 1 ? Green : m == 2 ? Blue : m == 3 ? Yellow : Red;
        _meshInstance.MaterialOverride = material;
    }


}
