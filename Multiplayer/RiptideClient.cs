using System;
using System.Linq;
using Godot;
using Godot.Collections;
using Riptide;
using Riptide.Utils;


public enum ClientToServerId : ushort
{
    name = 1,
    isLoadingStatus,
	movement,
    input
}

public partial class RiptideClient : Node
{
    public string UserName { get; set; }
    private static bool isHost = false;
    static Dictionary<ushort, PlayerState> playerStates = new Dictionary<ushort, PlayerState>();
    static Dictionary<ushort, Player> playerInstances = new Dictionary<ushort, Player>();

    private ushort _port = 25565;
    private string _ip = "127.0.0.1";
    private static Client _client;


    private static ushort _tickDivergenceTolerance = 1;
    private static ushort _serverTick;
    public static ushort ServerTick
    {
        get => _serverTick;
        private set
        {
            _serverTick = value;
            InterpolationTick = (ushort)(value - TicksBetweenPositionUpdates);
        }
    }

    public static ushort InterpolationTick { get; private set; }
    private static ushort _ticksBetweenPositionUpdates = 2;
    public static ushort TicksBetweenPositionUpdates
    {
        get => _ticksBetweenPositionUpdates;
        private set
        {
            _ticksBetweenPositionUpdates = value;
            InterpolationTick = (ushort)(ServerTick - value);
        }
    }

    MenuHandler menuHandler;
    GameManager gameManager;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        RiptideLogger.Initialize(GD.Print, GD.Print, GD.Print, GD.PrintErr, false);
        menuHandler = GetNode<MenuHandler>("/root/MenuHandler");
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");

        _client = new Client();
        _client.Connected += ConnectionSuccess;
        _client.ConnectionFailed += ConnectionFailed;
        _client.Disconnected += Disconnected;

        ServerTick = 2;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta)
    {
        _client.Update();

        ServerTick++;
    }


    [MessageHandler((ushort)ServerToClientId.sync)]
    public static void Sync(Message message)
    {
        SetTick(message.GetUShort());
    }

    public static void SetTick(ushort serverTick)
    {
        if (Mathf.Abs(ServerTick - serverTick) > _tickDivergenceTolerance)
        {
            GD.Print($"Client tick: {ServerTick} -> {serverTick}");
            ServerTick = serverTick;
        }
    }

    public Dictionary<ushort, PlayerState> GetPlayersToSpawn()
    {
        var playersToSpawn = new Dictionary<ushort, PlayerState>();
        foreach (var playerState in playerStates)
        {
            if (!playerInstances.Keys.Contains(playerState.Key))
            {
                // GD.Print($"PlayerInstances does not contain the player: {playerState.Key}");
                playersToSpawn.Add(playerState.Key, playerState.Value);
            }
        }
        return playersToSpawn;
    }

    public Dictionary<ushort, PlayerState> GetPlayerStates()
    {
        return playerStates;
    }

    public void AddPlayerInstance(ushort id, Player player)
    {
        playerInstances.Add(id, player);
    }

    public Dictionary<ushort, Player> GetPlayerInstances()
    {
        return playerInstances;
    }

    public void SendMessage(Message message)
    {
        _client.Send(message);
    }

    public static ushort GetId()
    {
        return _client.Id;
    }

    public static bool IsHost()
    {
        return isHost;
    }

    public bool IsLoading()
    {
        if (_client.IsConnected)
        {
            if (playerStates.ContainsKey(_client.Id))
            {
                return playerStates[_client.Id].IsLoading;
            }
            else return true;
        }
        else
        {
            return true;
        }
    }

    public bool IsServerLoading()
    {
        if (playerStates.TryGetValue(1, out var hostPlayerState))
        {
            return hostPlayerState.IsLoading;
        }
        else
        {
            return true;
        }
    }

    public bool GetIsConnected()
    {
        return _client.Connection.IsConnected;
    }

    public void Connect(string ip, ushort port, string userName, bool isHost = false)
    {
        RiptideClient.isHost = isHost;
        UserName = userName;
        _client.Connect($"{_ip}:{_port}");
    }

    public void Disconnect()
    {
        if (GetIsConnected())
        {
            _client.Disconnect();
        }
    }

    public void ConnectionSuccess(object sender, EventArgs e)
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToServerId.name);
        message.AddString(UserName);
        _client.Send(message);
    }

    public void Disconnected(object sender, EventArgs e)
    {
        GD.Print("Client disconnected");
        ResetSession();
    }

    public void ConnectionFailed(object sender, EventArgs e)
    {
        GD.Print("Connection failed.");
        ResetSession();
    }

    private void ResetSession()
    {
        playerStates = new Dictionary<ushort, PlayerState>();
        playerInstances = new Dictionary<ushort, Player>();
        UserName = "Jorma";

        if (!isHost)
        {
            gameManager.ResetGameState();
        }
    }


    [MessageHandler((ushort)ServerToClientId.updatePlayerStates)]
    private static void UpdatePlayerStatesMessageHandler(Message message)
    {
        var states = message.GetPlayerStates();

        foreach (var state in states)
        {
            GD.Print(state.Key + state.Value.Name);
        }

        playerStates = states;
    }

    [MessageHandler((ushort)ServerToClientId.playerMovement)]
    private static void PlayerMovementMessageHandler(Message message)
    {
        var id = message.GetUShort();
		var movementStateId = message.GetUShort();
        var velocity = message.GetVector3();
        var position = message.GetVector3();
        var rotation = message.GetVector3();
        var headRotation = message.GetVector3();

		if (id == _client.Id || RiptideServer.IsServerRunning()) return;

        if (playerInstances.TryGetValue(id, out Player player))
        {
			player.stateHandler.SetMovementState(movementStateId);
            player.HandleMovementUpdate(velocity, position, rotation, headRotation);
        }
    }
}
