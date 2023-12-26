// Copyright (c) 2023 Karrar Rahim. All rights reserved.

using Godot;
using Netick.GodotEngine.Constants;
using Netick.GodotEngine.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Netick.GodotEngine;

internal class GodotLogger : INetickLogger
{
    public void LogError(object message) => GD.PrintErr(message);
    public void LogWarning(object message) => GD.Print(message);
    public void Log(object message) => GD.Print(message);
}

internal struct ConnectionMeta
{
    internal short CurrentSceneIndex;
    internal short SceneOperationsCounter;
    internal bool HasLoadedScene;
}

internal struct EntityMeta
{
    internal int CreateId;
    internal int ParentId;
    internal Tick SpawnTick;
    internal SpawnPredictionKey SpawnKey;
    internal bool IsPrefab;
    internal int PrefabChildIndex;
    internal int PrefabRootId;

    internal Vector3 Pos;
    internal Quaternion Rot;
}

public unsafe partial class NetworkSandbox : Node, IGameEngine
{
    ExecutionList IGameEngine.GetPredictionInterestList() { return null; }
    void IGameEngine.SyncTransformsToPhysics() { }
    void IGameEngine.SimulatePhysics()
    {
        //if (!IsClientNotReady)  Physics.Simulate(UnityEngine.Time.fixedDeltaTime);
    }
    int IGameEngine.GetConnectionMetaSizeWords() => NetickUtils.GetWordSize(sizeof(ConnectionMeta));
    int IGameEngine.GetEntityMetaSizeWords() => NetickUtils.GetWordSize(sizeof(EntityMeta));
    void IGameEngine.OnInputRead() => Callbacks.OnInputRead(this);
    void IGameEngine.OnNetworkUpdate() { }
    void IGameEngine.OnNetworkRender() { }
    void IGameEngine.OnNetworkFixedUpdate() { }

    private Dictionary<NetworkConnection, ConnectionMeta> PreivousConnectionMetas = new(512);

    void IGameEngine.OnPacketReceived(NetworkConnection source)
    {
        // return;
        var connData = *(ConnectionMeta*)source.RemoteUserData;
        var connDataPre = PreivousConnectionMetas[source];

        if (Engine.IsClient)
        {
            if (SceneReloadFlag || LevelIndex != connData.CurrentSceneIndex || SceneOperationsCounter != connData.SceneOperationsCounter)
            {
                if (HasLoadLevel)
                {
                    //NetickLogger.Log($"SWITCH SCENE {SceneReloadFlag} -- {SceneBuildIndex != connData.CurrentSceneIndex} -- {SceneOperationsCounter != connData.SceneOperationsCounter}");
                    RemoteSceneSwitch((short)connData.CurrentSceneIndex);
                }
            }
        }

        else // Is Server
        {
            var a = connDataPre.CurrentSceneIndex != connData.CurrentSceneIndex;
            var b = connDataPre.SceneOperationsCounter != connData.SceneOperationsCounter;
            var c = connDataPre.HasLoadedScene != connData.HasLoadedScene;
            var k = connData.CurrentSceneIndex == LevelIndex && connData.SceneOperationsCounter == SceneOperationsCounter;

            try
            {
                if ((a || b || c) && k && connData.HasLoadedScene)
                {
                    Callbacks.OnClientSceneLoaded(this, source);   //UnityEngine.Debug.Log("^^^^^^^^^^^^^^^^^^^^ client loaded scene");
                }
            }
            catch (Exception exp)
            {
                NetickLogger.LogError(exp);
            }
        }

        PreivousConnectionMetas[source] = connData;
    }

