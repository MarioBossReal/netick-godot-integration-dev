#if TOOLS

using Godot;
using Netick.GodotEngine;

namespace NetickEditor;

internal static class NetickWebLinks
{
    public const string Discord = "https://discord.com/invite/uV6bfG66Fx";
    public const string Premium = "https://www.patreon.com/user?u=82493081";
    public const string Docs = "https://www.netick.net/docs.html";
    public const string Site = "https://www.netick.net";
    public static void GoToDiscord() => OS.ShellOpen(Discord);
    public static void GoToDocs() => OS.ShellOpen(Docs);
    public static void GoToSite() => OS.ShellOpen(Site);
}

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
    public VBoxContainer PrefabReferencesContainer { get; private set; }

    [Export]
    public VBoxContainer LevelReferencesContainer { get; private set; }

    [Export]
    public PackedScene ResourceReferenceItemScene { get; private set; }

    private NetickConfig _netickConfig;

    private bool _initialized = false;

    public override void _Ready()
    {
        VersionLabel.Text = $"Version: {Network.Version}-dev";
        DocumentationButton.Pressed += NetickWebLinks.GoToDocs;
        DiscordButton.Pressed += NetickWebLinks.GoToDiscord;
        SiteButton.Pressed += NetickWebLinks.GoToSite;
    }

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

    public void RemovePrefabReferenceFromList(ResourceReference reference)
    {
        foreach (var child in PrefabReferencesContainer?.GetChildren())
        {
            if (child is not ResourceReferenceListItem item)
                continue;

            if (item.NameLabel.Text != reference.Name)
                continue;

            item.QueueFree();
            break;
        }
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

    internal static NetickDock Instantiate()
    {
        return GD.Load<PackedScene>(NetickEditorResourcePaths.DockPath).Instantiate<NetickDock>();
    }
}

#endif