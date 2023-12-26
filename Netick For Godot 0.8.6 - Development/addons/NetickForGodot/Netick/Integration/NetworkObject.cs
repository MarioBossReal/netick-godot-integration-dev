// Copyright (c) 2023 Karrar Rahim. All rights reserved.

using Godot;
using Netick.GodotEngine.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Netick.GodotEngine;

public unsafe partial class NetworkObject : Resource, INetickEntity
{
    /// <summary>
    /// The <see cref="NetworkSandbox"/> this <see cref="NetworkObject"/> is being simulated in.
    /// </summary>
    public NetworkSandbox Sandbox { get; private set; }

    /// <summary>
    /// The <see cref="NetickEngine"/> that this <see cref="NetworkObject"/> is being simulated by.
    /// </summary>
    public NetickEngine Engine { get; internal set; }

    /// <summary>
    /// The <see cref="Netick.Entity"/> that this <see cref="NetworkObject"/> represents.
    /// </summary>
    public Entity Entity { get; private set; }

    /// <summary>
    /// The Network Id of this <see cref="NetworkObject"/>. The same as <see cref="Entity"/>.Id.
    /// </summary>
    public int NetworkId => Entity != null ? Entity.NetworkId : -1;

    /// <summary>
    /// The <see cref="Tick"/> which this <see cref="NetworkObject"/> was spawned at.
    /// </summary>
    public Tick SpawnTick { get; internal set; } = Tick.InvalidTick;

    /// <summary>
    /// The <see cref="Netick.SpawnPredictionKey"/> that this <see cref="NetworkObject"/> was spawned with.
    /// </summary>
    public SpawnPredictionKey SpawnPredictionKey { get; private set; }

    /// <summary>
    /// The <see cref="Godot.Node"/> that this <see cref="NetworkObject"/> wraps.
    /// </summary>
    public Node Node { get; set; }

    /// <summary>
    /// The list of instanced <see cref="NetworkObject"/>s that are descendants of this <see cref="NetworkObject"/>'s associated prefab.
    /// </summary>
    internal List<NetworkObject> InternalPrefabChildren { get; private set; } = new();

    /// <summary>
    /// The instanced <see cref="NetworkObject"/> which is the root of this <see cref="NetworkObject"/>'s associated prefab.
    /// </summary>
    internal NetworkObject InternalPrefabRoot;

    internal int SceneId = 0;

    /// <summary>
    ///  The global Id of this <see cref="NetworkObject"/>'s associated prefab.
    /// </summary>
    internal int PrefabId = -1;

    /// <summary>
    /// The index of this <see cref="NetworkObject"/> in the networked descendents of its <see cref="InternalPrefabRoot"/>.
    /// </summary>
    internal int PrefabIndex = -1;

    internal bool IsPrefabInstance = false;

    internal int InstanceCounter = 0;

    //internal List<NetworkEventsListner> EventListners = new();

    internal BaseNetworkBehaviour[] NetworkedBehaviours = new BaseNetworkBehaviour[0];

    internal BaseNetworkBehaviour[] NetickBehaviours = new BaseNetworkBehaviour[0];

    //internal NetworkPlayer TempInputSource;

    private Relevancy predictionMode = Relevancy.InputSource;

    public Relevancy PredictionMode => predictionMode;

    /// <summary>
    /// The <see cref="NetworkObject"/> parent of this object.
    /// </summary>
    public NetworkObject Parent { get; private set; }

    internal Node AuthParent { get; private set; }

    internal int ParentId = -1;

    /// <summary>
    /// Returns true if this <see cref="InternalSandbox"/> is a client.
    /// </summary>
    public bool IsClient => Engine.IsClient;

    /// <summary>
    /// Returns true if this <see cref="InternalSandbox"/> is the server.
    /// </summary>
    public bool IsServer => Engine.IsServer;

    /// <summary>
    /// Returns true if this <see cref="InternalSandbox"/> is the owner of this Object. In this version of Netick: Server=Owner.
    /// </summary>
    public bool IsOwner => Engine.IsServer;

    /// <summary>
    /// Returns true if this <see cref="NetworkSandbox.LocalPlayer"/> is providing inputs for this <see cref="NetworkObject"/>.
    /// </summary>
    public bool IsInputSource => Entity.IsInputSource;

    /// <summary>
    /// Returns true if we neither provide inputs nor own this <see cref="Object"/>.
    /// </summary>
    public bool IsProxy => IsClient && !IsInputSource;

    /// <summary>
    /// Returns the source <see cref="NetworkPlayer"/> (<see cref="NetworkPeer"/>/<see cref="ServerConnection"/>) of inputs for this <see cref="NetworkObject"/>. If the source of inputs is remote (from a client) it returns that <see cref="ServerConnection"/>, while on the
    /// input source itself it returns the local <see cref="NetworkPlayer"/>.
    /// </summary>
    public NetworkPlayer InputSource { get => Entity.InputSource; set => Entity.InputSource = value; }

    /// <summary>
    /// Returns true if we are currently resimulating a previous input of the past. On the server, it always returns false since <b>only the clients resimulate</b>.
    /// </summary>
    public bool IsResimulating => Engine.IsResimulating;
    internal bool IsPredictable => IsInputSource || PredictionMode == Relevancy.Everyone;

    public bool IsSceneObject => SceneId != -1; //{ get; internal set; } public bool                         IsSceneObject           => SceneId  != -1;
    public bool IsPrefabObject => PrefabId != -1;
    public bool IsSpawnPredicted => SpawnPredictionKey.IsValid;
    public int GetSceneId() => SceneId;

