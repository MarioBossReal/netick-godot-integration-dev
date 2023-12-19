using Godot;

namespace Netick.GodotEngine;

[Tool]
public partial class ResourceReferenceListItem : MarginContainer
{
    [Export]
    public Label NameLabel { get; set; }

    [Export]
    public Label IdLabel { get; set; }

    [Export]
    public Button RemoveButton { get; set; }

    public bool IsPrefabReference = false;

    private NetickConfig _netickConfig;

    public override void _Ready()
    {
        RemoveButton.Pressed += HandleRemove;
    }

    public void Initialize(NetickConfig netickConfig)
    {
        _netickConfig = netickConfig;
    }

    private void HandleRemove()
    {
        if (_netickConfig == null)
            return;

        if (IsPrefabReference)
        {
            _netickConfig.Prefabs.Remove(NameLabel.Text);
        }
        else
        {
            _netickConfig.Levels.Remove(NameLabel.Text);
        }

        ResourceSaver.Save(_netickConfig, _netickConfig.ResourcePath);

        QueueFree();
    }
}
