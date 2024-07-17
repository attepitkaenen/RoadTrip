using System;
using Godot;
using Godot.Collections;
using Riptide;
using Riptide.Utils;

public enum ServerToClientId : ushort
{
    sync = 1,
    updatePlayerStates,
    startLoad,
    playerMovement,
}

public partial class RiptideServer : Node
{
    // PlayerStates
    static Dictionary<ushort, Player> playerInstances = new Dictionary<ushort, Player>();
    static Dictionary<ushort, PlayerState> playerStates = new Dictionary<ushort, PlayerState>();


    private ushort _port = 25565;
    private ushort _maxClientCount = 10;
    private static Server _server;
    public ushort currentTick { get; private set; } = 0;

    static GameManager gameManager;
    RiptideClient riptideClient;
    MenuHandler menuHandler;
    SaveManager saveManager;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        RiptideLogger.Initialize(GD.Print, GD.Print, GD.Print, GD.PrintErr, false);

        riptideClient = GetTree().Root.GetNode<RiptideClient>("RiptideClient");
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
        menuHandler = GetNode<MenuHandler>("/root/MenuHandler");
        saveManager = GetNode<SaveManager>("/root/SaveManager");

        _server = new Server();
        _server.ClientDisconnected += PlayerLeft;
    }

    public override void _PhysicsProcess(double delta)
    {
        _server.Update();

        if (currentTick % 200 == 0)
        {
            SendSync();
        }

        currentTick++;
    }

    private void SendSync()
    {
        Message message = Message.Create(MessageSendMode.Unreliable, ServerToClientId.sync);
        message.Add(currentTick);

        _server.SendToAll(message);
    }

    public Dictionary<ushort, PlayerState> GetPlayerStates()
    {
        return playerStates;
    }

    public Dictionary<ushort, Player> GetPlayerInstances()
    {
        return playerInstances;
    }

    public void AddPlayerInstance(ushort id, Player player)
    {
        playerInstances.Add(id, player);
    }

    public static void SetPlayerLoadingStatus(ushort clientId, bool newStatus)
    {
        playerStates[clientId].IsLoading = newStatus;
        Message message = Message.Create(MessageSendMode.Reliable, ServerToClientId.updatePlayerStates);
        message.AddPlayerStates(playerStates);
        _server.SendToAll(message);
    }

    public void SetMaxPlayerCount(ushort maxClientCount)
    {
        _maxClientCount = maxClientCount;
    }

    public static bool IsServerRunning()
    {
        if (_server != null)
        {
            return _server.IsRunning;
        }
        else
        {
            return false;
        }
    }


    public void StartServer()
    {
        _server.Start(_port, _maxClientCount);
    }

    public void StopServer()
    {
        _server.Stop();
        ResetSession();
    }

    private void PlayerLeft(object sender, ServerDisconnectedEventArgs e)
    {
        playerStates.Remove(e.Client.Id);
        if (playerInstances.TryGetValue(e.Client.Id, out var player))
        {
            player.QueueFree();
        }
    }

    public void Host(ushort playerCount)
    {
        SetMaxPlayerCount(playerCount);
        StartServer();
    }

    public void SendMessageToAll(Message message)
    {
        _server.SendToAll(message);
    }

    [MessageHandler((ushort)ClientToServerId.movement)]
    private static void MovementMessageHandler(ushort fromClientId, Message message)
    {
		var movementStateId = message.GetUShort();
		var velocity = message.GetVector3();
        var position = message.GetVector3();
        var rotation = message.GetVector3();
        var headRotation = message.GetVector3();

		if (fromClientId == RiptideClient.GetId()) return;

        if (playerInstances.TryGetValue(fromClientId, out Player player))
        {
			player.stateHandler.SetMovementState(movementStateId);
            player.HandleMovementUpdate(velocity, position, rotation, headRotation);
        }
    }

    [MessageHandler((ushort)ClientToServerId.name)]
    private static void NewPlayerNameMessageHandler(ushort fromClientId, Message message)
    {
        if (playerStates.TryGetValue(fromClientId, out _)) return;

        var newPlayer = new PlayerState { Id = fromClientId, Name = message.GetString(), IsLoading = true };
        playerStates.Add(fromClientId, newPlayer);

        Message newMessage = Message.Create(MessageSendMode.Reliable, ServerToClientId.updatePlayerStates);

        newMessage.AddPlayerStates(playerStates);
        foreach (var playerState in playerStates)
        {
            GD.Print($"Sending Player id: {playerState.Key}, Player name: {playerState.Value.Name}, isLoading: {playerState.Value.IsLoading}");
        }
        _server.SendToAll(newMessage);

        if (GameManager.isGameStarted)
        {
            MakePlayerLoad(fromClientId);
        }
    }

    private static void MakePlayerLoad(ushort clientId)
    {
        var loadMessage = Message.Create(MessageSendMode.Reliable, ServerToClientId.startLoad);
        loadMessage.AddUShort((ushort)GameManager.activeMapId);
        _server.Send(loadMessage, clientId);
    }

    [MessageHandler((ushort)ClientToServerId.input)]
    private static void ServerHandleMovement(ushort fromClientId, Message message)
    {
        if (playerInstances.TryGetValue(fromClientId, out Player player))
        {
            var bools = message.GetBools(7);
            var rotation = message.GetVector3();
            var headRotation = message.GetVector3();

			// use this for vehicle driving
        }
    }

    private void ResetSession()
    {
        gameManager.ResetGameState();
        playerStates = new Dictionary<ushort, PlayerState>();
        playerInstances = new Dictionary<ushort, Player>();
        MenuHandler.OpenMenu(MenuHandler.MenuType.mainmenu);
    }
}
