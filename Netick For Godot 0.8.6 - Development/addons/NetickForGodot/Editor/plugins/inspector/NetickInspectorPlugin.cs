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
        if (@object is not Node node)
            return false;

        var category = CategorizeNode(node);

        if (category == InspectedNodeCategory.PrefabRoot)
            return true;

        if (category == InspectedNodeCategory.NestedPrefab)
            return false;

        return true;
    }

    public override void _ParseBegin(GodotObject @object)
    {
        var inspector = GD.Load<PackedScene>(NetickEditorResourcePaths.InspectorControlPath).Instantiate<NetickNodeInspector>();

        inspector.InspectedNode = @object as Node;
        var category = CategorizeNode(inspector.InspectedNode);
        inspector.Category = category;

        AddCustomControl(inspector);
        EmitSignal(SignalName.InspectorCreated, inspector);
    }

    private InspectedNodeCategory CategorizeNode(Node node)
    {
        if (node.SceneFilePath != "" && node.GetTree().EditedSceneRoot == node)
            return InspectedNodeCategory.PrefabRoot;

        if (node.SceneFilePath != "" && node.GetTree().EditedSceneRoot != node)
            return InspectedNodeCategory.NestedPrefab;

        return InspectedNodeCategory.NonPrefabChild;
    }

    public enum InspectedNodeCategory
    {
        PrefabRoot,
        NestedPrefab,
        NonPrefabChild
    }

}
#endif