    /// <summary>
    /// Returns true if this <see cref="NetworkObject"/> has been added to the simulation by Netick, and thus has a valid id.
    /// </summary>
    public bool HasValidId => Entity.NetworkId > -1;
    public int Id => Entity.NetworkId;

    INetickNetworkScript[] INetickEntity.NetworkScripts => NetworkedBehaviours;
    INetickScript[] INetickEntity.AllScripts => NetickBehaviours;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsPrefabChild() => PrefabIndex >= 0 && (!IsSceneObject);

    internal void OnSpawnPredictionSucceeded()
    {
        for (int i = 0; i < NetworkedBehaviours.Length; i++)
            NetworkedBehaviours[i].OnSpawnPredictionSucceeded();
    }

    public void Initialize(NetickEngine sandbox, Entity entity)
    {
        Engine = sandbox;
        Entity = entity;
        var p = entity.State;
        var behId = 0;

        foreach (var beh in NetworkedBehaviours)
        {
            beh.Entity = Entity;
            beh.Object = this;  //   beh.Engine           = sandbox;

            beh.BehaviourId = behId;

            beh.S = p;
            p += beh.InternalGetStateSizeWords();
            behId++;
        }
    }
    internal unsafe void Init()
    {
        for (int i = 0; i < NetworkedBehaviours.Length; i++)
            NetworkedBehaviours[i].InternalInit();
    }

    internal unsafe void InternalReset()
    {
        for (int i = 0; i < NetworkedBehaviours.Length; i++)
            NetworkedBehaviours[i].InternalReset();
    }

    public unsafe void NetworkAwake()
    {
        InternalReset();
    }

    internal void InitInternals(NetworkSandbox sandbox)
    {
        if (Node == null)
        {
            throw new Exception($"Netick: Cannot initialize NetworkObject that has no transform source.");
        }

        Sandbox = sandbox;

        var behaviours = Node.GetChildren<BaseNetworkBehaviour>();

        foreach (var beh in behaviours)
        {
            beh.Object = this;
            beh.Sandbox = sandbox;
        }

        NetickBehaviours = behaviours.ToArray();
        NetworkedBehaviours = behaviours.ToArray();

        InitParentData();
    }

#pragma warning disable
    void INetickEntity.NetworkRegister(Tick spawnTick, int id, NetworkPlayer us, SpawnPredictionKey spawnKey = default)
    {
        this.SpawnTick = spawnTick;
        this.SpawnPredictionKey = spawnKey;
        //NetickLogger.Log("NetworkRegister " +  id);

        // CHECK

        //if (GetParent() != null && (GetParent() as NetworkObject) != null)
        //    Parent = GetParent() as NetworkObject;

        // ----already commented out----

        //foreach (var networkEventListner in EventListners)
        //{
        //  networkEventListner.Init(Sandbox, this);
        //  Sandbox.Callbacks.Subscribe(networkEventListner);
        //}
        spawnKey = default;

        AuthParent = Node.GetParent();
        InitParentData();
    }
#pragma warning restore
    private void InitParentData()
    {
        // is prefab root or scene object

        // CHECK

        /*        if (GetParent() != null && (GetParent() as NetworkObject) != null)
                {
                    this.Parent = GetParent() as NetworkObject;
                    this.ParentId = Parent.Id;
                }
                else
                    this.ParentId = -2;*/

        //this.ParentId = -2;
    }
    /// <summary>
    /// <i><b>[Owner/InputSource Only]</b></i> Changes the parent of this object.
    /// </summary>
    /// <param name="obj">The object which will become the parent of this object.</param>
    public void SetParent(NetworkObject obj)
    {
        if (Engine.IsClient && !IsInputSource)
        {
            NetickLogger.LogError(Entity, "You must be either the owner or the input source of the object to be able to change its parent.");
            return;
        }

        if (IsPrefabChild())
        {
            NetickLogger.LogError(Entity, "You can't change the parent of a prefab child, only the root object.");
            return;
        }

        InternalSetParent(obj, false);
    }

    internal void InternalSetParent(NetworkObject parent, bool isServerSnapshot)
    {
        if (parent == Parent)
            return;

        if (parent != null)
        {
            if (Node.GetParent() != null)
                Node.GetParent().RemoveChild(Node);

            parent.Node.AddChild(Node);
            //transform.parent = parent.transform;

            ParentId = parent.Id;
        }
        else
        {
            if (Node.GetParent() != null)
                Node.GetParent().RemoveChild(Node);
            //transform.parent = null;
            ParentId = -1;
        }

        if (isServerSnapshot)
            AuthParent = Node.GetParent();

        var parentTrans = parent != null ? Node.GetParent() : null;

        Parent = parent;

        if (IsServer)
        {
            var meta = (EntityMeta*)Engine.GetEntityUserMeta(Entity);
            meta->ParentId = ParentId;
            Engine.SetEntityUserMetaDirty(Entity);
        }

    }

    public T GetBehaviour<T>() where T : BaseNetworkBehaviour
    {
        for (int i = 0; i < NetworkedBehaviours.Length; i++)
        {
            if (NetworkedBehaviours[i] is T)
                return NetworkedBehaviours[i] as T;
        }

        return null;
    }

    internal void InternalDestroy()
    {
        //foreach (var networkEventListner in EventListners)
        //  Sandbox.Callbacks.Unsubscribe(networkEventListner);
    }

    internal void Recycle()
    {
        SpawnTick = Tick.InvalidTick;
        SpawnPredictionKey = default;

        foreach (var comp in NetworkedBehaviours)
        {
            try
            {
                comp.NetworkReset();
            }
            catch (Exception exp)
            {
                NetickLogger.LogError(exp);
            }
        }
    }

    void INetickEntity.NetworkUnregister() { }

}
