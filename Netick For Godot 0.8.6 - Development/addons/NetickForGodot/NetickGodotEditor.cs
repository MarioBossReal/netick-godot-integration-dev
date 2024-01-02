// Copyright (c) 2023 Karrar Rahim. All rights reserved.

#if TOOLS
using Godot;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Netick.CodeGen;
using Netick.GodotEngine;
using Netick.GodotEngine.Constants;
using System;
using System.IO;
using static Netick.GodotEngine.NetickInspectorPlugin;

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

internal static class NetickEditorResourcePaths
{
    public const string DockPath = "res://addons/NetickForGodot/Editor/plugins/dock/NetickDock.tscn";
    public const string InspectorControlPath = "res://addons/NetickForGodot/Editor/plugins/inspector/netick_node_inspector.tscn";
}

[Tool]
public partial class NetickExportPlugin : EditorExportPlugin
{
    internal NetickGodotEditor _editor;
    string _assemblyPath;

    public override void _ExportBegin(string[] features, bool isDebug, string path, uint flags)
    {
        _assemblyPath = null;
        var fileName = Path.GetFileName(path);
        var folder = path.Replace(fileName, "");     //GD.Print($"MAIN FOLDER {folder}");
        var folders = System.IO.Directory.GetDirectories(folder, "*", System.IO.SearchOption.AllDirectories);
        var asmName = Path.GetFileName(_editor._editorConfig.MainEditorGameAssemblyPath);

        for (int i = 0; i < folders.Length; i++)
        {
            var p = $"{folders[i]}/{asmName}";
            bool doesExist = System.IO.File.Exists(p);

            if (doesExist)
            {
                _assemblyPath = p;
                break;
            }
        }
    }

    public override void _ExportEnd()
    {
        if (_assemblyPath != null)
        {
            Netick.CodeGen.Processor.ProcessAssembly(new GodotCodeGen(), _assemblyPath);
            GD.Print("Netick Editor: export done.");
        }
        else
        {
            GD.PrintErr("Netick Editor: export failed.");
        }
    }
}

[Tool]
public partial class NetickGodotEditor : EditorPlugin
{
    private NetickDock _dock;
    private NetickExportPlugin _exportPlugin;
    private NetickInspectorPlugin _inspectorPlugin;

    private TextEdit _configPath;
    private TextEdit _assemblyPath;
    private CheckButton _autoUpdateCheckButton;

    internal NetickGodotEditorConfig _editorConfig;
    internal NetickConfig _netickConfig;
    public override void _EnterTree()
    {
        _exportPlugin = new NetickExportPlugin();
        AddExportPlugin(_exportPlugin);
        _exportPlugin._editor = this;

        _editorConfig = GetNetickEditorConfigResource("res://addons/NetickForGodot/editorConfig.tres");
        _netickConfig = GetNetickConfigResource(_editorConfig.NetickConfigPath);

        _inspectorPlugin = new();
        AddInspectorPlugin(_inspectorPlugin);
        _inspectorPlugin.InspectorCreated += HandleInspectorCreated;

        SceneChanged += RegisterNetworkedLevel;

        InitEditor();
    }

    public override void _ExitTree()
    {
        RemoveExportPlugin(_exportPlugin);
        RemoveInspectorPlugin(_inspectorPlugin);
        RemoveControlFromDocks(_dock);
        _dock?.Free();
        _dock = null;
        _exportPlugin = null;
        _inspectorPlugin = null;
    }

    public override bool _Build()
    {
        _netickConfig.CleanupInvalidReferences();
        Weave();
        return true;
    }

