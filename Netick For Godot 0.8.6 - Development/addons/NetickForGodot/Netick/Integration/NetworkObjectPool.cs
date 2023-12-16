﻿// Copyright (c) 2023 Karrar Rahim. All rights reserved.

using Godot;
using System.Collections.Generic;

namespace Netick.GodotEngine
{
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

        internal static int GetPrefabIdFromName(string prefabName)
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
                All[i]?.QueueFree();
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
                obj?.QueueFree();
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

            if (newObj.TransformSource != null)
            {
                var node = newObj.TransformSource;

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

            Sandbox.Level.AddChild(newObj);
            return newObj;
        }


        private NetworkObject Create(Vector3 pos, Quaternion rot)
        {
            //Debug.Log("Pool Create");
            var scene = GD.Load<PackedScene>(PrefabPath);
            var prefabRoot = scene.Instantiate() as NetworkObject;

            Init(prefabRoot);
            All.Add(prefabRoot);

            return prefabRoot;
        }

        private void Init(NetworkObject obj)
        {
            if (obj != null)
            {
                obj.InitInternals(Sandbox);
                Sandbox.Engine.CreateEntityLocal(obj);
                obj.IsPrefabInstance = true;
            }

            foreach (var child in obj.BakedInternalPrefabChildren)
            {
                Init(child);
            }

        }

        public void Push(NetworkObject obj)
        {
            obj.GetParent().RemoveChild(obj);
            Free.Push(obj);
        }
    }
}
