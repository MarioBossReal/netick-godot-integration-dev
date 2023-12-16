using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Netick.Transport
{
  public class LiteNetLibTransport : NetworkTransport, INetEventListener
  {
    public class LNLConnection : TransportConnection
    {
      public LiteNetLibTransport        Transport;
      public NetPeer                    LNLPeer;
      public override IEndPoint         EndPoint => LNLPeer.EndPoint.ToNetickEndPoint();
      public override int               Mtu => LNLPeer.Mtu;

      public LNLConnection(LiteNetLibTransport transport)
      {
        Transport = transport;
      }

      public unsafe override void Send(IntPtr ptr, int length)
      {
        byte* p = (byte*)ptr.ToPointer();

        for (int i = 0; i < length; i++)
          Transport._bytes[i] = p[i];

        LNLPeer.Send(Transport._bytes, 0, length, DeliveryMethod.Unreliable);
      }
    }

    private NetManager                         _netManager;

    private BitBuffer                          _buffer;
    private readonly byte[]                    _bytes           = new byte[2048];
    private readonly byte[]                    _connectionBytes = new byte[200];

    private int                                _port;
    private bool                               _isServer        = false;
    private Dictionary<NetPeer, LNLConnection> _clients         = new Dictionary<NetPeer, LNLConnection>();
    private Queue<LNLConnection>               _freeClients     = new Queue<LNLConnection>();

    // LAN Matchmaking
    private List<Session>                      _sessions        = new List<Session>();
    private NetDataWriter                      _writer          = new NetDataWriter();
    private string                             _machineName;

    public override void Init()
    {
      _buffer      = new BitBuffer(createChunks: false);
      _netManager  = new NetManager((INetEventListener)this) { AutoRecycle = true };
      _machineName = Environment.MachineName;
      //_netManager.DisconnectTimeout = 1000;

      for (int i = 0; i < Engine.Config.MaxPlayers; i++)
        _freeClients.Enqueue(new LNLConnection(this));
    }

    public override void PollEvents()
    {
      _netManager.PollEvents();
    }

    public override void ForceUpdate()
    {
      _netManager.TriggerUpdate();
    }

    public override void Run(RunMode mode, int port)
    {
      if (mode == RunMode.Client)
      {
        _netManager.UnconnectedMessagesEnabled = true;
        _netManager.Start();
        _isServer = false;
      }

      else
      {
        _netManager.BroadcastReceiveEnabled = true;
        _netManager.Start(port);
        _isServer = true;
      }

      _port = port;
    }

    public override void Shutdown()
    {
      _netManager.Stop();
    }

    public override void Connect(string address, int port, byte[] connectionData, int connectionDataLen)
    {
      if (!_netManager.IsRunning)
        _netManager.Start();

      if (connectionData == null)
      {
        _netManager.Connect(address, port, "");
      }
      else
      {
        _writer.Reset();
        _writer.Put(connectionData, 0, connectionDataLen);
        _netManager.Connect(address, port, _writer);
      }


    }

    public override void Disconnect(TransportConnection connection)
    {
      _netManager.DisconnectPeer(((LNLConnection)connection).LNLPeer);
    }

    void INetEventListener.OnConnectionRequest(ConnectionRequest request)
    {
      int len = request.Data.AvailableBytes;
      request.Data.GetBytes(_connectionBytes, 0, len);
      bool accepted = NetworkPeer.OnConnectRequest(_connectionBytes, len, request.RemoteEndPoint.ToNetickEndPoint());

      if (accepted)
        request.Accept();
      else
        request.Reject();


      // request.Accept();
    }

    void INetEventListener.OnPeerConnected(NetPeer peer)
    {
      var connection = _freeClients.Dequeue();
      connection.LNLPeer = peer;

      _clients.Add(peer, connection);
      NetworkPeer.OnConnected(connection);
    }

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
      if (!_isServer)
      {
        if (disconnectInfo.Reason == DisconnectReason.ConnectionRejected || disconnectInfo.Reason == DisconnectReason.ConnectionFailed)
        {
          NetworkPeer.OnConnectFailed(ConnectionFailedReason.Refused);
          return;
        }

        if (peer == null)
        {
          NetickLogger.Log($"ERROR: {disconnectInfo.Reason}");
          NetworkPeer.OnConnectFailed(ConnectionFailedReason.Refused);
          return;
        }

      }

      if (peer == null)
      {
        return;
      }

      if (_clients.ContainsKey(peer))
      {
        TransportDisconnectReason reason = disconnectInfo.Reason == DisconnectReason.Timeout ? TransportDisconnectReason.Timeout : TransportDisconnectReason.Shutdown;

        NetworkPeer.OnDisconnected(_clients[peer], reason);
        _freeClients.Enqueue(_clients[peer]);
        _clients.Remove(peer);
      }
    }

     unsafe void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {
      if (_clients.TryGetValue(peer, out var c))
      {
        var len = reader.AvailableBytes;
        reader.GetBytes(_bytes, 0, reader.AvailableBytes);
     
        fixed(byte* ptr = _bytes)
        {
          _buffer.SetFrom(ptr, len, _bytes.Length);
          NetworkPeer.Receive(c, _buffer);
        }
      }
    }

    void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
      NetickLogger.Log("[S] NetworkError: " + socketError);
      NetworkPeer.OnConnectFailed(ConnectionFailedReason.Refused);
    }

    void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)  {  }
    void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
  }

}

