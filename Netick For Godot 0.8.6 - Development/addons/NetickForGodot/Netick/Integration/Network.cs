// Copyright (c) 2023 Karrar Rahim. All rights reserved.

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Netick.GodotEngine
{
    /// <summary>
    /// Manages Netick and its sandboxes, and is used to start and shut down Netick.
    /// </summary>
    public enum StartMode
    {
        Server,
        Client,
        ServerAndClient
    }
    internal struct ShutdownRequest
    {
        public bool Shutdown;
        public bool DestroyAllNetworkObjects;

        public ShutdownRequest(bool shutdown, bool destroyAll)
        {
            this.Shutdown = shutdown;
            this.DestroyAllNetworkObjects = destroyAll;
        }
    }

    public sealed partial class Network : Node
    {
        public struct Sandboxs
        {
            public NetworkSandbox Server;
            public NetworkSandbox[] Clients;
        }

        internal Network() { }
        public static Network Instance => _instance;
        public StartMode StartMode { get; internal set; }
        public static bool IsRunning => Instance != null && Instance._started;
        public static bool IsHeadless => Instance != null && Instance._isHeadless;
        public static NetickConfig Config => Instance._config;
        public static string Version => "0.8.6";

        internal NetworkTransport Transport;
        internal ReflectionData ReflectionData;

        internal List<NetworkSandbox> Sandboxes = new List<NetworkSandbox>();

        internal Dictionary<short, string> ScenesIdToPath = new();
        internal Dictionary<string, short> ScenesPathToId = new();

        internal Server Server { get; private set; }
        internal static int FocusedIndex = 0;

        private NetickConfig _config;
        private static Network _instance;
        private ShutdownRequest _shutdownRequested = default;
        private bool _started = false;
        private bool _isHeadless = false;

        internal static bool CallNetworkRenderInHeadless => !IsHeadless || Config.CallRenderInHeadless;
        internal static bool IsP;

        /// <summary>
        /// Initializes Netick. This is automatically called when you start Netick. 
        /// <para>If you don't provide a <see cref="NetickConfig"/>, Netick will use the default config, which can be modified/found in (Netick -> Settings).</para>
        /// <para>If you don't provide a <see cref="NetworkTransport"/>, Netick will use the transport assigned in (Netick -> Settings -> Transport).</para>
        /// </summary>
        /// <param name="transport"></param>

        public static void Init(Node level, NetworkTransport transport = null, NetickConfig config = null)
        {
            if (Instance != null)
                return;

            var root = level.GetTree().Root;
            var netScene = new Network();
            netScene.Name = "Netick";
            root.AddChild(netScene);

            _instance = netScene;
            _instance._config = config;
            _instance.Transport = transport;


            _instance._isHeadless = DisplayServer.GetName() == "headless";

            if (config.Levels != null)
            {
                foreach (var pair in config.Levels)
                {
                    var path = pair.Value.Path;
                    var id = pair.Value.Id;

                    Instance.ScenesPathToId.Add(path, (short)id);
                    Instance.ScenesIdToPath.Add((short)id, path);
                }
            }

            var asmsList = new string[1] { "Godot Game Project" }.ToList();

            if (config.OtherScriptAssemblies != null)
                for (int i = 0; i < config.OtherScriptAssemblies.Length; i++)
                    asmsList.Add(config.OtherScriptAssemblies[i]);

            Instance.ReflectionData = new ReflectionData(asmsList.ToArray());
            IsP = Instance.ReflectionData.AssemblyIsLoaded("NetickPremium");
        }



        /// <summary>
        /// Shuts down Netick and destroys all sandboxes. The shutdown will occur in the next frame. For immediate shutdown, use: <see cref="ShutdownImmediately(bool)"/>
        /// </summary>

        public static void Shutdown(bool destroyAllNetworkObjects = false)
        {
            if (Instance != null)
                Instance._shutdownRequested = new ShutdownRequest(true, destroyAllNetworkObjects);
        }

        /// <summary>
        /// Shuts down Netick and destroys all sandboxes immediately.
        /// </summary>

        public static void ShutdownImmediately(bool destroyAllNetworkObjects = false)
        {
            if (Instance != null)
            {
                PerformShutdown(destroyAllNetworkObjects);
            }
        }

        /// <summary>
        /// Shuts down a specific sandbox.
        /// </summary>
        /// <param name="sandbox">The sandbox to shut down.</param>

        public static void ShutdownSandbox(NetworkSandbox sandbox, bool destroyAllNetworkObjects = false)
        {
            if (Instance.Sandboxes.Contains(sandbox))
            {
                Instance.Sandboxes.Remove(sandbox);
                sandbox.Shutdown(false, destroyAllNetworkObjects);
            }

        }

        /// <summary>
        /// Focus on a specific sandbox.
        /// </summary>
        /// <param name="sandbox">The sandbox to focus on.</param>

        public static void Focus(NetworkSandbox sandbox)
        {
            if (!Instance.Sandboxes.Contains(sandbox))
                return;


            for (int i = 0; i < Instance.Sandboxes.Count; i++)
            {
                var s = Instance.Sandboxes[i];

                if (s != sandbox)
                {
                    //s.IsVisiable = false;
                    s.Engine.InputEnabled = false;
                }
                else
                {
                    FocusedIndex = i;

                    //s.IsVisiable = true;
                    s.Engine.InputEnabled = true;
                }
            }

        }

        /// <summary>
        /// Starts both a client (or clients) and a server.
        /// </summary>
        /// <param name="serverPort">Network port.</param>
        /// <param name="numOfClients">Number of client sandboxs to create.</param>
        /// <returns></returns>
        //public static Sandboxs StartAsServerAndClient(Node caller, NetworkTransport transport, int serverPort, string prefab = null, int numOfClients = 1)
        //{
        //  if (Instance != null && Instance._started)
        //    return default;

        //  NetworkSandbox[] sandboxes = new NetworkSandbox[numOfClients];

        //  var server = CreateSandbox(caller, prefab, transport, serverPort, true, true, "");

        //  for (int i = 0; i < numOfClients; i++)
        //  {
        //    sandboxes[i] = CreateSandbox(caller, prefab, transport, serverPort, false, false, i.ToString());

        //    if (i == 0)
        //      Focus(sandboxes[i]);
        //  }

        //  var host = new Sandboxs()
        //  {
        //    Clients = sandboxes,
        //    Server = server
        //  };

        //  Instance._started  = true;
        //  Instance.StartMode = StartMode.ServerAndClient;

        //  return host;
        //}

        /// <summary>
        /// Starts Netick as a client.
        /// </summary>
        /// <returns>The sandbox representing the client</returns>

        public static NetworkSandbox StartAsClient(NetworkLevel level, NetickConfig config, NetworkTransport transport, string prefab = null)
        {
            if (Instance != null && Instance._started)
                return null;

            var client = CreateSandbox(level, config, prefab, transport, -1, false, true, "");
            Instance._started = true;
            Instance.StartMode = StartMode.Client;

            return client;
        }
        /// <summary>
        /// Starts Netick as a server.
        /// </summary>
        /// <param name="port">Network port.</param>
        /// <returns>The sandbox representing the server</returns>

        public static NetworkSandbox StartAsServer(NetworkLevel level, NetickConfig config, NetworkTransport transport, int port, string prefab = null)
        {
            if (Instance != null && Instance._started)
                return null;

            var server = CreateSandbox(level, config, prefab, transport, port, true);
            Instance._started = true;
            Instance.StartMode = StartMode.Server;

            return server;
        }

        private static void PerformShutdown(bool destroyAllNOs)
        {
            if (Instance != null)
            {
                for (int i = Instance.Sandboxes.Count - 1; i >= 0; i--)
                {
                    Instance.Sandboxes[i].Shutdown(false, destroyAllNOs);
                }

                // StaticUnitySceneManager.Reset();
                Instance.Sandboxes.Clear();
                //Destroy(Instance.Actor);

                Instance._started = false;

                // var scene = Instance.NetworkScene;
                //Instance._InternalDestroy();

                // Destroy(Instance.Actor);

                // _instance.Delete();
                Instance.Delete();
            }
        }

        //public override void OnDestroy()
        //{
        //  PerformShutdown(true);
        //  //Delete();
        //  //  Delete();
        //}
        public override void _ExitTree()
        {
            Delete();
        }

        private void Delete()
        {
            if (_instance != null)
            {
                _instance = null;
                GetParent().RemoveChild(this);
                QueueFree();
            }
        }

        private static NetworkSandbox CreateSandbox(Node level, NetickConfig config, string prefab, NetworkTransport transport, int port, bool isServer, bool useMainScene = true, string name = "")
        {
            // Only needs level to access SceneTree.Root
            if (Network.Instance == null)
                Init(level, transport, config);

            NetworkSandbox sandbox;

            if (level == null)
            {
                throw new Exception("Netick: you must pass a Level node before starting Netick.");
            }

            if (prefab == null)
            {
                sandbox = new NetworkSandbox();
                Network.Instance.AddChild(sandbox);
            }
            else
            {
                var prefabInstance = GD.Load<PackedScene>(prefab);
                sandbox = prefabInstance.Instantiate() as NetworkSandbox;
                Network.Instance.AddChild(sandbox);
            }

            sandbox.Init(level, transport, isServer, !useMainScene, name, port, Instance.ReflectionData);

            (sandbox as Node).Name = $"{sandbox.Name} Sandbox";
            Instance.Sandboxes.Add(sandbox);

            if (sandbox.Engine.Server != null)
                Instance.Server = sandbox.Engine.Server;

            return sandbox;
        }

        public override void _Process(double delta)
        {

            if (_shutdownRequested.Shutdown && Instance != null)
            {
                PerformShutdown(_shutdownRequested.DestroyAllNetworkObjects);
                return;
            }

            //if (Config.PhysicsType != PhysicsType.None)
            //{
            //}

            for (int i = Sandboxes.Count - 1; i >= 0; i--)
            {
                try
                {
                    if (i < Sandboxes.Count)
                    {
                        Sandboxes[i].NetworkUpdate(delta);
                        //Sandboxes[i].InternalLateUpdate();
                    }
                }

                catch (Exception exp)
                {
                    NetickLogger.LogError(exp);
                }
            }

            //  OnLateUpdate()

            if (_shutdownRequested.Shutdown && Instance != null)
            {
                PerformShutdown(_shutdownRequested.DestroyAllNetworkObjects);
                return;
            }

            for (int i = Sandboxes.Count - 1; i >= 0; i--)
            {
                try
                {
                    Sandboxes[i].InternalLateUpdate();
                }
                catch (Exception exp)
                {
                    NetickLogger.LogError(exp);
                }
            }



        }
    }
}
