using Godot;

namespace Netick.GodotEngine;

[Tool]
public partial class NetickDock : Control
{
    [Export]
    public Label VersionLabel { get; private set; }

    [Export]
    public Button DocumentationButton { get; private set; }

    [Export]
    public Button DiscordButton { get; private set; }

    [Export]
    public Button SiteButton { get; private set; }

    [Export]
    public Button RegisterPrefabsAndLevelsButton { get; private set; }

    [Export]
    public CheckButton AutoUpdatePrefabsAndLevelsButton { get; private set; }

    [Export]
    public TextEdit ConfigPathTextEdit { get; private set; }

    [Export]
    public TextEdit AssemblyPathTextEdit { get; private set; }

    [Export]
    public VBoxContainer PrefabReferencesContainer { get; private set; }

    [Export]
    public VBoxContainer LevelReferencesContainer { get; private set; }

    [Export]
    public PackedScene ResourceReferenceItemScene { get; private set; }

    public string ConfigPath => ConfigPathTextEdit?.Text ?? string.Empty;

    public string AssemblyPath => AssemblyPathTextEdit?.Text ?? string.Empty;

    private NetickConfig _netickConfig;



    public void Initialize(NetickConfig netickConfig)
    {
        _netickConfig = netickConfig;

        ClearReferenceLists();

        foreach (var pair in _netickConfig.Prefabs)
        {
            var reference = pair.Value;
            AddPrefabReferenceToList(reference);
        }

        foreach (var pair in _netickConfig.Levels)
        {
            var reference = pair.Value;
            AddLevelReferenceToList(reference);
        }
    }

    public void AddPrefabReferenceToList(ResourceReference reference)
    {
        var item = CreateReferenceListItem(reference);

        item.GetNode<Button>("%RemoveButton").Pressed += () =>
        {
            _netickConfig.Prefabs.Remove(item.GetNode<Label>("%NameLabel").Text);
            ResourceSaver.Save(_netickConfig, _netickConfig.ResourcePath);
            item.QueueFree();
        };

        PrefabReferencesContainer.AddChild(item);
    }

    public void AddLevelReferenceToList(ResourceReference reference)
    {
        var item = CreateReferenceListItem(reference);

        item.GetNode<Button>("%RemoveButton").Pressed += () =>
        {
            _netickConfig.Levels.Remove(item.GetNode<Label>("%NameLabel").Text);
            ResourceSaver.Save(_netickConfig, _netickConfig.ResourcePath);
            item.QueueFree();
        };

        LevelReferencesContainer.AddChild(item);
    }

    private void ClearReferenceLists()
    {
        foreach (var child in PrefabReferencesContainer.GetChildren())
        {
            child?.QueueFree();
        }

        foreach (var child in LevelReferencesContainer.GetChildren())
        {
            child?.QueueFree();
        }
    }

    private Control CreateReferenceListItem(ResourceReference reference)
    {
        var item = ResourceReferenceItemScene.Instantiate<Control>();

        item.GetNode<Label>("%NameLabel").Text = reference.Name;
        item.GetNode<Label>("%IdLabel").Text = reference.Id.ToString();
        return item;
    }
}
