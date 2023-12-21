#if TOOLS
using Godot;
using NetickEditor;

namespace Netick.GodotEngine;
public partial class NetickInspectorPlugin : EditorInspectorPlugin
{

    [Signal]
    public delegate void InspectorCreatedEventHandler(NetickNodeInspector inspector);

    public override bool _CanHandle(GodotObject @object)
    {
        return @object is Node node && node.SceneFilePath != "" && node.GetTree().EditedSceneRoot == node;
    }

    public override void _ParseBegin(GodotObject @object)
    {
        var inspector = GD.Load<PackedScene>(NetickEditorResourcePaths.InspectorControlPath).Instantiate<NetickNodeInspector>();

        inspector.InspectedNode = @object as Node;

        AddCustomControl(inspector);

        EmitSignal(SignalName.InspectorCreated, inspector);
    }

}
#endif