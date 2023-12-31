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

internal static class NetickEditorResourcePaths
{
    public const string DockPath = "res://addons/NetickForGodot/Editor/plugins/dock/NetickDock.tscn";
    public const string InspectorControlPath = "res://addons/NetickForGodot/Editor/plugins/inspector/netick_node_inspector.tscn";
}

[Tool]
public partial class NetickGodotEditor : EditorPlugin
{
    private NetickDock _dock;
    private NetickExportPlugin _exportPlugin;
    private NetickInspectorPlugin _inspectorPlugin;

    internal NetickConfig _netickConfig;
    public override void _EnterTree()
    {
        _exportPlugin = new();
        AddExportPlugin(_exportPlugin);

        _inspectorPlugin = new();
        AddInspectorPlugin(_inspectorPlugin);

        _netickConfig = LoadOrCreateNetickConfig();

        _inspectorPlugin.InspectorCreated += HandleInspectorCreated;
        SceneChanged += RegisterNetworkedLevel;

        InitDock();
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

    private void InitDock()
    {
        _dock = NetickDock.Instantiate();
        AddControlToDock(DockSlot.LeftUl, _dock);
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

    private static NetickConfig LoadOrCreateNetickConfig()
    {
        var path = NetickProjectSettings.GetDefaultConfigPath();

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

    private static void Weave()
    {
        var k = ProjectSettings.GlobalizePath(NetickProjectSettings.FullGameAssemblyPath);
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
