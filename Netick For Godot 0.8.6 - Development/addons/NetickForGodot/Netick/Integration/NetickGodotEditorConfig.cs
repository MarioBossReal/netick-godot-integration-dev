// Copyright (c) 2023 Karrar Rahim. All rights reserved.

using Godot;

namespace Netick.GodotEngine;

[Tool, GlobalClass]
public partial class NetickGodotEditorConfig : Resource
{
    [Export]
    public string EditorGameAssemblyDirectoryPath = "res://.godot/mono//temp//bin//Debug/";
    [Export]
    public string NetickConfigPath = "res://netickConfig.tres";
    [Export]
    public bool AutoUpdateLevelsAndPrefabs = false;

    public string MainEditorGameAssemblyPath => $"{EditorGameAssemblyDirectoryPath}/{ProjectSettings.GetSetting("dotnet/project/assembly_name")}.dll";

}
