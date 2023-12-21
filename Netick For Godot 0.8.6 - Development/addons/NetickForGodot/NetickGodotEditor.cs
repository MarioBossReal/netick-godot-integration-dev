// Copyright (c) 2023 Karrar Rahim. All rights reserved.

#if TOOLS
using Godot;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Netick.CodeGen;
using Netick.GodotEngine;
using System;
using System.Collections.Generic;
using System.IO;

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
    public const string DockPath = "res://addons/NetickForGodot/NetickDock.tscn";
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
        _dock = GD.Load<PackedScene>("res://addons/NetickForGodot/NetickDock.tscn").Instantiate<NetickDock>();
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
        inspector.NetworkedCheckbox.SetPressedNoSignal(_netickConfig.Prefabs.ContainsKey(name));
        inspector.NetworkedPropertySet += HandleNodeNetworkedPropertySet;
    }

    private void HandleNodeNetworkedPropertySet(Node node, bool networked)
    {
        if (networked)
        {
            RegisterNetworkedPrefab(node);
        }
        else
        {
            UnRegisterNetworkedPrefab(node);
        }
    }

    // Currently does not setup child prefabs like the original code does. Because this is going to be changed at some point.
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
        //GD.Print("Netick: Registered new prefab: " + Path.GetFileName(sceneRoot.SceneFilePath));

        /*        if (sceneRoot is NetworkLevel)
                {
                    bool exists = _netickConfig.Levels.ContainsKey(name);

                    if (!exists)
                    {
                        var reference = new ResourceReference();
                        reference.Name = name;
                        reference.Path = path;
                        reference.Id = _netickConfig.GetValidNewLevelId();

                        _netickConfig.Levels.Add(name, reference);

                        _dock.AddLevelReferenceToList(reference);

                        GD.Print("Netick: Registered new level: " + Path.GetFileName(sceneRoot.SceneFilePath));
                    }
                }*/

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

        //GD.Print("Netick: Unregistered prefab: " + Path.GetFileName(sceneRoot.SceneFilePath));
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

    /*---------------------------------------------------------------------------------------------------------*/

    // To be deleted.
    private void RegisterStuff()
    {
        var scenes = GetAllFilesStartingWith(ProjectSettings.GlobalizePath("res://"), ".tscn", null);
        var levels = new List<string>();
        var prefabs = new List<string>();

        for (int i = 0; i < scenes.Count; i++)
        {
            var scene = GD.Load<PackedScene>(scenes[i]);
            var instance = scene.Instantiate();

            if (IsNetworkObject(instance))
                prefabs.Add(scenes[i]);
            else if (IsNetworkLevel(instance, scenes[i]))
            {
                levels.Add(scenes[i]);
            }

            instance.QueueFree();
        }

        var config = _netickConfig;
        config.Set(nameof(NetickConfig.Levels), LocalizeArrayElements(levels.ToArray()));
        config.Set(nameof(NetickConfig.Prefabs), LocalizeArrayElements(prefabs.ToArray()));

        GD.Print("Netick Editor: registering levels and prefabs done.");

        SaveNetickResource(config);
        ProcessObjects();
    }

    // To be deleted.
    private void ProcessObjects()
    {
        ProcessSceneObjects();
        if (_netickConfig.Get(nameof(NetickConfig.Prefabs)).AsStringArray() != null)
            ProcessPrefabs(_netickConfig.Get(nameof(NetickConfig.Prefabs)).AsStringArray());
    }

    // To be deleted.
    internal static void ProcessSceneObjects()
    {
        var editorInterface = EditorInterface.Singleton;
        var rootChilds = editorInterface.GetEditedSceneRoot().GetChildren();
        int counter = 0;

        for (int i = 0; i < rootChilds.Count; i++)
            ProcessSceneObject(rootChilds[i], ref counter);

        GD.Print("Netick Editor: updating scene objects done.");
    }

    // To be deleted.
    internal static void ProcessSceneObject(Node parent, ref int sceneIdCounter)
    {
        if (IsNetworkObject(parent))
        {
            SetPrefabNodeData(parent, -1, null, -1);
            parent.Set(nameof(NetworkObject.SceneId), sceneIdCounter);
            SetPrefabNodeChildren(parent, new Node[0]);
            sceneIdCounter++;
        }

        foreach (var obj in parent.GetChildren())
            ProcessSceneObject(obj, ref sceneIdCounter);
    }

    // To be deleted.
    void ProcessPrefabs(string[] prefabPaths)
    {
        int id = 0;

        foreach (var prefab in prefabPaths)
            RegisterPrefab(prefab, ref id);

        GD.Print("Netick Editor: updating network prefabs done.");

    }

    // To be deleted.
    internal void RegisterPrefab(string prefabPath, ref int id)
    {
        var scene = GD.Load<PackedScene>(prefabPath);
        var obj = scene.Instantiate();

        if (!IsNetworkObject(obj))
        {
            GD.PrintErr("Netick Editor: registering prefab failed because the prefab does not have a NetworkObject root.");
            return;
        }

        if (obj == null)
        {
            GD.PrintErr("Netick Editor: registering prefab failed.");
            return;
        }

        SetPrefabNodeData(obj, id, null, -1);
        var childs = new List<Node>();

        foreach (Node child in obj.GetChildren())
            SetPrefabId(child, id, childs, obj);

        SetPrefabNodeChildren(obj, childs.ToArray());       //   obj.Children = childs.ToArray();
        id++;

        var packedScene = new PackedScene();
        packedScene.Pack(obj);
        ResourceSaver.Save(packedScene, prefabPath);
        packedScene.TakeOverPath(prefabPath);
        EditorInterface.Singleton.GetResourceFilesystem().UpdateFile(prefabPath);   //GetEditorInterface().GetResourceFilesystem().Scan();
    }

    // To be deleted.
    internal static void SetPrefabId(Node obj, int prefabId, List<Node> rootChildren, Node root)
    {
        if (rootChildren != null && IsNetworkObject(obj))
        {
            SetPrefabNodeData(obj, prefabId, root, rootChildren.Count);   // GD.Print("Child: " + obj.Name + " Type: " + obj.GetType());
            SetPrefabNodeChildren(obj, new Node[0]);
            rootChildren.Add(obj);  // net.Children    = new NetworkObject[0];
        }

        foreach (Node child in obj.GetChildren())
            SetPrefabId(child, prefabId, rootChildren, root);
    }

    // To be deleted.
    internal static void SetPrefabNodeData(Node node, int prefabId, Node prefabRoot, int prefabIndex)
    {
        node.Set(nameof(NetworkObject.SceneId), -1);
        node.Set(nameof(NetworkObject.PrefabId), prefabId);
        node.Set(nameof(NetworkObject.BakedInternalPrefabRoot), prefabRoot);
        node.Set(nameof(NetworkObject.PrefabIndex), prefabIndex);
    }

    // To be deleted.
    internal static void SetPrefabNodeChildren(Node node, Node[] children)
    {
        var godotArray = new Godot.Collections.Array(children);
        node.Set(nameof(NetworkObject.BakedInternalPrefabChildren), Variant.From(godotArray));
    }

    // To be deleted.
    internal static bool IsNetworkObject(Node parent)
    {
        return HasScript(parent, nameof(NetworkObject));
    }

    // To be deleted.
    internal static bool IsNetworkLevel(Node parent, string path)
    {
        return HasScript(parent, nameof(NetworkLevel));
    }

    // To be deleted.
    internal static bool HasScript(Node parent, string scriptName)
    {
        foreach (var d in parent.GetPropertyList())
        {
            if (d["name"].AsString().Contains(scriptName))
            {
                return true;
            }
        }

        return false;
    }

    // To be deleted.
    private static string GetLocalPathFromGlobal(string global)
    {
        var k = global.Replace(ProjectSettings.GlobalizePath("res://"), "").Insert(0, "res://");
        k = k.Replace("\\", "/");
        return k;
    }

    // To be deleted.
    private List<string> GetAllFilesStartingWith(string path, string format, string startingWord)
    {
        var allfiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
        var filtered = new List<string>(10);

        foreach (var fileo in allfiles)
        {
            if (startingWord == null)
            {
                if (fileo.EndsWith(format))
                    filtered.Add(fileo);
            }
            else
            {
                if (fileo.EndsWith(format) && fileo.ToLower().StartsWith(startingWord))
                    filtered.Add(fileo);
            }
        }

        return filtered;
    }

    // To be deleted.
    private string[] LocalizeArrayElements(string[] array)
    {
        for (int i = 0; i < array.Length; i++)
            array[i] = GetLocalPathFromGlobal(array[i]);
        return array;
    }

    // To be deleted.
    private void SaveNetickResource(Godot.Resource resource)
    {
        ResourceSaver.Save(resource, resource.ResourcePath);
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

}


#endif