    void IGameEngine.OnConnectFailed(Netick.ConnectionFailedReason reason)
    {
        Callbacks.OnConnectFailed(this, reason);
    }
    void IGameEngine.OnConnectRequest(Netick.NetworkConnectionRequest request)
    {
        Callbacks.OnConnectRequest(this, request);
    }
    void IGameEngine.OnBeforeSend(NetworkConnection connoo)
    {
        //connoo.SendEntities = true;
        //return;
        if (IsServer)
        {
            var connData = (ConnectionMeta*)connoo.LocalUserData;
            connData->CurrentSceneIndex = LevelIndex;
            connData->SceneOperationsCounter = SceneOperationsCounter;
            connData->HasLoadedScene = HasLoadLevel;

            var connDataRemote = (ConnectionMeta*)connoo.RemoteUserData;
            var hasLoadedOurScene = (Engine.IsClient ? !SceneReloadFlag : true) && connDataRemote->HasLoadedScene && connDataRemote->CurrentSceneIndex == LevelIndex && connDataRemote->SceneOperationsCounter == SceneOperationsCounter;

            //if (!hasLoadedOurScene)
            //{
            //  GD.Print("SEND ENTITIES: " + hasLoadedOurScene);

            //  GD.Print($"connDataRemote->HasLoadedScene {connDataRemote->HasLoadedScene}                 connDataRemote->CurrentSceneIndex: {connDataRemote->CurrentSceneIndex}       SceneIndex{SceneIndex}          connDataRemote->SceneOperationsCounter{connDataRemote->SceneOperationsCounter}     SceneOperationsCounter{SceneOperationsCounter}  ");

            //}


            connoo.SendEntities = hasLoadedOurScene;
        }
        else
        {
            var connData = (ConnectionMeta*)connoo.LocalUserData;
            connData->CurrentSceneIndex = LevelIndex;
            connData->SceneOperationsCounter = SceneOperationsCounter;
            connData->HasLoadedScene = HasLoadLevel;
        }
    }

    void IGameEngine.OnPeerConnected(NetworkConnection conn)
    {
        var connDataLocal = (ConnectionMeta*)conn.LocalUserData;
        connDataLocal->CurrentSceneIndex = -1;
        connDataLocal->SceneOperationsCounter = 0;
        connDataLocal->HasLoadedScene = false;

        var connDataRemote = (ConnectionMeta*)conn.RemoteUserData;
        connDataRemote->CurrentSceneIndex = -1;
        connDataRemote->SceneOperationsCounter = 0;
        connDataRemote->HasLoadedScene = false;

        PreivousConnectionMetas.Add(conn, *connDataLocal);

        if (Engine.IsClient)
            Callbacks.OnConnectedToServer(this, conn);
        else
            Callbacks.OnClientConnected(this, conn);
    }

    void IGameEngine.OnPeerDisconnected(NetworkConnection conn, TransportDisconnectReason transportDisconnectReason)
    {
        PreivousConnectionMetas.Remove(conn);

        if (Engine.IsClient)
            Callbacks.OnDisconnectedFromServer(this, conn, transportDisconnectReason);
        else
            Callbacks.OnClientDisconnected(this, conn, transportDisconnectReason);
    }

    void IGameEngine.OnClientConnected() { }

    void IGameEngine.OnClientDisconnected(TransportDisconnectReason transportDisconnectReason)
    {
        SceneReloadFlag = true;
        InternalReset();
    }

    void IGameEngine.PopulateEntityMeta(Entity ent)
    {
        var entity = (NetworkObject)ent.UserEntity;
        var userData = (EntityMeta*)Engine.GetEntityUserMeta(entity.Entity);
        userData->CreateId = entity.IsPrefabObject ? entity.PrefabId : entity.GetSceneId();
        userData->ParentId = entity.ParentId;
        userData->SpawnKey = entity.SpawnPredictionKey;
        userData->SpawnTick = entity.SpawnTick;
        userData->PrefabChildIndex = entity.PrefabIndex;
        userData->IsPrefab = entity.IsPrefabObject;
        userData->PrefabRootId = entity.PrefabIndex != -1 ? entity.InternalPrefabRoot.Id : -1;

        if (entity.Node != null)
        {
            var node3D = entity.Node as Node3D;
            var node2D = entity.Node as Node2D;

            if (node3D != null)
            {
                userData->Pos = node3D.Position;
                userData->Rot = node3D.Quaternion;
            }
            else if (node2D != null)
            {
                userData->Pos = new Vector3(node2D.Position.X, node2D.Position.Y, 0);
                userData->Rot = new Quaternion(node2D.Rotation, 0, 0, 0);
            }
        }
    }

