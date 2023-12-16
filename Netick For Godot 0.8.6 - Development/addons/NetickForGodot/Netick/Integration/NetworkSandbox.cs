// Copyright (c) 2023 Karrar Rahim. All rights reserved.

using Godot;
using System;

namespace Netick.GodotEngine
{
    public static class NetickGodotExtensions
    {
        public static void SetAoIPosition(this NetworkPlayer conn, Vector3 position)
        {
            conn.Position = NetickGodotUtils.Vector3EngineToNetick(position);
        }
        public static T GetBehaviour<T>(this OnChangedData onChangedData) where T : NetworkBehaviour
        {
            return (T)onChangedData.Behaviour;
        }
    }

    public interface ResetOnSceneSwitch
    {
        public void OnSceneSwitchStart();
    }

    public partial class NetworkSandbox : Node
    {
        internal StartMode StartMode => Network.Instance.StartMode;

        /// <summary>
        /// Configuration data for Netick.
        /// </summary>
        public NetickConfig Config => Network.Config;

        /// <summary>
        /// Returns true if this <see cref="NetworkSandbox"/> is the server.
        /// </summary>

        public bool IsServer { get; private set; }

        /// <summary>
        /// Returns true if this <see cref="NetworkSandbox"/> is a client.
        /// </summary>

        public bool IsClient => !IsServer;

        /// <summary>
        /// Use this to associate an object with this sandbox.
        /// </summary>
        public object UserObject { get; set; }

        /// <summary>
        /// The level associated with this <see cref="NetworkSandbox"/>.
        /// </summary>
        public Node Level { get; private set; }

        /// <summary>
        /// <i><b>[Client Only]</b></i> Returns true if this client is currently connected to a server.
        /// </summary>
        public bool IsClientConnected => Engine.IsConnected;

        /// <summary>
        /// A list containing all connected clients currently.
        /// <para>Note: if you want the clients + the server, use <see cref="ConnectedPlayers"/>.</para>
        /// </summary>
        public NetickList<ServerConnection> ConnectedClients => Engine.ConnectedClients;

        /// <summary>
        /// <para>A list containing all connected clients currently, in addition to the server.</para>
        /// <para>Note: if you only want the clients, use <see cref="ConnectedClients"/>.</para>
        /// </summary>
        public NetickList<NetworkPlayer> ConnectedPlayers => Engine.ConnectedPlayers;

        /// <summary>
        /// A list containing all simulated/registered network objects [<see cref="NetworkObject"/>] currently.
        /// 
        /// <para>Note: to get a network object by id, use: <see cref="TryGetObject(int, out NetworkObject)"/> </para>
        /// </summary>
        public ObjectList Objects { get; private set; }


        /// <summary>
        /// This player.
        /// </summary>
        public NetworkPlayer LocalPlayer => Engine.LocalPeer;

        public LocalInterpolation LocalInterpolation => Engine.LocalInterpolation;
        public RemoteInterpolation RemoteInterpolation => Engine.RemoteInterpolation;
        public SimulationClock Timer => Engine.Timer;


        public T GetInput<T>() where T : unmanaged => Engine.GetInput<T>();
        public void SetInput<T>(T input) where T : unmanaged => Engine.SetInput<T>(input);


        /// <summary>
        /// Time period between network simulation steps./>
        /// </summary>

        public float FixedDeltaTime => Engine.Timer.FixedDelta;
        public float DeltaTime => Engine.Timer.DeltaTime;
        public float ScaledFixedDeltaTime => Engine.Timer.FixedDelta * 1;
        public float NetworkTime => LocalInterpolation.Time;

        /// <summary>
        /// Current simulation tick. 
        /// <para>On the server, <b>it's always going forward/increasing.</b></para>
        /// <para>On the client, <b>during resimulations it returns the current resimulated tick. </b> To check for resimulations, use <see cref="IsResimulating"/>.</para>
        /// </summary>

        public Tick Tick => Engine.Tick;

        /// <summary>
        /// Returns true if we are currently resimulating a previous input/tick of the past. On the server, it always returns false since <b>only the clients resimulate</b>.
        /// </summary>
        public bool IsResimulating => Engine.IsResimulating;
        public int Resimulations => Engine.ClientSimulation.Resimulatios;
        public int ResimulationStep => Engine.ClientSimulation.ResimulationStep;

        /// <summary>
        /// Incoming data in kilobytes per second (KBps).
        /// </summary>

        public float InKBps => Engine.InKBps;

        /// <summary>
        /// Outgoing data in kilobytes per second (KBps).
        /// </summary>

        public float OutKBps => Engine.OutKBps;

