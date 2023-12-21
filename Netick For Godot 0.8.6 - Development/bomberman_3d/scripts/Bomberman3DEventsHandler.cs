using Godot;
using Netick;
using Netick.GodotEngine;

public partial class Bomberman3DEventsHandler : NetworkEventsListener
{
    [Export]
    private Node3D _topLeft { get; set; }

    [Export]
    public int BoardSize { get; private set; }

    [Export]
    private Node3D _spawns { get; set; }

    [Export]
    private Material _red;

    [Export]
    private Material _green;

    [Export]
    private Material _blue;

    [Export]
    private Material _yellow;

    private Vector3[] _spawnPositions;

    public override void OnStartup(NetworkSandbox sandbox)
    {
        base.OnStartup(sandbox);

        _spawnPositions = new Vector3[_spawns.GetChildCount()];

        for (int i = 0; i < _spawnPositions.Length; i++)
        {
            _spawnPositions[i] = _spawns.GetChild<Node3D>(i).GlobalPosition;
        }
    }

    public override void OnClientConnected(NetworkSandbox sandbox, NetworkConnection client)
    {
        base.OnClientConnected(sandbox, client);

        var pos = _spawnPositions[sandbox.ConnectedClients.Count];
        var playerObj = sandbox.NetworkInstantiate("bomber_man_3d", pos, Quaternion.Identity, client);

        foreach (var child in playerObj.TransformSource.GetChildren())
        {
            foreach (var gchild in child.GetChildren())
                if (gchild is Bomberman3DController controller)
                {
                    client.PlayerObject = controller;
                    controller.MaterialIndex = sandbox.ConnectedClients.Count;
                }
        }

    }

    public override void OnClientDisconnected(NetworkSandbox sandbox, NetworkConnection client, TransportDisconnectReason transportDisconnectReason)
    {
        base.OnClientDisconnected(sandbox, client, transportDisconnectReason);
    }

    public override void OnSceneLoaded(NetworkSandbox sandbox)
    {
        if (sandbox.IsClient)
            return;
    }

}
