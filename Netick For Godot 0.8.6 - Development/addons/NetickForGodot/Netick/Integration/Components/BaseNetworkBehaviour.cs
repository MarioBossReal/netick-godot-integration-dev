// Copyright (c) 2023 Karrar Rahim. All rights reserved.

using Godot;
using System.Collections.Generic;

namespace Netick.GodotEngine;

public unsafe abstract partial class BaseNetworkBehaviour : Node, INetickScript
{
    /// <summary>
    /// The <see cref="NetworkSandbox"/> containing this <see cref="Object"/>.
    /// </summary>
    public NetworkSandbox Sandbox { get; internal set; }

    /// <summary>
    /// The <see cref="NetworkObject"/> this behaviour is attached to.
    /// </summary>
    public NetworkObject Object { get; internal set; }

    public Entity Entity { get; internal set; }

    public NetickEngine Engine { get; private set; }

    /// <summary>
    /// The type-qualified node that this behaviour is attached to.
    /// </summary>

    void INetickScript.Initialize(Netick.NetickEngine sandbox)
    {
        Engine = sandbox;
    }

    public unsafe virtual void NetworkAwake() { }
    /// <summary>
    /// Called when this behaviour has been added to the simulation. 
    /// </summary>

    public unsafe virtual void NetworkStart() { }

    /// <summary>
    /// Called when this behaviour has been removed from the simulation.
    /// </summary>

    public unsafe virtual void NetworkDestroy() { }

    /// <summary>
    /// Called every frame. Executed before NetworkFixedUpdate.
    /// </summary>

    public unsafe virtual void NetworkUpdate() { }

    /// <summary>
    /// Called every fixed-time network update/tick. Any changes/updates to the network state must happen here.
    /// <para>On the client, if you are the Input Source or if this Object.PredictionMode is set to Everyone, it will be called several times in one update/tick during resimulations. To check for resimulations, use [<see cref="IsResimulating"/>].</para> 
    /// </summary>

    public unsafe virtual void NetworkFixedUpdate() { }

    /// <summary>
    /// Called every frame. Executed after NetworkUpdate and NetworkFixedUpdate.
    /// </summary>

    public unsafe virtual void NetworkRender() { }
}

public unsafe abstract partial class BaseNetworkBehaviour : Node, INetickNetworkScript
{
    List<NetworkRpc> INetickNetworkScript.RelatedRpcs => _RelatedRPCs;
    int INetickNetworkScript.Index => BehaviourId;
    int* INetickNetworkScript.State => S;

    public int* S;

    internal List<NetworkRpc> _RelatedRPCs = new List<NetworkRpc>(32);

    /// <summary>
    /// The network id of this <see cref="NetworkBehaviour"/>.
    /// </summary>

    public int BehaviourId { get; internal set; }

    /// <summary>
    /// The network id of this <see cref="Object"/>.
    /// </summary>

    public int Id => Entity.NetworkId;

    /// <summary>
    /// Returns true if this <see cref="Sandbox"/> is a client.
    /// </summary>

    public bool IsClient => Engine.IsClient;

    /// <summary>
    /// Returns true if this <see cref="Sandbox"/> is the server.
    /// </summary>

    public bool IsServer => Engine.IsServer;

    /// <summary>
    /// Returns true if this <see cref="Sandbox"/> is the owner of this Object. In this version of Netick: Server=Owner.
    /// </summary>

    public bool IsOwner => Engine.IsServer;

    /// <summary>
    /// Returns true if this <see cref="NetworkSandbox.LocalPlayer"/> is providing inputs to this Object.
    /// </summary>

    public bool IsInputSource => Object.IsInputSource;

    /// <summary>
    /// Returns true if we neither provide inputs nor own this <see cref="Object"/>.
    /// </summary>

    public bool IsProxy => Object.IsProxy;
    /// <summary>
    /// Returns true if we are currently resimulating a previous input of the past. On the server, it always returns false since <b>only the clients resimulate</b>.
    /// </summary>

    public bool IsResimulating => Engine.IsResimulating;

    /// <summary>
    /// Fetchs a network input for this tick. Returns false if no input source is currently providing inputs to this Object, or when input loss occurs (in case of a remote input source).
    /// </summary>

    public bool FetchInput<T>(out T input) where T : unmanaged => Engine.FetchInput<T>(out input, Entity);

    public Interpolator FindInterpolator<T>(string propertyName) where T : unmanaged
    {
        var proName = $"{this.GetType().Name}_{propertyName}";

        if (Entity.ObjectMeta.PropertyNameToDataOffset.TryGetValue(proName, out int offsetWords))
            return new Interpolator(Entity, S, offsetWords);

        return default;
    }

    public unsafe virtual void GameEngineIntoNetcode() { }
    public unsafe virtual void NetcodeIntoGameEngine() { }
    public unsafe virtual void NetworkReset() { }
    public virtual void OnInputSourceChanged(NetworkPlayer previous) { }

    /// <summary>
    /// Called on the server when the input source of this Object has disconnected.
    /// </summary>

    public virtual void OnInputSourceLeft() { }

    /// <summary>
    /// Called on the client when the server confirms that this object has been successfully spawn-predicted and therefore has a valid Id.
    /// </summary>

    internal virtual void OnSpawnPredictionSucceeded() { }
    public unsafe virtual void InternalInit() { }
    public unsafe virtual void InternalReset() { }
    public unsafe virtual int InternalGetStateSizeWords() { return 0; }
}
