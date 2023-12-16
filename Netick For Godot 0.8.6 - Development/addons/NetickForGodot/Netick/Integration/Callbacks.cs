// Copyright (c) 2023 Karrar Rahim. All rights reserved.

using System.Collections;
using System.Collections.Generic;
using System.Net;
using Godot;

namespace Netick.GodotEngine
{

  public delegate void OnConnectRequestDelegate(NetworkSandbox sandbox, NetworkConnectionRequest request);
  public delegate void OnConnectFailedDelegate(NetworkSandbox sandbox, ConnectionFailedReason reason);
  public delegate void OnStartupDelegate(NetworkSandbox sandbox);
  public delegate void OnShutdownDelegate(NetworkSandbox sandbox);
  public delegate void OnInputReadDelegate(NetworkSandbox sandbox);


  public delegate void OnSceneLoadedDelegate(NetworkSandbox sandbox);
  public delegate void OnSceneLoadStartedDelegate(NetworkSandbox sandbox);
  public delegate void OnClientSceneLoadedDelegate(NetworkSandbox sandbox, NetworkConnection client);

  public delegate void OnObjectCreatedDelegate(NetworkObject entity);
  public delegate void OnEntityDestroyedDelegate(NetworkObject entity);

  public delegate void OnConnectedToServerDelegate(NetworkSandbox sandbox, NetworkConnection server);
  public delegate void OnDisconnectedFromServerDelegate(NetworkSandbox sandbox, NetworkConnection server, TransportDisconnectReason transportDisconnectReason);


  public delegate void OnClientConnectedDelegate(NetworkSandbox sandbox, NetworkConnection server);
  public delegate void OnClientDisconnectedDelegate(NetworkSandbox sandbox, NetworkConnection server, TransportDisconnectReason transportDisconnectReason);

  public delegate void OnMatchListUpdateDelegate(NetworkSandbox sandbox, List<Session> sessions);
  public delegate void OnMatchCreatedDelegate(NetworkSandbox sandbox);

  internal class NetickCallbacks
  {

    private NetworkSandbox Sandbox;

    internal NetickCallbacks(NetworkSandbox sandbox)
    {
      this.Sandbox = sandbox;
    }

    // Events
    public event OnConnectRequestDelegate OnConnectRequestEvent;
    public event OnConnectFailedDelegate OnConnectFailedEvent;
    public event OnStartupDelegate OnStartupEvent;
    public event OnShutdownDelegate OnShutdownEvent;
    public event OnInputReadDelegate OnInputReadEvent;

    public event OnSceneLoadedDelegate OnSceneLoadedEvent;
    public event OnSceneLoadStartedDelegate OnSceneLoadStartedEvent;
    public event OnClientSceneLoadedDelegate OnClientSceneLoadedEvent;

    public event OnObjectCreatedDelegate OnObjectCreatedEvent;
    public event OnObjectCreatedDelegate OnObjectDestroyedEvent;

    public event OnClientConnectedDelegate OnClientConnectedEvent;
    public event OnClientDisconnectedDelegate OnClientDisconnectedEvent;

    public event OnConnectedToServerDelegate OnConnectedToServerEvent;
    public event OnDisconnectedFromServerDelegate OnDisconnectedFromServerEvent;



    public event OnMatchListUpdateDelegate OnMatchListUpdateEvent;
    public event OnMatchCreatedDelegate OnMatchCreatedEvent;


    // ---------------------------------------------------------------

    public void OnMatchListUpdate(NetworkSandbox sandbox, List<Session> sessions) => OnMatchListUpdateEvent?.Invoke(sandbox, sessions);
    public void OnMatchCreated(NetworkSandbox sandbox) => OnMatchCreatedEvent?.Invoke(sandbox);

    public bool OnConnectRequest(NetworkSandbox sandbox, NetworkConnectionRequest request)
    {
      OnConnectRequestEvent?.Invoke(sandbox, request);
      return request.AllowConnection;
    }

    public void OnConnectFailed(NetworkSandbox sandbox, ConnectionFailedReason reason) => OnConnectFailedEvent?.Invoke(sandbox, reason);

    public void OnStartup(NetworkSandbox sandbox) => OnStartupEvent?.Invoke(sandbox);
    public void OnShutdown(NetworkSandbox sandbox) => OnShutdownEvent?.Invoke(sandbox);
    public void OnInputRead(NetworkSandbox sandbox) => OnInputReadEvent?.Invoke(sandbox);