    void IGameEngine.OnEntitySpawned(Entity ent)
    {
        //NetickLogger.Log($"ENTITY SPAWN: Id: {ent.NetworkId}      Behs:{ent.Scripts.Length}");
        var entity = (NetworkObject)ent.UserEntity;
        Entities.Add(ent.NetworkId, entity);

        //if (!NodeToNetworkObject.ContainsKey(entity.Node))
        //NodeToNetworkObject.Add(entity.Node, entity);

        //entity.Node.SetMeta(MetaConstants.NetworkObject, entity);

        Callbacks.OnObjectCreated(entity);
    }

    void IGameEngine.OnEntityDespawned(Entity ent)
    {
        var entity = (NetworkObject)ent.UserEntity;
        RecycleEntity(entity);
    }

    void IGameEngine.OnEntityMetaChanged(NetickEntityMeta netickMeta, byte* metaData)
    {
        //return;
        var userData = (EntityMeta*)metaData;
        var id = netickMeta.Id;
        var isUsable = false;   //var isUsable = netickMeta->InputSourceId;
                                //NetickLogger.Log("ID " + id + " --- sceneId: " + userData->CreateId);
                                //  NetickLogger.Log("ID " + id + " --- ISDESTROYED: " + (netickMeta.IsDestroyed == 1));

        if (id == -1)
            return;

        if (Entities.TryGetValue(id, out var oldEnt))
        {
            if (netickMeta.IsDestroyed == 1 || oldEnt.InstanceCounter != netickMeta.InstanceCounter)
            {
                if (!oldEnt.IsPrefabChild())
                    NetworkDestroyForClient(oldEnt);
            }
        }

        if (netickMeta.IsDestroyed == 1)
            return;

        // is prefab
        if (userData->IsPrefab)
        {
            if (userData->PrefabChildIndex != -1)
            {
                //NetickLogger.Log($"CHILD: ROOT ID {userData->PrefabRootId}  --- Child Index {userData->PrefabChildIndex}--- Child ID {id} --- Create Id {userData->CreateId}");
                if (!Entities.ContainsKey(userData->PrefabRootId))
                {
                    Vector3 rootPos = default;
                    Quaternion rootRot = Quaternion.Identity;
                    var obj = InstantiatePrefabRootForClient(netickMeta.InstanceCounter, userData->SpawnTick, rootPos, rootRot, userData->PrefabRootId, userData->CreateId, userData->SpawnKey, false);
                    obj.InstanceCounter = netickMeta.InstanceCounter;
                }

                var child = LinkPrefabChildForClient(Entities[userData->PrefabRootId], userData->PrefabChildIndex, id, isUsable);
                if (child != null)
                    child.InstanceCounter = netickMeta.InstanceCounter;
            }
            else
            {

                if (!Entities.ContainsKey(id))
                {
                    Vector3 pos = userData->Pos;
                    Quaternion rot = userData->Rot;
                    //Vector3    pos      = default;
                    //Quaternion rot      = Quaternion.identity;
                    var obj = InstantiatePrefabRootForClient(netickMeta.InstanceCounter, userData->SpawnTick, pos, rot, id, userData->CreateId, userData->SpawnKey, isUsable);
                    obj.InstanceCounter = netickMeta.InstanceCounter;
                }
            }
        }

        else // is scene object
        {
            if (!Entities.ContainsKey(id))
            {
                Engine.ClientAddEntity(userData->SpawnTick, SceneObjects[userData->CreateId], id, isUsable ? Engine.LocalPeer : null, true, true, default);
                SceneObjects[userData->CreateId].InstanceCounter = netickMeta.InstanceCounter;
            }
        }
    }

