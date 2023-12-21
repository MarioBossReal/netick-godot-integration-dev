// Copyright (c) 2023 Karrar Rahim. All rights reserved.

using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Netick.GodotEngine;

public unsafe partial class NetworkObject : INetickEntity
{
    public NetworkSandbox Sandbox { get; private set; }
    public NetickEngine Engine { get; internal set; }
    public Entity Entity { get; private set; }

    public int NetworkId
    {
        get
        {
            if (Entity == null)
                return -1;
            return Entity.NetworkId;
        }
    }

    /// <summary>
    /// The tick which this object was spawned at.
    /// </summary>
    public Tick SpawnTick { get; internal set; } = Tick.InvalidTick;
    public SpawnPredictionKey SpawnPredictionKey { get; private set; }

    [Export]
    public Node TransformSource;
    [Export]
    internal NetworkObject[] BakedInternalPrefabChildren = new NetworkObject[0];
    [Export]
    internal NetworkObject BakedInternalPrefabRoot;
    // [Export]
    internal int SceneId = 0;
    // [Export]
    internal int PrefabId = -1;

    //[Export]
    internal int PrefabIndex = -1;


    internal bool IsPrefabInstance = false;
    internal int InstanceCounter = 0;

    //internal List<NetworkEventsListner> EventListners           = new();
    internal BaseNetworkBehaviour[] NetworkedBehaviours = new BaseNetworkBehaviour[0];
    internal BaseNetworkBehaviour[] NetickBehaviours = new BaseNetworkBehaviour[0];
    // internal NetworkPlayer              TempInputSource;

    // [SerializeField]
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
        if (TransformSource == null)
        {
            throw new Exception($"Netick: Cannot initialize NetworkObject that has no transform source.");
        }

        this.Sandbox = sandbox;
        var listdd = new List<BaseNetworkBehaviour>();
        GetBehaviours(TransformSource, listdd); //   GetBehaviours(Actor, EventListners);
        NetickBehaviours = listdd.ToArray();//  NetickLogger.Log("INIT -- Behs" + listdd.Count);

        var listo = new List<BaseNetworkBehaviour>();
        for (int i = 0; i < NetickBehaviours.Length; i++)
        {
            if (NetickBehaviours[i] as BaseNetworkBehaviour != null)
                listo.Add(NetickBehaviours[i] as BaseNetworkBehaviour);
        }

        NetworkedBehaviours = listo.ToArray();//this.NetworkedBehaviours.Sort((x, y) => GetBehOrder(x).CompareTo(GetBehOrder(y)));

        for (int i = 0; i < NetickBehaviours.Length; i++)
        {
            this.NetickBehaviours[i].Object = this;
            this.NetickBehaviours[i].Sandbox = sandbox;
            //   this.NetickBehaviours[i].Engine = sandbox.InternalSandbox;
        }

        InitParentData();
    }

    static void GetBehaviours<T>(Node obj, List<T> behaviourList) where T : Node
    {
        var myChilds = obj.GetChildren();

        foreach (var child in myChilds)
        {
            if (child as T != null)
                behaviourList.Add(child as T);  // (child as NetworkBehaviour).Object = obj;

            // CHECK

            //if (child as NetworkObject == null)
            //GetBehaviours(child, behaviourList);
        }
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

        AuthParent = TransformSource.GetParent();
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

        this.ParentId = -2;
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
            if (TransformSource.GetParent() != null)
                TransformSource.GetParent().RemoveChild(TransformSource);

            parent.TransformSource.AddChild(TransformSource);
            //transform.parent = parent.transform;

            ParentId = parent.Id;
        }
        else
        {
            if (TransformSource.GetParent() != null)
                TransformSource.GetParent().RemoveChild(TransformSource);
            //transform.parent = null;
            ParentId = -1;
        }

        if (isServerSnapshot)
            AuthParent = TransformSource.GetParent();

        var parentTrans = parent != null ? TransformSource.GetParent() : null;

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
