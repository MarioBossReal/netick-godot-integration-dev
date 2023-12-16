// Copyright (c) 2023 Karrar Rahim. All rights reserved.

using System;
using System.Collections.Generic;
using Godot;
using Netick;

namespace Netick.GodotEngine
{
    public unsafe abstract partial class NetworkEventsListener : Node
  {
    public NetworkSandbox  Sandbox { get; internal set; }
    internal NetworkObject Object  { get; set; }

    internal void Init(NetworkSandbox sandbox, NetworkObject obj)
    {
      this.Sandbox = sandbox;
      this.Object = obj;
    }

    /// <summary>
    /// Called on the server when a client tries to connect. Use <paramref name="request"/> to decide whether or not to allow this client to connect.
    /// </summary>
    /// <param name="sandbox"></param>
    /// <param name="request"></param>
    public virtual void OnConnectRequest(NetworkSandbox sandbox, NetworkConnectionRequest request) { }

    /// <summary>
    /// Called on the client when the connection to the server was refused, or simply failed.
    /// </summary>
    /// <param name="sandbox"></param>
    /// <param name="reason"></param>

    public virtual void OnConnectFailed(NetworkSandbox sandbox, ConnectionFailedReason reason) { }

    /// <summary>
    /// Called to read inputs.
    /// </summary>
    /// <param name="sandbox"></param>

    public virtual void OnInput(NetworkSandbox sandbox) { }

    /// <summary>
    /// Called when Netick has been started.
    /// </summary>
    /// <param name="sandbox"></param>

    public virtual void OnStartup(NetworkSandbox sandbox) { }

    /// <summary>
    /// Called when Netick has been shut down.
    /// </summary>
    /// <param name="sandbox"></param>

    public virtual void OnShutdown(NetworkSandbox sandbox) { }

    /// <summary>
    /// Called on both the client and the server when the scene has been loaded.
    /// </summary>
    /// <param name="sandbox"></param>

    public virtual void OnSceneLoaded(NetworkSandbox sandbox) { }

    /// <summary>
    /// Called on both the client and the server before beginning to load the new scene.
    /// </summary>
    /// <param name="sandbox"></param>

    public virtual void OnSceneLoadStarted(NetworkSandbox sandbox) { }

    /// <summary>
    /// Called on the server when a specific client finished loading the scene.
    /// </summary>
    /// <param name="sandbox"></param>
    /// <param name="client"></param>

    public virtual void OnClientSceneLoaded(NetworkSandbox sandbox, NetworkConnection client) { }

    /// <summary>
    /// Called on the client when connection to the server has been initiated.
    /// </summary>
    /// <param name="sandbox"></param>
    /// <param name="server"></param>

    public virtual void OnConnectedToServer(NetworkSandbox sandbox, NetworkConnection server) { }

    /// <summary>
    /// Called on the client when connection to the server ended, or when a network error caused the disconnection.
    /// </summary>
    /// <param name="sandbox"></param>
    /// <param name="server"></param>

    public virtual void OnDisconnectedFromServer(NetworkSandbox sandbox, NetworkConnection server, TransportDisconnectReason transportDisconnectReason) { }

    /// <summary>
    /// Called on the server when a specific client has connected.
    /// </summary>
    /// <param name="sandbox"></param>
    /// <param name="client"></param>

    public virtual void OnClientConnected(NetworkSandbox sandbox, NetworkConnection client) { }

    /// <summary>
    /// Called on the server when a specific client has disconnected.
    /// </summary>
    /// <param name="sandbox"></param>
    /// <param name="client"></param>

    public virtual void OnClientDisconnected(NetworkSandbox sandbox, NetworkConnection client, TransportDisconnectReason transportDisconnectReason) { }

    /// <summary>
    /// Called when a network object has been created/initialized. 
    /// </summary>
    /// <param name="entity"></param>

    public virtual void OnObjectCreated(NetworkObject entity) { }

    /// <summary>
    /// Called when a network object has been destroyed/recycled.
    /// </summary>
    /// <param name="entity"></param>
    public virtual void OnObjectDestroyed(NetworkObject entity) { }


    public virtual void OnMatchListUpdate(NetworkSandbox sandbox, List<Session> sessions) { }

    public virtual void OnMatchCreated(NetworkSandbox sandbox) { }

    public override void _ExitTree()
    {
      Sandbox?.Callbacks.Unsubscribe(this);
    }

    public void UnlinkFromNetick()
    {
      Sandbox.Callbacks.Unsubscribe(this);
    }

  }

}