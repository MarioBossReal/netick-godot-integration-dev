using Godot;
using Netick.GodotEngine;

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
    [Networked(relevancy: Relevancy.InputSource)]
    public int MaxBombs { get; set; } = 3;

    [Networked]
    public int BombCount { get; set; } = 0;

    [Networked(relevancy: Relevancy.InputSource)]
    private bool wishPlaceBomb = false;

    public override void _Ready()
    {
        _bombermanEventsHandler = NetickGodotUtils.FindObjectOfType<BombermanEventsHandler>(GetTree().Root);

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
        var input = Sandbox.GetInput<BombermanInput>();
        input.Movement = Input.GetVector(_moveLeft, _moveRight, _moveUp, _moveDown);

        if (Input.IsActionJustPressed(_placeBomb))
            wishPlaceBomb = true;

        input.PlantBomb = wishPlaceBomb;
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
                var bombNetworkObject = Sandbox.NetworkInstantiate(_bombPrefab, new Vector3(Round(BaseNode.Position).X, Round(BaseNode.Position).Y, 0), Quaternion.Identity);
                var bomb = NetickGodotUtils.FindObjectOfType<Bomb>(bombNetworkObject);
                BombCount++;
                //bomb.Bomber = this;
                bomb.Exploded += () => BombCount--;
            }
        }
        wishPlaceBomb = false;
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
        wishPlaceBomb = false;
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