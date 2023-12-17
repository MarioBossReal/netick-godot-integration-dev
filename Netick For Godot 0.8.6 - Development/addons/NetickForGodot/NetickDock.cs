#if TOOLS

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

    private bool _initialized = false;

    public void Initialize(NetickConfig netickConfig)
    {
        if (_initialized)
            return;

        _netickConfig = netickConfig;
        _initialized = true;

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
        if (!_initialized)
            return;

        var item = CreateReferenceListItem(reference);

        item.IsPrefabReference = true;

        PrefabReferencesContainer?.AddChild(item);
    }

    public void AddLevelReferenceToList(ResourceReference reference)
    {
        if (!_initialized)
            return;

        var item = CreateReferenceListItem(reference);

        item.IsPrefabReference = false;

        LevelReferencesContainer?.AddChild(item);
    }

    private void ClearReferenceLists()
    {

        foreach (var child in PrefabReferencesContainer?.GetChildren())
        {
            child?.QueueFree();
        }

        foreach (var child in LevelReferencesContainer?.GetChildren())
        {
            child?.QueueFree();
        }
    }

    private ResourceReferenceListItem CreateReferenceListItem(ResourceReference reference)
    {
        var item = ResourceReferenceItemScene.Instantiate<ResourceReferenceListItem>();

        item.NameLabel.Text = reference.Name;
        item.IdLabel.Text = reference.Id.ToString();
        item.Initialize(_netickConfig);

        return item;
    }
}

#endif