    void IGameEngine.OnEntityMetaChangedPhaseTwo(NetickEntityMeta netickMeta, byte* metaData)
    {
        var userData = (EntityMeta*)metaData;
        if (netickMeta.Id == -1 || netickMeta.IsDestroyed == 1)
            return;

        if (Entities.TryGetValue(netickMeta.Id, out var entity))
        {
            if (userData->ParentId != -2)
            {
                if (userData->ParentId == -1)
                    entity.InternalSetParent(null, true);
                else
                    entity.InternalSetParent(Entities[userData->ParentId], true);
            }
        }
    }

    internal void RemoteSceneSwitch(short sceneBuildIndex)
    {
        var path = Network.Instance.ScenesIdToPath[sceneBuildIndex];
        StartSceneSwitch(path);

        //if (Network.Config.UseSceneSwitchThread)
        //{
        //  SceneSwitchThread = new SceneSwitchThread(this);
        //  SceneSwitchThread.Start();
        //  //Thread.Sleep(2000);
        //}
    }

    internal unsafe void LoadTheWorld(Node scene, short buildIndex, bool firstTime, bool createNewScene)
    {
        //lock (SceneSwitchLock)
        {
            Level = scene;
            LevelIndex = buildIndex;

            if (!firstTime && Engine.IsClient)
            {
                var serverConnMeta = (ConnectionMeta*)Engine.Client.ConnectedServer.RemoteUserData;

                SceneOperationsCounter = (short)serverConnMeta->SceneOperationsCounter;
                serverConnMeta->CurrentSceneIndex = LevelIndex;
                serverConnMeta->HasLoadedScene = true;
            }

            //EnsureSingleInstances();  // TEMP - no persisitent global listners

            //if (IsPhysicsStepedByNetick)
            //  NetPhysics.FindAllRigidbodies(Scene);

            GetNetworkListeners(Level);
            HasLoadLevel = true;
            SceneReloadFlag = false;

            // if (!firstTime)
            //InitScene();

            foreach (var pool in NetworkObjectPools.Values)
                pool.Reload();
            var sceneRootObjects = Level.GetChildren();

            foreach (var obj in sceneRootObjects)
                InitializeSceneObjectChildren(obj);

            Engine.DrainPendingObjects();

            // OnSceneSwitchDone();

            //if (Network.Instance.StartMode == StartMode.ServerAndClient)
            //  AddObjects(FindObjectsOfType<Component>(true));

            // TODO---------------------
            try
            {
                Callbacks.OnSceneLoaded(this);
            }
            catch (Exception e)
            {
                NetickLogger.LogError(Engine, e);
            }

            Engine.ResetForSceneSwitch();
        }
    }

    Queue<string> _loadingScenes = new Queue<string>();
    string _currentSceneLoadingOperation = "";

    internal void StartSceneSwitch(string sceneBuildIndex)
    {
        if (_currentSceneLoadingOperation == "")
        {
            OnSceneSwitchStart(sceneBuildIndex);
            //CheckSceneLoadingProgress();
        }
        else
        {
            _loadingScenes.Enqueue(sceneBuildIndex);
        }
    }

    internal void OnSceneSwitchStart(string sceneBuildIndex)
    {
        if (_loadingScenes.Contains(sceneBuildIndex))
            return;

        //foreach (var comp in GetComponents<ResetOnSceneSwitch>())
        //  comp.OnSceneSwitchStart();

        Callbacks.OnSceneLoadStarted(this);

        DestroySceneObjects();
        Level.QueueFree();
        Level = null;

        if (Engine.IsClient)
            Engine.RemoteInterpolation.Reset();

        Callbacks.Reset();

        _currentSceneLoadingOperation = sceneBuildIndex;
        HasLoadLevel = false;
        ResourceLoader.LoadThreadedRequest(sceneBuildIndex);       //StaticUnitySceneManager.SwitchScene(sceneBuildIndex, this);
    }


