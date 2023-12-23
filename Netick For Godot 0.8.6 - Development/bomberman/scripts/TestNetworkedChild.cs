using Godot;
using Netick.GodotEngine;

namespace Netick.Samples.Bomberman;
public partial class TestNetworkedChild : NetworkBehaviour
{
    [Networked]
    NetworkString64 LabelText { get; set; }

    private Label label;

    public override void NetworkStart()
    {
        label = GetBaseNode<Label>();

        if (IsServer)
            LabelText = new("Hello, my id is " + Id);
    }

    [OnChanged(nameof(LabelText))]
    private void HandleLabelTextChanged(OnChangedData data)
    {
        if (label == null)
            return;

        label.Text = LabelText.ToString();
    }
}
