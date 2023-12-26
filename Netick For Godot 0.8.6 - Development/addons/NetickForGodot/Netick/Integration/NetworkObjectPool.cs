// Copyright (c) 2023 Karrar Rahim. All rights reserved.

using Godot;
using Netick.GodotEngine.Constants;
using Netick.GodotEngine.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Netick.GodotEngine;

internal class NetworkObjectPool
{
    private readonly NetworkSandbox Sandbox;
    private readonly string PrefabPath;
    internal int PrefabId = -1;

    private readonly Stack<NetworkObject> Free = new(64);
    private readonly List<NetworkObject> All = new(64);

    public bool HideUnspawned = true;
    public bool UsePool = false;
    public int PreloadAmount = 0;

    public NetworkObjectPool(NetworkSandbox sandbox, ResourceReference prefabReference, int preloadAmount, bool hideUnspawned)
    {
        Sandbox = sandbox;
        PrefabPath = prefabReference.Path;
        HideUnspawned = hideUnspawned;
        PrefabId = prefabReference.Id;

        Preload(preloadAmount);
    }

    internal static int GetPrefabIdFromName(StringName prefabName)
    {
        Network.Config.Prefabs.TryGetValue(prefabName, out var reference);

        if (reference != null)
            return reference.Id;
        else
            return -1;
    }

    private void Preload(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Free.Push(Create(Vector3.Zero, Quaternion.Identity));
        }
    }

    public void DestroyEverything()
    {
        UsePool = false;

        for (int i = 0; i < All.Count; i++)
        {
            All[i]?.Node?.QueueFree();
        }

        Free.Clear();
        All.Clear();
    }

    public void Decommission()
    {
        CleanupAllFree();
        UsePool = false;
    }

    public void CleanupAllFree()
    {
        while (Free.Count > 0)
        {
            var obj = Free.Pop();
            obj?.Node?.QueueFree();
        }
    }

    public void Reload()
    {
        if (UsePool)
        {
            CleanupAllFree();
            //Free.Clear();
            Preload(PreloadAmount);
        }
    }

    public NetworkObject Get(Vector3 pos, Quaternion rot)
    {
        NetworkObject newObj;

        if (!UsePool)
        {
            newObj = Create(pos, rot);
        }
        else
        {
            if (Free.Count > 0)
                newObj = Free.Pop();
            else
                newObj = Create(pos, rot);
        }

        if (newObj.Node != null)
        {
            var node = newObj.Node;

            if (node is Node3D node3d)
            {
                node3d.Position = pos;
                node3d.Quaternion = rot;
            }
            else if (node is Node2D node2d)
            {
                node2d.Position = new Vector2(pos.X, pos.Y);
                node2d.Rotation = rot.X;
            }
        }

        Sandbox.Level.AddChild(newObj.Node);
        return newObj;
    }

    private NetworkObject Create(Vector3 pos, Quaternion rot)
    {
        var scene = GD.Load<PackedScene>(PrefabPath);
        var prefabRoot = scene.Instantiate();
        var networkObject = new NetworkObject();
        networkObject.Node = prefabRoot;
        prefabRoot.SetMeta(MetaConstants.NetworkObject, networkObject);

        InitPrefabNodeChildren(prefabRoot);
        Init(networkObject, networkObject);

        All.Add(networkObject);

        return networkObject;
    }

    private void InitPrefabNodeChildren(Node root)
    {
        var descendants = root.GetDescendants();

        // Children which are nested prefabs of a network object
        // are treated as non-prefabs (made local to scene).
        foreach (var descendant in descendants)
        {
            descendant.SceneFilePath = "";
            descendant.Owner = root;
        }
    }

    private void Init(NetworkObject currentNode, NetworkObject prefabRoot)
    {
        if (currentNode != null)
        {
            currentNode.InitInternals(Sandbox);
            Sandbox.Engine.CreateEntityLocal(currentNode);
            currentNode.IsPrefabInstance = true;
            currentNode.PrefabId = PrefabId;
        }

        var childObjs = currentNode.Node.GetChildren<Node>()
            .Where(x => x.HasMeta(MetaConstants.NetworkedNode));

        foreach (var childObj in childObjs)
        {
            var childNetObj = new NetworkObject();
            childNetObj.Node = childObj;
            childNetObj.ParentId = currentNode.Id;
            childNetObj.InternalPrefabRoot = prefabRoot;

            childNetObj.PrefabIndex = childObj.Owner.GetDescendants()
                .Where(x => x.HasMeta(MetaConstants.NetworkedNode))
                .ToList()
                .IndexOf(childObj);

            childObj.SetMeta(MetaConstants.NetworkObject, childNetObj);
            childObj.SetMeta(MetaConstants.OwnerPrefabId, PrefabId);

            prefabRoot.InternalPrefabChildren.Add(childNetObj);

            Init(childNetObj, prefabRoot);
        }
    }
    public void Push(NetworkObject obj)
    {
        obj.Node.GetParent().RemoveChild(obj.Node);
        Free.Push(obj);
    }
}