    private void CheckSceneLoadingProgress()
    {
        if (_currentSceneLoadingOperation != "")
        {
            if (ResourceLoader.LoadThreadedGetStatus(_currentSceneLoadingOperation) == ResourceLoader.ThreadLoadStatus.Loaded)
            {
                var newSceneRes = ResourceLoader.LoadThreadedGet(_currentSceneLoadingOperation) as PackedScene;
                var newScene = newSceneRes.Instantiate<Node>();

                Network.Instance.GetTree().Root.AddChild(newScene);

                LoadTheWorld(newScene, Network.Instance.ScenesPathToId[newScene.SceneFilePath], false, false);
                _currentSceneLoadingOperation = "";
            }
            else if (ResourceLoader.LoadThreadedGetStatus(_currentSceneLoadingOperation) == ResourceLoader.ThreadLoadStatus.Failed)
            {
                NetickLogger.LogError(Engine, $"Loading scene failed [{_currentSceneLoadingOperation}]");
                _currentSceneLoadingOperation = "";
            }
        }
        else
        {
            if (_loadingScenes.Count > 0)
            {
                var newSceneOperation = _loadingScenes.Dequeue();
                OnSceneSwitchStart(newSceneOperation);
            }
        }
    }

    //// subscribe event listeners
    private void GetNetworkListeners(Node sceneRoot)
    {
        var listners = new List<NetworkEventsListener>(10);
        NetickGodotUtils.FindObjectsOfType(sceneRoot, listners);

        foreach (var listner in listners)
            Callbacks.Subscribe(listner);
    }
    public void FindObjectsOfType<T>(List<T> results) where T : Node
    {
        NetickGodotUtils.FindObjectsOfType(Level, results);
    }
    public T FindObjectOfType<T>() where T : Node
    {
        return NetickGodotUtils.FindObjectOfType<T>(Level);
    }
    void GetNetworkListners(Node obj, List<NetworkEventsListener> behaviourList)
    {
        foreach (var child in obj.GetChildren())
        {
            if (child as NetworkEventsListener != null)
                behaviourList.Add(child as NetworkEventsListener);  // (child as NetworkBehaviour).Object = obj;

            GetNetworkListners(child, behaviourList);
        }
    }

    internal Dictionary<int, NetworkObject> Entities;
    internal readonly Dictionary<int, NetworkObject> SceneObjects = new(512);
    internal readonly List<int> DestroyedSceneObjects = new(512);
    internal readonly Dictionary<uint, NetworkObject> PredictedSpawns = new(512);

    internal Dictionary<int, NetworkObjectPool> NetworkObjectPools = new(64);

    internal void InitNetworkSandboxEntities()
    {
        foreach (var pair in Network.Config.Prefabs)
        {
            var reference = pair.Value;

            if (reference != null)
            {
                var pool = new NetworkObjectPool(this, reference, 0, false);
                NetworkObjectPools.Add(pool.PrefabId, pool);
            }
        }
    }

    internal void Cleanup()
    {
        foreach (var pool in NetworkObjectPools.Values)
        {
            pool.DestroyEverything();
        }
    }


    /// <summary>
    /// Initializes the pool for the specified prefab. After this method has been called for a certain prefab, all instances of that prefab will be recycled and reset when created/destroyed.
    /// <para>Note: this method should only be called on <see cref="NetworkEventsListener.OnStartup(NetworkSandbox)"/>, in other words, just after Netick has been started. </para>
    /// </summary>
    /// <param name="prefabName">The name of the prefab to enable pooling for.</param>
    /// <param name="preloadedAmount">How many instances to be preloaded.</param>
    /// <param name="hideInactiveMembers">Pass true to hide inactive pool members.</param>

