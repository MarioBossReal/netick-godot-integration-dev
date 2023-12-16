// Copyright (c) 2023 Karrar Rahim. All rights reserved.
using Godot;
using Godot.Collections;

namespace Netick.GodotEngine;

/// <summary>
/// Config class for Netick.
/// </summary>
[Tool]
[GlobalClass]
public partial class NetickConfig : Resource
{
    [Export]
    public Dictionary<string, ResourceReference> Levels;

    [Export]
    public Dictionary<string, ResourceReference> Prefabs;

    public ReplicationMode Replication = ReplicationMode.Pessimistic;

    [Export]
    public float TickRate = 1 / 0.03f;

    [Export]
    public int MaxPlayers = 16;

    [Export]
    public int MaxObjects = 512;

    [Export]
    public int MaxSendableDataSize = 50000;

    [Export]
    public int AllocatorBlockSize = 532768;

    [Export]
    public int ReceiveBufferSize = 32768;

    [Export]
    public int SendBufferSize = 16384;

    [Export]
    public int Timeout = 10;

    [Export]
    public int MaxPredicatedTicks = 64;

    [Export]
    public bool CallRenderInHeadless = true;

    [Export]
    public bool EnableLogging = true;

    [Export]
    public string[] OtherScriptAssemblies;

    //public PhysicsType PhysicsType = Netick.PhysicsType.Physics3D;

    public bool PredictedClientPhysics = false;

    public int ServerDivisor = 1;

    public int ClientDivisor = 1;

    public float SimServerLoss;

    public float SimClientLoss;

    public bool EnableLagCompensation = false;

    public bool AoI = false;

    public int CellSize = 450;

    public int WorldSize = 8000;

    public bool UseSceneSwitchThread = true;

    public bool LagCompensationDebug = true;

    /// <summary>
    /// Removes references to prefabs and levels which no longer exist in the filesystem.
    /// Only to be used in editor.
    /// </summary>

    public void CleanupInvalidReferences()
    {
        if (!Engine.IsEditorHint())
            return;

        if (ResourcePath == string.Empty)
            return;

        bool needsSaving = false;

        foreach (var pair in Levels)
        {
            if (FileAccess.FileExists(pair.Value.Path))
                continue;

            Levels.Remove(pair.Key);
            needsSaving = true;

            GD.Print("Netick: Removed invalid level: " + pair.Key);
        }

        foreach (var pair in Prefabs)
        {
            if (FileAccess.FileExists(pair.Value.Path))
                continue;

            Prefabs.Remove(pair.Key);
            needsSaving = true;

            GD.Print("Netick: Removed invalid prefab: " + pair.Key);
        }

        if (needsSaving)
        {
            ResourceSaver.Save(this, ResourcePath);
        }
    }
}
