#if TOOLS
using Godot;

namespace Netick.GodotEngine;
public partial class NetickInspectorPlugin : EditorInspectorPlugin
{
    public override bool _CanHandle(GodotObject @object)
    {
        return @object is Node node && node.SceneFilePath != "" && node.GetTree().EditedSceneRoot == node;
    }

    public override void _ParseBegin(GodotObject @object)
    {
        var inspector = GD.Load<PackedScene>("res://addons/NetickForGodot/Editor/plugins/inspector/netick_node_inspector.tscn").Instantiate<NetickNodeInspector>();

        inspector.InspectedNode = @object as Node;

        AddCustomControl(inspector);
    }

}
#endif