    public void InitializePool(StringName prefabName, int preloadedAmount)
    {
        int prefabId = NetworkObjectPool.GetPrefabIdFromName(prefabName);

        if (prefabId == -1)
        {
            GD.PrintErr($"Netick: couldn't load prefab with name [{prefabName}]");
            return;
        }

        NetworkObjectPools[prefabId].PreloadAmount = preloadedAmount;
        NetworkObjectPools[prefabId].UsePool = true;
        NetworkObjectPools[prefabId].HideUnspawned = true;// hideInactiveMembers;
        NetworkObjectPools[prefabId].Reload();
    }

    public void DestroyPool(StringName prefabName)
    {
        int prefabId = NetworkObjectPool.GetPrefabIdFromName(prefabName);

        if (prefabId == -1)
        {
            GD.PrintErr($"Netick: couldn't load prefab with name [{prefabName}]");
            return;
        }

        NetworkObjectPools[prefabId].Decommission();
    }

    internal void InitializeSceneObjectChildren(Node obj)
    {
        InitializeSceneObj(obj);

        foreach (Node child in obj.GetChildren())
            InitializeSceneObjectChildren(child);
    }

    private void InitializeSceneObj(Node obj)
    {
        if (obj == null)
            return;

        if (!obj.HasMeta(MetaConstants.NetworkedNode))
            return;

        // CHECK

        //if (entity.PrefabId > -1 /*|| entity.Actor.Scene != Scene*/)
        //return;

        if (Entities.Count > (Network.Config.MaxObjects + 1) || SceneObjects.Count > (Network.Config.MaxObjects + 1))
        {
            NetickLogger.LogError($"Netick stopped due to exceeding the maximum number of network objects at one time [{Network.Config.MaxObjects}]. Consider increasing it in Netick Settings (Netick -> Netick Settings).");
            Network.Shutdown();
            throw new Exception();
        }


        //GD.Print($"@@@@@@@@@@@@@@@@@@@ OBJ:    Name:{entity.GetPath()}       SceneId:{entity.SceneId}");

        var entity = new NetworkObject();
        entity.Node = obj;

        entity.InitInternals(this);
        Engine.CreateEntityLocal(entity);
        SceneObjects.Add(entity.GetSceneId(), entity);

        if (Engine.IsServer)
            Engine.ServerAddEntity(null, entity, default);
    }

    private bool TryFindSpawnPredictedObject(SpawnPredictionKey key, out NetworkObject obj)
    {
        if (PredictedSpawns.ContainsKey(key.RawValue))
            obj = PredictedSpawns[key.RawValue];
        else
            obj = Entities.SingleOrDefault(x => x.Value.SpawnPredictionKey.RawValue == key.RawValue).Value;

        return obj != null;
    }

    /// <summary>
    /// Instantiates a network prefab. 
    /// <para>Commonly, this should only be called on the server, since only the server can instantiate network prefabs. However, the client can also call this to spawn-predict a prefab by providing a spawn key which must be the same on the server and client when this method is called. Read the docs to learn more about how to use spawn-prediction.</para>
    /// <para>Note: make sure the prefab has been registered. </para> 
    /// <para>Note: the specified input source will be given to every <see cref="NetworkObject"/> child of this prefab.</para> 
    /// </summary>
    /// <param name="prefabName">The name of the prefab to be instantiated.</param>
    /// <param name="position">Position of the instantiated object.</param>
    /// <param name="rotation">Rotation of the instantiated object.</param>
    /// <param name="inputSource">Input source of the instantiated object.</param>
    /// <returns></returns>
    public NetworkObject NetworkInstantiate(StringName prefabName, Vector3? position = null, Quaternion? rotation = null, NetworkPlayer inputSource = null, SpawnPredictionKey predictedSpawnKey = default)
    {
        if (prefabName == null || prefabName == "")
            throw new Exception("Netick: prefabName cannot be empty.");

        if (Engine.IsClient && !predictedSpawnKey.IsValid)
            throw new NotServerException("Netick: only the server can instantiate network prefabs. Read the docs to learn about client spawn-prediction if that's what you are looking for.");

        Config.Prefabs.TryGetValue(prefabName, out var reference);

        if (reference == null)
            return null;

        if (!NetworkObjectPools.TryGetValue(reference.Id, out var pool))
            return null;

        // Redundant? This was in the original code.
        if (pool.PrefabId == -1 || !NetworkObjectPools.ContainsKey(pool.PrefabId))
            return null;

        if (Engine.IsResimulating && predictedSpawnKey.IsValid && TryFindSpawnPredictedObject(predictedSpawnKey, out var _newObj))
            return _newObj;

        var pos = (Vector3)(position == null ? Vector3.Zero : position);
        var rot = (Quaternion)(rotation == null ? Quaternion.Identity : rotation);

        var newObj = pool.Get(pos, rot);

        if (predictedSpawnKey.IsValid && Engine.IsClient)
            PredictedSpawns.Add(predictedSpawnKey.RawValue, newObj);

        LinkObject(newObj, inputSource, predictedSpawnKey);

        return newObj;
    }