        /// <summary>
        /// <i><b>[Client Only]</b></i> Interpolation delay in seconds.
        /// </summary>

        public float InterpolationDelay => Engine.InterpolationDelay;

        /// <summary>
        /// <i><b>[Client Only]</b></i> The round-trip time (RTT) of the client in seconds.
        /// </summary>

        public double RTT => Engine.RTT;

        /// <summary>
        /// Always always returns null except when called inside the body of an RPC method, it returns the <see cref="NetworkConnection"/> we are executing RPCs from.
        /// </summary>
        /// <returns></returns>

        public NetworkConnection CurrentRpcSource => Engine.CurrentRpcSource;
        public NetworkPlayer CurrentRpcCaller => Engine.CurrentRpcCaller;

        public NetickEngine Engine { get; private set; }
        public new string Name { get; private set; }

        internal NetickCallbacks Callbacks;

        // Scene Stuff
        internal bool UseMainScene = true;
        // internal PhysicsType                  PhysicsType              = PhysicsType.Physics3D;
        // internal bool                         IsPhysicsStepedByNetick  => PhysicsType != PhysicsType.None;

        internal bool HasRemoteServerLoadScene = false;
        internal short SceneOperationsCounter = 0;
        internal bool SceneReloadFlag = false;   // This variable is used for when the client disconnects after the scene has been tampered with and need to be reloaded.
        public bool HasLoadLevel { get; set; }
        public short LevelIndex { get; private set; } = -1;
        private string SwitchedToScene = null;

        internal bool IsClientNotReady => (IsClient && !IsClientConnected) || (IsClient && !ServerHasLoadedScene);
        internal unsafe bool ServerHasLoadedScene => ((ConnectionMeta*)Engine.Client.ConnectedServer.LocalUserData)->HasLoadedScene;


        /// <summary>
        /// Gets the <see cref="NetworkObject"/> with the specified id. Returns null in case no object with that id exists.
        /// </summary>
        /// <param name="id"> The id of the <see cref="NetworkObject"/></param>
        /// <returns></returns>

        public NetworkObject GetObject(int id)
        {
            if (TryGetObject(out NetworkObject obj, id))
                return obj;
            else
                return null;

        }
        /// <summary>
        /// Trys to get the <see cref="NetworkObject"/> with the specified id.
        /// </summary>
        /// <param name="id"> The id of the <see cref="NetworkObject"/></param>
        /// <returns></returns>
        public bool TryGetObject(int id, out NetworkObject obj)
        {
            return TryGetObject(out obj, id);
        }

        /// <summary>
        /// Trys to get the <see cref="NetworkBehaviour"/> of a <see cref="NetworkObject"/> with the specified id.
        /// </summary>
        /// <param name="id"> The id of the network object</param>
        /// <returns></returns>

        public bool TryGetBehaviour<T>(int id, out T behaviour) where T : NetworkBehaviour
        {
            return TryGetBehaviour<T>(out behaviour, id);
        }

        private bool TryGetObject(out NetworkObject obj, int id)
        {
            obj = null;
            return Entities.TryGetValue(id, out obj); ;
        }

        private bool TryGetBehaviour<T>(out T behaviour, int id) where T : NetworkBehaviour
        {
            behaviour = null;

            if (Entities.TryGetValue(id, out NetworkObject obj))
                behaviour = obj.GetBehaviour<T>();

            return behaviour != null;
        }

        /// <summary>
        /// Converts <paramref name="tick"/> to time in seconds.
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>

        public float TickToTime(Tick tick)
        {
            return ((float)tick.TickValue) * FixedDeltaTime;
        }

        /// <summary>
        /// Converts <paramref name="tick"/> to time in seconds.
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>

        public float TickToTime(float tick)
        {
            return tick * FixedDeltaTime;
        }

        /// <summary>
        /// <i><b>[Server Only]</b></i> Switches to a different level.
        /// </summary>
        /// <param name="sceneBuildIndex">Path of the desired level scene.</param>
        public void SwitchLevel(string path)
        {
            if (Engine.IsClient)
                throw new NotServerException();

            SwitchedToScene = path;
        }

