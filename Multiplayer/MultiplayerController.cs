using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;


public partial class MultiplayerController : Control
{

    [Signal] public delegate void PlayerConnectedEventHandler(long peerId, PlayerState state);
    [Signal] public delegate void PlayerDisconnectedEventHandler(long peerId);

    [Export] private int port = 25565;
    public int maxConnections = 20;
    private string defaultServerIp = "127.0.0.1";


    private ENetMultiplayerPeer _peer;
    private ENetConnection _host;


    MenuHandler menuHandler;
    GameManager gameManager;
    PlayerManager playerManager;
    SaveManager saveManager;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        menuHandler = GetNode<MenuHandler>("/root/MenuHandler");
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
        playerManager = GetTree().Root.GetNode<PlayerManager>("PlayerManager");
        saveManager = GetTree().Root.GetNode<SaveManager>("SaveManager");

        // Signals
        Multiplayer.PeerConnected += PeerConnected;
        Multiplayer.ConnectionFailed += ConnectionFailed;
        Multiplayer.ServerDisconnected += ServerDisconnected;
        Multiplayer.ConnectedToServer += ConnectedToServer;
        Multiplayer.PeerDisconnected += PeerDisconnected;
    }


    private void PeerDisconnected(long id)
    {
        GD.Print($"Peer disconnected {id}");
    }

    private void ConnectedToServer()
    {
        GD.Print($"Connected to server run on {Multiplayer.GetUniqueId()}, {PlayerManager.localPlayerState.Name}, {PlayerManager.localPlayerState.IsLoading}");
        Rpc(nameof(RegisterPlayer), Multiplayer.GetUniqueId(), PlayerManager.localPlayerState.Name, PlayerManager.localPlayerState.IsLoading);
    }

    private void ServerDisconnected()
    {
        GD.Print("Server disconnected");
        Multiplayer.MultiplayerPeer = null;
        gameManager.ResetWorld();
        PlayerManager.Reset();
        menuHandler.OpenMenu(MenuHandler.MenuType.mainmenu);
    }

    private void ConnectionFailed()
    {
        GD.Print("Connection failed");
        Multiplayer.MultiplayerPeer = null;
    }

    private void PeerConnected(long id)
    {
        // This runs on everyone else except the player of the received id
        GD.Print($"Player {id} has connected");
    }


    public void CloseConnection()
    {
        if (Multiplayer.IsServer())
        {
            ShutDownServer();
        }
        else
        {
            Disconnect();
        }
    }

    public void Disconnect()
    {
        gameManager.ResetWorld();
        PlayerManager.Reset();
        Multiplayer.MultiplayerPeer.Close();
        Multiplayer.MultiplayerPeer = null;
    }

    public void ShutDownServer()
    {
        gameManager.ResetWorld();
        PlayerManager.Reset();
        Multiplayer.MultiplayerPeer.Close();

        // This line is required for windows to not leave server running
        _host.Destroy();

        Multiplayer.MultiplayerPeer = null;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Kick(string reason)
    {
        GD.Print($"kicked: {reason}");
        Disconnect();
    }

    public void JoinGame(string address)
    {
        if (address is null)
        {
            address = defaultServerIp;
        }
        _peer = new ENetMultiplayerPeer();
        var error = _peer.CreateClient(address, port);
        if (error != Error.Ok)
        {
            GD.Print("error cannot join! :" + error.ToString());
            return;
        }
        _peer.Host.Compress(ENetConnection.CompressionMode.Zlib);
        Multiplayer.MultiplayerPeer = _peer;
        GD.Print("Joining Game!");
    }

    public void CreateGame()
    {
        GD.Print("Creating server");
        _peer = new ENetMultiplayerPeer();
        var error = _peer.CreateServer(port, maxConnections);
        if (error != Error.Ok)
        {
            GD.Print("error cannot host! :" + error.ToString());
            return;
        }
        _peer.Host.Compress(ENetConnection.CompressionMode.Zlib);
        Multiplayer.MultiplayerPeer = _peer;
        _host = _peer.Host;
        PlayerManager.localPlayerState.Id = 1;
        PlayerManager.AddPlayerState(PlayerManager.localPlayerState);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RegisterPlayer(int id, string name, bool isLoading)
    {
        GD.Print($"Received a RegisterPlayer request for id: {id}, name: {name}");

        var newPlayerState = new PlayerState { Name = name, Id = id, IsLoading = isLoading };

        PlayerManager.AddPlayerState(newPlayerState);

        EmitSignal(SignalName.PlayerConnected, id, newPlayerState);

        if (Multiplayer.IsServer())
        {
            foreach (var state in PlayerManager.playerStates)
            {
                Rpc(nameof(RegisterPlayer), state.Value.Id, state.Value.Name, state.Value.IsLoading);
            }
        }

        if (Multiplayer.IsServer() && GameManager.isGameStarted)
        {
            saveManager.RpcId(id, nameof(saveManager.InstantiateLoad), saveManager.activeMapScenePath);
            playerManager.SpawnPlayer(newPlayerState);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetLoadingStatus(bool status)
    {
        GD.Print($"On player {Multiplayer.GetUniqueId()} the status for loading player {Multiplayer.GetRemoteSenderId()} is set to {status}");

        PlayerManager.SetLoadingStatus(Multiplayer.GetRemoteSenderId(), status);

        if (!PlayerManager.localPlayerState.IsLoading && Multiplayer.IsServer() && !GameManager.isGameStarted)
        {
            GD.Print("Starting game");

        }
    }
}
