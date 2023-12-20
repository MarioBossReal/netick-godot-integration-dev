using Godot;

namespace Netick.GodotEngine;

[Tool]
public partial class NetickNodeInspector : Control
{
    public Node InspectedNode { get; set; }

    [Export]
    public CheckBox NetworkedCheckbox { get; set; }

    public override void _Ready()
    {
        NetworkedCheckbox.Toggled += SetNetworked;
        NetworkedCheckbox.SetPressedNoSignal(InspectedNode.HasMeta("networked_object"));
    }

    private void SetNetworked(bool state)
    {
        if (state)
        {
            InspectedNode.SetMeta("networked_object", true);
            GD.Print($"{InspectedNode.Name} toggled networked as on.");
        }
        else
        {
            if (InspectedNode.HasMeta("networked_object"))
                InspectedNode.RemoveMeta("networked_object");
            GD.Print($"{InspectedNode.Name} toggled networked as off.");
        }
    }
}