    private void LinkObject(NetworkObject networkObject, NetworkPlayer inputSource, SpawnPredictionKey spawnKey)
    {
        var entity = networkObject;

        if (entity != null)
        {
            Engine.ServerAddEntity(inputSource, entity, spawnKey);
            foreach (var child in entity.InternalPrefabChildren)
            {
                LinkObject(child, inputSource, spawnKey);
            }
        }
    }

    internal NetworkObject InstantiatePrefabRootForClient(int instanceCounter, Tick spawnTick, Vector3 pos, Quaternion rot, int id, int prefabId, SpawnPredictionKey spawnKey, bool isThisInputSource)
    {
        NetworkObject entity = null;

        // check if we have a local predicted version of the entity
        if (spawnKey.IsValid && PredictedSpawns.TryGetValue(spawnKey.RawValue, out var obj))
        {
            entity = obj;
            PredictedSpawns.Remove(spawnKey.RawValue);//entity.transform.position = pos; //entity.transform.rotation = rot;
            Engine.ClientAddEntity(spawnTick, entity, id, isThisInputSource ? Engine.LocalPeer : null, true, false, spawnKey);
            entity.InstanceCounter = instanceCounter;
            entity.OnSpawnPredictionSucceeded();
        }
        else
        {
            entity = NetworkObjectPools[prefabId].Get(pos, rot);
            Engine.ClientAddEntity(spawnTick, entity, id, isThisInputSource ? Engine.LocalPeer : null, true, true, spawnKey);
            entity.InstanceCounter = instanceCounter;
            Callbacks.OnObjectCreated(entity);

            //if (Network.Instance.StartMode == StartMode.ServerAndClient)
            //  AddObject(entity.gameObject);
        }

        return entity;
    }

    internal NetworkObject LinkPrefabChildForClient(NetworkObject root, int childIndex, int childId, bool usable)
    {
        NetworkObject childEntity = root.InternalPrefabChildren.Find(x => x.PrefabIndex == childIndex);

        if (childEntity == null)
            return null;

        if (!Engine.Entities.ContainsKey(childId))
        {
            var rootSpawnKey = root.SpawnPredictionKey;

            if (rootSpawnKey.IsValid)
            {
                Engine.ClientAddEntity(root.SpawnTick, childEntity, childId, usable ? Engine.LocalPeer : null, true, false, rootSpawnKey);
            }
            else
            {
                Engine.ClientAddEntity(root.SpawnTick, childEntity, childId, usable ? Engine.LocalPeer : null, true, true, default);
                Callbacks.OnObjectCreated(childEntity);
            }
        }

        return childEntity;
    }

    /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// /// ///

    internal void DestroySceneObjects()
    {
        /// Causes GC!  
        foreach (var entity in Entities.Values.ToList())
        {
            Engine.DestroyEntity(entity, true, true);    //RecycleEntity(entity);
        }

        SceneObjects.Clear();
    }

