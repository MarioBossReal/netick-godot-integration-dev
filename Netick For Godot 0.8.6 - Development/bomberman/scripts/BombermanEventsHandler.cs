using Godot;
using Netick.GodotEngine;
using System.Collections.Generic;

namespace Netick.Samples.Bomberman;

[GlobalClass]
public partial class BombermanEventsHandler : NetworkEventsListener
{
    [Export]
    private string _playerPrefab;
    [Export]
    private string _bombPrefab;
    [Export]
    private string _destroyableBlockPrefab;

    private Queue<Vector2> _freePositions = new Queue<Vector2>(4);
    //bool _pressedSpace = false;

    public List<BombermanController> AlivePlayers = new List<BombermanController>();
    public Vector2[] SpawnPositions = new Vector2[4]
    {
        new Vector2(11, 9),
        new Vector2(11, 1),
        new Vector2(1,  9),
        new Vector2(1,  1)
    };

    // ******************** Netick Callbacks ********************

    // This is called on the server and the clients when Netick has started.
    public override void OnStartup(NetworkSandbox sandbox)
    {
        sandbox.InitializePool(_bombPrefab, 5);
    }

    // This is called to read inputs.
    public override void OnInput(NetworkSandbox sandbox)
    {

    }

    // This is called on the server when a client has connected.
    public override void OnClientConnected(NetworkSandbox sandbox, NetworkConnection client)
    {
        var pos = SpawnPositions[Sandbox.ConnectedClients.Count];
        var playerNetworkObject = sandbox.NetworkInstantiate(_playerPrefab, new Vector3(pos.X, pos.Y, 0), Quaternion.Identity, client);
        var player = NetickGodotUtils.FindObjectOfType<BombermanController>(playerNetworkObject.TransformSource);
        client.PlayerObject = player;
        AlivePlayers.Add(player);
    }

    // This is called on the server when a client has disconnected.
    public override void OnClientDisconnected(NetworkSandbox sandbox, NetworkConnection client, TransportDisconnectReason reason)
    {
        _freePositions.Enqueue(((BombermanController)client.PlayerObject).SpawnPos);
    }

    public override void OnConnectRequest(NetworkSandbox sandbox, NetworkConnectionRequest request)
    {
        if (_freePositions.Count < 1)
            request.Refuse();
    }

    // This is called on the server and the clients when the scene has been loaded.
    public override void OnSceneLoaded(NetworkSandbox sandbox)
    {
        if (sandbox.IsClient)
            return;

        _freePositions.Clear();

        for (int i = 0; i < 4; i++)
            _freePositions.Enqueue(SpawnPositions[i]);

        for (int i = 0; i < sandbox.ConnectedPlayers.Count; i++)
        {
            var player = sandbox.NetworkInstantiate(_playerPrefab, new Vector3(SpawnPositions[i].X, SpawnPositions[i].Y, 0), Quaternion.Identity, sandbox.ConnectedPlayers[i]);

            GD.Print(player.TransformSource.Name);

            var asBombmer = NetickGodotUtils.FindObjectOfType<BombermanController>(player.TransformSource);
            sandbox.ConnectedPlayers[i].PlayerObject = asBombmer;
        }

        RestartGame();
    }

    // ***************************************

    public void RestartGame()
    {
        DestroyLevel();
        CreateNewLevel();

        foreach (var player in Sandbox.ConnectedPlayers)
            ((BombermanController)player.PlayerObject).Respawn();
    }

    private void DestroyLevel()
    {
        var blocks = new List<Block>();
        var bombs = new List<Bomb>();
        NetickGodotUtils.FindObjectsOfType(Sandbox.Level, blocks);
        NetickGodotUtils.FindObjectsOfType(Sandbox.Level, bombs);

        foreach (var block in blocks)
            Sandbox.Destroy(block.Object);
        foreach (var bomb in bombs)
            Sandbox.Destroy(bomb.Object);
    }

    private void CreateNewLevel()
    {
        var takenPositions = new List<Vector2>();
        var maxX = 11;
        var maxY = 9;

        for (int x = 1; x <= maxX; x++)
        {
            for (int y = 1; y <= maxY; y++)
            {
                var spawn = GD.RandRange(0f, 1f) > 0.4f;
                var pos = new Vector2(x, y);

                if (spawn && IsValidPos(pos))
                {
                    Sandbox.NetworkInstantiate(_destroyableBlockPrefab, new Vector3(pos.X, pos.Y, 0), Quaternion.Identity);
                    takenPositions.Add(pos);
                }
            }
        }
    }

    public void KillPlayer(BombermanController bomber)
    {
        AlivePlayers.Remove(bomber);

        if (AlivePlayers.Count == 1)
        {
            AlivePlayers[0].Score++;
            RestartGame();
        }

        else if (AlivePlayers.Count < 1)
            RestartGame();
    }
    public void RespawnPlayer(BombermanController bomber)
    {
        if (!AlivePlayers.Contains(bomber))
            AlivePlayers.Add(bomber);
    }

    private bool IsValidPos(Vector2 pos)
    {
        // if the pos is the position of a static block, we ignore it
        if ((pos.X >= 2 && pos.X <= 10) && (pos.Y >= 2 && pos.Y <= 8))
            if (pos.X % 2 == 0 && pos.Y % 2 == 0)
                return false;

        // if the pos is near the position of the spawn locations of the players, we ignore it
        foreach (var loc in SpawnPositions)
        {
            if (pos == loc)
                return false;
            if (pos == loc + Vector2.Up || pos == loc + Vector2.Down)
                return false;
            if (pos == loc + Vector2.Left || pos == loc + Vector2.Right)
                return false;
        }
        return true;
    }
}