    public void OnSceneLoaded(NetworkSandbox sandbox) => OnSceneLoadedEvent?.Invoke(sandbox);
    public void OnSceneLoadStarted(NetworkSandbox sandbox) => OnSceneLoadStartedEvent?.Invoke(sandbox);
    public void OnClientSceneLoaded(NetworkSandbox sandbox, NetworkConnection client) => OnClientSceneLoadedEvent?.Invoke(sandbox, client);

    public void OnConnectedToServer(NetworkSandbox sandbox, NetworkConnection server) => OnConnectedToServerEvent?.Invoke(sandbox, server);
    public void OnDisconnectedFromServer(NetworkSandbox sandbox, NetworkConnection server, TransportDisconnectReason transportDisconnectReason) => OnDisconnectedFromServerEvent?.Invoke(sandbox, server, transportDisconnectReason);

    public void OnClientConnected(NetworkSandbox sandbox, NetworkConnection client) => OnClientConnectedEvent?.Invoke(sandbox, client);
    public void OnClientDisconnected(NetworkSandbox sandbox, NetworkConnection client, TransportDisconnectReason transportDisconnectReason) => OnClientDisconnectedEvent?.Invoke(sandbox, client, transportDisconnectReason);

    public void OnObjectCreated(NetworkObject obj) => OnObjectCreatedEvent?.Invoke(obj);
    public void OnObjectDestroyed(NetworkObject obj) => OnObjectDestroyedEvent?.Invoke(obj);

    internal List<NetworkEventsListener> Listners = new List<NetworkEventsListener>();

    public void Subscribe(NetworkEventsListener eventListner)
    {
      if (!Listners.Contains(eventListner))
      {
        eventListner.Sandbox = Sandbox;

        OnConnectRequestEvent += eventListner.OnConnectRequest;
        OnConnectFailedEvent += eventListner.OnConnectFailed;
        OnStartupEvent += eventListner.OnStartup;
        OnShutdownEvent += eventListner.OnShutdown;
        OnInputReadEvent += eventListner.OnInput;

        OnConnectedToServerEvent += eventListner.OnConnectedToServer;
        OnDisconnectedFromServerEvent += eventListner.OnDisconnectedFromServer;

        OnClientConnectedEvent += eventListner.OnClientConnected;
        OnClientDisconnectedEvent += eventListner.OnClientDisconnected;

        OnObjectCreatedEvent += eventListner.OnObjectCreated;
        OnObjectDestroyedEvent += eventListner.OnObjectDestroyed;

        OnSceneLoadedEvent += eventListner.OnSceneLoaded;
        OnSceneLoadStartedEvent += eventListner.OnSceneLoadStarted;
        OnClientSceneLoadedEvent += eventListner.OnClientSceneLoaded;

        OnMatchListUpdateEvent += eventListner.OnMatchListUpdate;
        OnMatchCreatedEvent += eventListner.OnMatchCreated;

        Listners.Add(eventListner);
      }
    }

    public void Unsubscribe(NetworkEventsListener eventListner)
    {
      if (Listners.Contains(eventListner))
      {
        OnConnectRequestEvent -= eventListner.OnConnectRequest;
        OnConnectFailedEvent -= eventListner.OnConnectFailed;
        OnStartupEvent -= eventListner.OnStartup;
        OnShutdownEvent -= eventListner.OnShutdown;
        OnInputReadEvent -= eventListner.OnInput;

        OnConnectedToServerEvent -= eventListner.OnConnectedToServer;
        OnDisconnectedFromServerEvent -= eventListner.OnDisconnectedFromServer;

        OnClientConnectedEvent -= eventListner.OnClientConnected;
        OnClientDisconnectedEvent -= eventListner.OnClientDisconnected;

        OnObjectCreatedEvent -= eventListner.OnObjectCreated;
        OnObjectDestroyedEvent -= eventListner.OnObjectDestroyed;

        OnSceneLoadedEvent -= eventListner.OnSceneLoaded;
        OnSceneLoadStartedEvent -= eventListner.OnSceneLoadStarted;
        OnClientSceneLoadedEvent -= eventListner.OnClientSceneLoaded;

        OnMatchListUpdateEvent -= eventListner.OnMatchListUpdate;
        OnMatchCreatedEvent -= eventListner.OnMatchCreated;

        Listners.Remove(eventListner);

        eventListner.Sandbox = null;
      }

    }

    public void Reset()
    {
      for (int i = Listners.Count - 1; i >= 0; i--)
      {
        if (Listners[i].Object != null /*&& !Listners[i].Object.IsValidPersistentObject*/)
          continue;

        Unsubscribe(Listners[i]);
      }


      Listners.Clear();
    }

  }
}

