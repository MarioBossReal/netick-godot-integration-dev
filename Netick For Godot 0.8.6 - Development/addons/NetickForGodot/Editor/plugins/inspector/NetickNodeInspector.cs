using Godot;

namespace Netick.GodotEngine;

[Tool]
public partial class NetickNodeInspector : Control
{
    public Node InspectedNode { get; set; }

    [Signal]
    public delegate void NetworkedPropertySetEventHandler(Node rootNode, bool networked);

    [Export]
    public CheckBox NetworkedCheckbox { get; set; }

    public override void _Ready()
    {
        NetworkedCheckbox.Toggled += SetNetworked;
        //NetworkedCheckbox.SetPressedNoSignal(InspectedNode.HasMeta("networked_object"));
    }

    private void SetNetworked(bool state)
    {
        EmitSignal(SignalName.NetworkedPropertySet, InspectedNode, state);
    }
}
