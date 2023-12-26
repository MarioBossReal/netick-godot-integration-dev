using Godot;
using Netick.GodotEngine;
using Netick.Transport;
using System;
using System.Collections.Generic;
public class NetworkInfoLabel
{
    public Label Title;
    public Label Content;
    public Func<NetworkSandbox, string> InfoSource;
}

/// <summary>
/// This is an example of a script that starts Netick and also shows some useful monitoring information.
/// </summary>
[GlobalClass]
public partial class GameStarter : Control
{
    [Export]
    public string IPAddress = "127.0.0.1";
    [Export]
    public int Port = 4564;

    [Export]
    public NetworkLevel CurrentLevel;
    [Export]
    public NetickConfig NetickConfig;

    private NetworkSandbox _sandbox;
    private List<Control> _buttons = new();
    private List<NetworkInfoLabel> _networkInfoLabels = new();

    // ********************* How to start Netick ********************* \\

    /// <summary>
    /// This is how you start Netick as a client.
    /// </summary>
    private void StartAsClient()
    {
        var transprt = new LiteNetLibTransport();

        _sandbox = Network.StartAsClient(CurrentLevel, NetickConfig, transprt);

        _sandbox.Connect(Port, IPAddress);

        //var connectionRequestData = new byte[100];
        //_sandbox.Connect(Port, IPAddress, connectionRequestData, connectionRequestData.Length);
    }

    /// <summary>
    /// This is how you start Netick as a server.
    /// </summary>
    private void StartAsServer()
    {
        var transprt = new LiteNetLibTransport();
        _sandbox = Network.StartAsServer(CurrentLevel, NetickConfig, transprt, Port);
    }

    // ********************* ********************* ********************* \\

    public unsafe override void _Ready()
    {
        AddStartButtons();
        AddNetworkInfos();
    }

    public override void _Process(double dt)
    {
        bool isClientConnected = _sandbox != null && _sandbox.IsClientConnected;

        // toggle network info visibility based on isClientConnected
        ToggleNetworkInfoLabelsVisibleState(isClientConnected);

        // update network info when client is connected
        if (isClientConnected)
            UpdateNetworkInfo();
    }

    private void UpdateNetworkInfo()
    {
        foreach (var networkInfoLabel in _networkInfoLabels)
        {
            networkInfoLabel.Content.Text = networkInfoLabel.InfoSource(_sandbox);
        }
    }
    private void ToggleNetworkInfoLabelsVisibleState(bool state)
    {
        foreach (NetworkInfoLabel c in _networkInfoLabels)
        {
            c.Title.Visible = state;
            c.Content.Visible = state;
        }
    }

    private void AddNetworkInfos()
    {
        Vector2 offset = new Vector2(5, 5);
        float increase = 15;
        AddNetworkInfoLabel("In", (sand) => { return $"{sand.InKBps.ToString()} KB/s"; }, ref offset, increase);
        AddNetworkInfoLabel("Out", (sand) => { return $"{sand.OutKBps.ToString()} KB/s"; }, ref offset, increase);
        AddNetworkInfoLabel("RTT", (sand) => { return $"{sand.RTT * 1000f} ms"; }, ref offset, increase);
        AddNetworkInfoLabel("Interp Delay", (sand) => { return $"{(sand.InterpolationDelay * 1000f).ToString()} ms"; }, ref offset, increase);
        AddNetworkInfoLabel("Resims", (sand) => { return $"{sand.Resimulations.ToString()} Ticks"; }, ref offset, increase);
        AddNetworkInfoLabel("Delta time", (sand) => { return $"{sand.DeltaTime * 1000f} ms"; }, ref offset, increase);
    }

    private void AddNetworkInfoLabel(string name, Func<NetworkSandbox, string> contentSourceFunction, ref Vector2 offset, float increaseAmount)
    {
        var newOffset = new Vector2(offset.X, offset.Y + increaseAmount);

        var title = new Label();
        title.Text = name + ":";
        title.Size = new Vector2(title.Size.X + 100, title.Size.Y);
        title.Scale = title.Scale * 0.7f;

        var content = new Label();
        content.Text = name;
        content.Size = new Vector2(content.Size.X + 300, content.Size.Y);
        content.Scale = content.Scale * 0.7f;


        content.Position = new Vector2(offset.X + 300, offset.Y);
        title.Position = new Vector2(offset.X, offset.Y);
        offset = newOffset;

        AddChild(title);
        AddChild(content);

        var netLabel = new NetworkInfoLabel()
        {
            Title = title,
            Content = content,
            InfoSource = contentSourceFunction,
        };

        _networkInfoLabels.Add(netLabel);
    }

    private void RemoveButtons()
    {
        foreach (Control c in _buttons)
            RemoveChild(c);
    }

    private void AddStartButtons()
    {
        var offset = new Vector2(5, 5);
        var increase = 35;

        AddButton(() =>
        {
            StartAsServer();
            RemoveButtons();
        },

        "Start as Server", ref offset, increase);

        AddButton(() =>
        {
            StartAsClient();
            RemoveButtons();
        },

       "Start as Client", ref offset, increase);

    }

    private void AddButton(Action action, string name, ref Vector2 offset, float increaseAmount)
    {
        var button = new Button();
        button.Text = name;
        button.Pressed += action;
        button.Size = new Vector2(button.Size.X + 150, button.Size.Y);

        button.Position = new Vector2(offset.X, offset.Y);
        offset = new Vector2(offset.X, offset.Y + increaseAmount);
        AddChild(button);
        _buttons.Add(button);
    }
}