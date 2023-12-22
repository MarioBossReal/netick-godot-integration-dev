using Godot;
using static Netick.GodotEngine.NetickInspectorPlugin;

namespace Netick.GodotEngine;

[Tool]
public partial class NetickNodeInspector : Control
{
    public Node InspectedNode { get; set; }

    [Signal]
    public delegate void NetworkedPropertySetEventHandler(NetickNodeInspector inspector);

    [Export]
    public CheckBox NetworkedCheckbox { get; set; }

    public InspectedNodeCategory Category { get; set; }

    public bool NetworkedPropertyValue { get; set; }

    public override void _Ready()
    {
        NetworkedCheckbox.Toggled += SetNetworked;
        //NetworkedCheckbox.SetPressedNoSignal(InspectedNode.HasMeta("networked_object"));
    }

    private void SetNetworked(bool state)
    {
        NetworkedPropertyValue = state;
        EmitSignal(SignalName.NetworkedPropertySet, this);
    }
}