    /// <summary>
    /// Destroys a network object. Only call this on the server or on the client for spawn-predicted objects which have -1 Ids.
    /// <para>Note: never call this on a child <see cref="NetworkObject"/> of the original prefab, only the root of the prefab.</para> 
    /// </summary>
    /// <param name="obj">The object to destroy.</param>
    /// <returns></returns>
    public void Destroy(NetworkObject obj)
    {
        if (Engine.IsClient && obj.Id != -1 && !obj.SpawnPredictionKey.IsValid)
            throw new NotServerException("Only the server can destroy network objects which have valid Ids. The client can only destroy spawn-predicted network objects which have invalid Ids (-1), meaning they haven't yet been confirmed by the server.");

        if (obj.IsPrefabChild() && obj.IsPrefabInstance)
        {
            GD.PrintErr("You can't destroy a prefab child. You must destroy the prefab root.");
            return;
        }

        InternalNetworkDestroy(obj.Node);
    }

    private void InternalNetworkDestroy(Node obj)
    {
        foreach (Node child in obj.GetChildren())
            InternalNetworkDestroy(child);

        var entity = obj.GetNetworkObject();

        if (entity != null)
        {
            foreach (NetworkObject child in entity.InternalPrefabChildren)
            {
                GetObjectResetInfo(child, out bool shouldReset, out bool isPrefabRoot);
                Engine.DestroyEntity(child, false, !isPrefabRoot);
            }

            if (entity.InternalPrefabRoot == null)
            {
                GetObjectResetInfo(entity, out bool shouldReset, out bool isPrefabRoot);
                Engine.DestroyEntity(entity, false, !isPrefabRoot);
            }
        }
    }

    ///////////// CLIENT -------------------------------------------------

    internal void NetworkDestroyForClient(NetworkObject entity)
    {
        GetObjectResetInfo(entity, out bool shouldReset, out bool isPrefabRoot);
        UnlinkChildren(entity.Node);

        foreach (NetworkObject child in entity.InternalPrefabChildren)
            Engine.DestroyEntity(child, true, !isPrefabRoot);

        Engine.DestroyEntity(entity, true, !isPrefabRoot);
    }

    private void UnlinkChildren(Node obj)
    {
        foreach (Node child in obj.GetChildren())
        {
            var childNet = child.GetNetworkObject();

            if (childNet != null)
            {
                if (childNet.InternalPrefabRoot == null)
                    childNet.InternalSetParent(null, true);
            }

            UnlinkChildren(child);
        }
    }
    private void GetObjectResetInfo(NetworkObject entity, out bool shouldReset, out bool isPrefabRoot)
    {
        bool usePool = entity.IsPrefabInstance && NetworkObjectPools[entity.PrefabId].UsePool;    // TODO: deal with scene switching
        shouldReset = (entity.IsPrefabInstance || entity.InternalPrefabRoot != null) && usePool;       //entity.InternalDestroy();   // TODO: add pool support  
        if (entity.PrefabId > -1 && entity.InternalPrefabRoot == null && usePool)
            isPrefabRoot = true;
        else
            isPrefabRoot = false;
    }

    private void RecycleEntity(NetworkObject entity)
    {
        Callbacks.OnObjectDestroyed(entity);   /// TODO - dont recycle prefab parts -- DONE

        if (entity.IsSceneObject)
            SceneObjects.Remove(entity.GetSceneId());

        if (IsClient)
            PredictedSpawns.Remove(entity.SpawnPredictionKey.RawValue);

        Entities.Remove(entity.Id);
        entity.InternalDestroy();
        GetObjectResetInfo(entity, out bool shouldReset, out bool isPrefabRoot);

        if (shouldReset)
            entity.Recycle();

        if (isPrefabRoot)
        {
            NetworkObjectPools[entity.PrefabId].Push(entity);
        }
        else if (entity.InternalPrefabRoot == null)
        {
            if (entity.Node.GetParent() != null)
                entity.Node.GetParent().RemoveChild(entity.Node);
            entity.Node.QueueFree();
        }
    }

}

