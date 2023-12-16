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

    public string ConfigPath => ConfigPathTextEdit?.Text ?? string.Empty;

    public string AssemblyPath => AssemblyPathTextEdit?.Text ?? string.Empty;
}