    private void InitEditor()
    {
        _dock = GD.Load<PackedScene>(NetickEditorResourcePaths.DockPath).Instantiate<NetickDock>();
        AddControlToDock(DockSlot.LeftUl, _dock);

        _dock.VersionLabel.Text = $"Version: {Network.Version}-dev";

        _dock.DocumentationButton.Pressed += NetickWebLinks.GoToDocs;
        _dock.DiscordButton.Pressed += NetickWebLinks.GoToDiscord;
        _dock.SiteButton.Pressed += NetickWebLinks.GoToSite;

        _configPath = _dock.ConfigPathTextEdit;
        _assemblyPath = _dock.AssemblyPathTextEdit;

        _configPath.Text = _editorConfig.NetickConfigPath;
        _assemblyPath.Text = _editorConfig.EditorGameAssemblyDirectoryPath;

        _configPath.TextChanged += () => _editorConfig.NetickConfigPath = _configPath.Text;
        _assemblyPath.TextChanged += () => _editorConfig.EditorGameAssemblyDirectoryPath = _assemblyPath.Text;

        _dock?.Initialize(_netickConfig);
    }

    private void HandleInspectorCreated(NetickNodeInspector inspector)
    {
        var name = Path.GetFileNameWithoutExtension(inspector.InspectedNode.SceneFilePath);

        bool check = false;

        if (inspector.Category == InspectedNodeCategory.PrefabRoot)
            check = _netickConfig.Prefabs.ContainsKey(name);

        if (inspector.Category == InspectedNodeCategory.NonPrefabChild)
            check = inspector.InspectedNode.HasMeta(MetaConstants.NetworkedNode);

        inspector.NetworkedCheckbox.SetPressedNoSignal(check);
        inspector.NetworkedPropertySet += HandleNodeNetworkedPropertySet;
    }

    private void HandleNodeNetworkedPropertySet(NetickNodeInspector inspector)
    {
        var category = inspector.Category;
        var networked = inspector.NetworkedPropertyValue;
        var node = inspector.InspectedNode;

        if (category == InspectedNodeCategory.NonPrefabChild)
        {
            if (networked)
            {
                node.SetMeta(MetaConstants.NetworkedNode, true);
            }
            else
            {
                if (node.HasMeta(MetaConstants.NetworkedNode))
                    node.RemoveMeta(MetaConstants.NetworkedNode);
            }
        }

        if (category == InspectedNodeCategory.PrefabRoot)
        {
            if (networked)
            {
                node.SetMeta(MetaConstants.NetworkedNode, true);
                RegisterNetworkedPrefab(node);
            }
            else
            {
                UnRegisterNetworkedPrefab(node);
                if (node.HasMeta(MetaConstants.NetworkedNode))
                    node.RemoveMeta(MetaConstants.NetworkedNode);
            }
        }
    }

    private void RegisterNetworkedPrefab(Node sceneRoot)
    {
        if (sceneRoot == null)
            return;

        if (sceneRoot.SceneFilePath == null || sceneRoot.SceneFilePath == string.Empty)
            return;

        var path = sceneRoot.SceneFilePath;

        var name = Path.GetFileNameWithoutExtension(path);

        // Already registered.
        if (_netickConfig.Prefabs.ContainsKey(name))
            return;

        var reference = new ResourceReference();
        reference.Name = name;
        reference.Path = path;
        reference.Id = _netickConfig.GetValidNewPrefabId();

        _netickConfig.Prefabs.Add(name, reference);
        _dock.AddPrefabReferenceToList(reference);

        ResourceSaver.Save(_netickConfig, _netickConfig.ResourcePath);
    }

    // Temporary! To allow registering levels in the same way as before.
    private void RegisterNetworkedLevel(Node sceneRoot)
    {
        if (sceneRoot == null)
            return;

        if (sceneRoot.SceneFilePath == null || sceneRoot.SceneFilePath == string.Empty)
            return;

        var path = sceneRoot.SceneFilePath;

        var name = Path.GetFileNameWithoutExtension(path);

        // Already registered.
        if (_netickConfig.Levels.ContainsKey(name))
            return;

        if (sceneRoot is not NetworkLevel)
            return;

        var reference = new ResourceReference();
        reference.Name = name;
        reference.Path = path;
        reference.Id = _netickConfig.GetValidNewLevelId();

        _netickConfig.Levels.Add(name, reference);

        _dock.AddLevelReferenceToList(reference);

        ResourceSaver.Save(_netickConfig, _netickConfig.ResourcePath);

        GD.Print("Netick: Registered new level: " + Path.GetFileName(sceneRoot.SceneFilePath));
    }

