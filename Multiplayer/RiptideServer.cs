using Godot;
using Godot.Collections;
using Riptide;
using Riptide.Utils;

public enum ServerToClientId : ushort
{
	updatePlayerStates = 1,
	startLoad
}

public partial class RiptideServer : Node
{
	// PlayerStates
	public Dictionary<ushort, Player> playerInstances = new Dictionary<ushort, Player>();
	static Dictionary<ushort, PlayerState> playerStates = new Dictionary<ushort, PlayerState>();

	private ushort _port = 25565;
	private ushort _maxClientCount = 10;
	private static Server _server;

	GameManager gameManager;
	RiptideClient riptideClient;
	MenuHandler menuHandler;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		RiptideLogger.Initialize(GD.Print, GD.Print, GD.Print, GD.PrintErr, false);

		riptideClient = GetTree().Root.GetNode<RiptideClient>("RiptideClient");
		gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
		menuHandler = GetNode<MenuHandler>("/root/MenuHandler");

		_server = new Server();
		_server.ClientDisconnected += PlayerLeft;
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

	public bool IsServerRunning()
	{
		return _server.IsRunning;
	}

	public override void _PhysicsProcess(double delta)
	{
		_server.Update();
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

	[MessageHandler((ushort)ClientToServerId.sendName)]
	private static void NewPlayerNameMessageHandler(ushort fromClientId, Message message)
	{
		if (playerStates.TryGetValue(fromClientId, out _)) return;

		var newPlayer = new PlayerState { Id = fromClientId, Name = message.GetString(), IsLoading = true };
		playerStates.Add(fromClientId, newPlayer);

		Message newMessage = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.updatePlayerStates);
		newMessage.AddPlayerStates(playerStates);
		foreach (var playerState in playerStates)
		{
			GD.Print($"Sending Player id: {playerState.Key}, Player name: {playerState.Value.Name}, isLoading: {playerState.Value.IsLoading}");
		}
		_server.SendToAll(newMessage);
	}

	private void ResetSession()
	{
		gameManager.ResetGameState();
		playerStates = new Dictionary<ushort, PlayerState>();
		playerInstances = new Dictionary<ushort, Player>();
		MenuHandler.OpenMenu(MenuHandler.MenuType.mainmenu);
	}
}
