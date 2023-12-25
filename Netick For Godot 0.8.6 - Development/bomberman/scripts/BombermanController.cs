using Godot;
using Netick.GodotEngine;
using Netick.GodotEngine.Extensions;

namespace Netick.Samples.Bomberman;

[GlobalClass]
public partial class BombermanController : NetworkBehaviour
{
    [Export]
    private string _bombPrefab;
    [Export]
    private float _speed = 6.0f;

    private BombermanEventsHandler _bombermanEventsHandler;

    [Export]
    private Sprite2D _sprite2D;

    [Export]
    private CollisionShape2D _collisionShape2D;

    public CharacterBody2D BaseNode { get; private set; }

    // Cache input strings as StringNames to prevent allocations and avoid GC hiccups;
    private StringName _moveLeft = "move_left";
    private StringName _moveRight = "move_right";
    private StringName _moveUp = "move_up";
    private StringName _moveDown = "move_down";
    private StringName _placeBomb = "place_bomb";

    public Vector2 SpawnPos;

    // Networked properties
    [Networked]
    public int Score { get; set; } = 0;
    [Networked]
    public bool Alive { get; set; } = true;

    [Networked]
    public int MaxBombs { get; set; } = 3;

    [Networked]
    public int BombCount { get; set; } = 0;

    public override void _Ready()
    {
        _bombermanEventsHandler = GetTree().Root.GetDescendant<BombermanEventsHandler>();

        BaseNode = GetBaseNode<CharacterBody2D>();

        // We store the spawn pos so that we use it later during respawn
        SpawnPos = BaseNode.Position;
    }

    public override void OnInputSourceLeft()
    {
        _bombermanEventsHandler.KillPlayer(this);
        // destroy the player object when its input source (controller player) leaves the game
        Sandbox.Destroy(Object);
    }

    public override void NetworkUpdate()
    {
        if (IsProxy)
            return;

        var input = Sandbox.GetInput<BombermanInput>();
        input.Movement = Input.GetVector(_moveLeft, _moveRight, _moveUp, _moveDown);

        if (Input.IsActionJustPressed(_placeBomb))
            input.PlantBomb = true;

        Sandbox.SetInput(input);
    }

    public override void NetworkFixedUpdate()
    {
        if (!Alive)
            return;

        if (FetchInput(out BombermanInput input))
        {
            BaseNode.MoveAndCollide(input.Movement * _speed * Sandbox.FixedDeltaTime);

            if (IsServer && input.PlantBomb && BombCount < MaxBombs && !IsResimulating)
            {
                // * round the bomb pos so that it snaps to the nearest square.
                var bombNetworkObject = Sandbox.NetworkInstantiate(_bombPrefab, new Vector3(Round(BaseNode.Position).X, Round(BaseNode.Position).Y, 0));
                var bomb = bombNetworkObject.Node.GetChild<Bomb>();
                BombCount++;
                //bomb.Bomber = this;
                bomb.Exploded += () => { BombCount--; if (BombCount < 0) BombCount = 0; };
            }
        }

    }

    public void Die()
    {
        Alive = false;
        _bombermanEventsHandler.KillPlayer(this);
    }
    public void Respawn()
    {
        Alive = true;
        MaxBombs = 3;
        BombCount = 0;
        _bombermanEventsHandler.RespawnPlayer(this);
        BaseNode.Position = SpawnPos;
    }

    [OnChanged(nameof(Alive))]
    private void OnAliveChanged(OnChangedData onChangedData)
    {
        // * based on state of Alive: Hide/show player object
        _sprite2D.Visible = Alive;
        _collisionShape2D.Disabled = !Alive;
    }

    public Vector2 Round(Vector2 vec) => new Vector2(Mathf.Round(vec.X), Mathf.Round(vec.Y));
}