        internal unsafe void Init(Node scene, NetworkTransport transport, bool isServer, bool createNewScene, String name, int port, ReflectionData reflectionData)
        {
            UseMainScene = !createNewScene;
            IsServer = isServer;
            Name = isServer ? $"Server {name}" : $"Client {name}";
            Entities = new(Network.Config.MaxObjects);
            // NetPhysics                           = new NetworkPhysics(this);
            Callbacks = new NetickCallbacks(this);
            Objects = new ObjectList(this);
            Engine = new NetickEngine();

            //if (GetComponent<PhysicsSimulationStep>() == null && Config.PhysicsType != PhysicsType.None)
            //  gameObject.AddComponent<PhysicsSimulationStep>();

            Level = scene;

            var config = new NetickConfigData()
            {
                ServerDivisor = 1,

                ReplicationMode = ReplicationMode.Pessimistic,
                TickRate = Config.TickRate,
                MaxObjects = Config.MaxObjects,
                MaxPlayers = Config.MaxPlayers,
                EnableLogging = Config.EnableLogging,
                MaxPredicatedTicks = Config.MaxPredicatedTicks,
                MaxInterpolationBufferCount = (int)(Config.TickRate * 1.2f),
                SavedSnapshotsCount = (int)(Config.TickRate * 1.2f),
                MaxDataPerConnectionPerTickInBytes = Config.MaxSendableDataSize, //2500

                EnableLagCompensation = Config.EnableLagCompensation,
                EnableAoI = Config.AoI,

                AoIWorldSize = Config.WorldSize,
                AoICellSize = Config.CellSize,

                AllocatorBlockSize = Config.AllocatorBlockSize,
                TransportReceiveBufferSize = Config.ReceiveBufferSize,
                TransportSendBufferSize = Config.SendBufferSize,
                TransportTimeout = Config.Timeout
            };

            Engine.Start(Name, port, IsServer, this, config, transport, reflectionData, new GodotLogger(), new DefaultAllocator());
            InitNetworkSandboxEntities();

            if (Level.SceneFilePath == null || Level.SceneFilePath == "")
            {
                throw new Exception("Netick: level node is not a Scene file.");
            }

            //var l = Network.Instance.ScenesPathToId[Level.SceneFilePath];
            LoadTheWorld(Level, Network.Instance.ScenesPathToId[Level.SceneFilePath], true, !UseMainScene);
            Callbacks.OnStartup(this);
        }

        public void Connect(int port, string ip, byte[] connectionData = null, int connectionDataLength = 0)
        {
            Engine.Connect(port, ip, connectionData, connectionDataLength);
        }

        /// <summary>
        /// <i><b>[Server Only]</b></i> Disconnects a client from the server.
        /// </summary>
        /// <param name="client">The client to be disconnected.</param>
        public void Kick(ServerConnection client)
        {
            Engine.Kick(client);
        }

        /// <summary>
        /// <i><b>[Client Only]</b></i> Disconnects this client from the server.
        /// </summary>
        public void DisconnectFromServer()
        {
            Engine.DisconnectFromServer();
        }

        /// <summary>
        /// Shuts down this sandbox.
        /// </summary>
        internal void Shutdown(bool remove, bool destroyAllNetworkObjects)
        {
            OnShutdown();
            Cleanup();

            //if (!UseMainScene)
            //  SceneManager.UnloadSceneAsync(Scene);

            //else if (destroyAllNetworkObjects)
            //{
            //  var NOs = FindObjectsOfType<NetworkObject>(false);

            //  foreach (var no in NOs)
            //    if (no != null && no.gameObject != null)
            //      GameObject.Destroy(no.gameObject);
            //}

            //if (gameObject != null)
            //  GameObject.Destroy(gameObject);

            //_Destroy(true);
            Engine.Shutdown(); //LocalPeer.Stop();  //LocalPeer = null;   //IsRunning = false;

            if (remove)
                Network.Instance.Sandboxes.Remove(this);
        }

        internal void OnShutdown()
        {
            try
            {
                Callbacks.OnShutdown(this);
            }
            catch (Exception exp)
            {
                NetickLogger.LogError(Engine, exp);
            }
        }

        internal void InternalReset()
        {
            HasLoadLevel = true;
            LevelIndex = -1;
        }
        internal void InternalLateUpdate()
        {
            //if (InternalSandbox != null)
            //{
            //  NetPhysics.Interpolate(InternalSandbox.Timer.Alpha);
            //}
        }

        private void OnDestroy()
        {
            Engine.Shutdown();
        }

        private string _sceneSwitchOperation;

        internal void NetworkUpdate(double delta)
        {
            Engine.Simulate = !IsClientNotReady;
            Engine.Update((float)GetProcessDeltaTime(), (float)Godot.Engine.TimeScale);

            if (Network.CallNetworkRenderInHeadless)
                Engine.Render();

            if (SwitchedToScene != null)
            {
                StartSceneSwitch(SwitchedToScene);     //GD.Print("SwitchedToScene");
                LevelIndex = Network.Instance.ScenesPathToId[SwitchedToScene];  // SceneBuildIndex = SwitchedToScene;
                SceneOperationsCounter++;
                SwitchedToScene = null;
            }

            CheckSceneLoadingProgress();
        }
    }
}