    private void UnRegisterNetworkedPrefab(Node sceneRoot)
    {
        if (sceneRoot == null)
            return;

        var name = Path.GetFileNameWithoutExtension(sceneRoot.SceneFilePath);

        if (!_netickConfig.Prefabs.TryGetValue(name, out var reference))
            return;

        _netickConfig.Prefabs.Remove(name);
        _dock.RemovePrefabReferenceFromList(reference);

        ResourceSaver.Save(_netickConfig, _netickConfig.ResourcePath);
    }

    private static NetickGodotEditorConfig GetNetickEditorConfigResource(string path)
    {
        if (ResourceLoader.Exists(path))
            return GD.Load<NetickGodotEditorConfig>(path);

        var newNetickConfig = new NetickGodotEditorConfig();
        ResourceSaver.Save(newNetickConfig, path);
        return newNetickConfig;
    }

    private static NetickConfig GetNetickConfigResource(string path)
    {
        if (ResourceLoader.Exists(path))
        {
            var config = GD.Load(path);
            if (config is NetickConfig netickConfig)
                return netickConfig;
        }

        var newNetickConfig = new NetickConfig();
        newNetickConfig.OtherScriptAssemblies = new string[1] { ProjectSettings.GetSetting("dotnet/project/assembly_name").AsString() };

        ResourceSaver.Save(newNetickConfig, path);
        return newNetickConfig;
    }

    private void Weave()
    {
        var k = ProjectSettings.GlobalizePath(_editorConfig.MainEditorGameAssemblyPath);
        Netick.CodeGen.Processor.ProcessAssembly(new GodotCodeGen(), k);
    }
}

public class GodotCodeGen : ICodeGenUser
{
    private Type NetworkBehaviourType;

    public void Init(ModuleDefinition typeDefinition)
    {
        NetworkBehaviourType = typeof(BaseNetworkBehaviour);
    }

    public string GetNetworkScriptTypeFullName()
    {
        return NetworkBehaviourType.FullName;
    }
    public void Log(object msg)
    {
        GD.Print(msg);
    }
    public void LogError(object error)
    {
        GD.PrintErr(error);
    }
    public void OnPropertyFinishProcessing(PropertyDefinition property, FieldDefinition backFieldResolved) { }
    public void OnArrayFinishProcessing(FieldDefinition arrayField) { }
    public bool IsPropertyCompressable(PropertyDefinition property, float precision) { return false; }
    public void HandleCompressablePropertyGetter(MethodDefinition getMeth, PropertyDefinition item, TypeDefinition typeDefinition, MethodReference ptr, int offsetInWords, float precision) { }
    public void HandleCompressablePropertySetter(MethodDefinition setMeth, PropertyDefinition item, FieldDefinition backingField, TypeDefinition typeDefinition, MethodReference ptr, MethodReference dirtfy, int offsetInWords, int sizeInWords, int is64, bool hasOnChanged, float precision) { }
    internal void InsertReadCompressed(TypeReference proType, Mono.Collections.Generic.Collection<Instruction> instructions, ILProcessor il) { }
    internal void InsertWriteCompressed(TypeReference proType, Mono.Collections.Generic.Collection<Instruction> instructions, ILProcessor il) { }


    public bool IsPropertyAutoSmoothable(PropertyDefinition property)
    {
        return false;
    }
    public void HandleAutoSmoothablePropertyGetter(MethodDefinition getMeth, PropertyDefinition item, TypeDefinition typeDefinition, MethodReference ptr, int offsetInWords, float precision, FieldDefinition interpolator)
    {
    }

    public void AddAutoSmoothableInitInstructions(MethodDefinition initMethod, PropertyDefinition property, FieldDefinition interpolator)
    {
    }

    int ICodeGenUser.GetAutoSmoothableVectorFloatFieldsCount(PropertyDefinition item)
    {
        return 1;
    }
}


#endif
