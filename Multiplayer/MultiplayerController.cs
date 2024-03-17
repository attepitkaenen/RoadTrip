using Godot;
using Godot.Collections;
using System.ComponentModel.Design;
using System.Linq;


public partial class MultiplayerController : Control
{
	[Signal] public delegate void PlayerConnectedEventHandler(long peerId, PlayerState peerState);
	[Signal] public delegate void PlayerDisconnectedEventHandler(long peerId);

	[Export] private int port = 25565;
	private int maxConnections = 20;
	private string defaultServerIp = "127.0.0.1";

	PlayerState playerState = new PlayerState { Id = 1, Name = "Jorma", IsLoading = true };
	int playersLoaded = 0;
	public bool isGameStarted = false;

	Dictionary<long, PlayerState> players = new Dictionary<long, PlayerState>();

	private ENetMultiplayerPeer _peer;
	private ENetConnection _host;


	MenuHandler menuHandler;
	GameManager gameManager;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		menuHandler = GetNode<MenuHandler>("/root/MenuHandler");
		gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
		Multiplayer.PeerConnected += PeerConnected;
		Multiplayer.PeerDisconnected += PeerDisconnected;
		Multiplayer.ConnectedToServer += ConnectedToServer;
		Multiplayer.ConnectionFailed += ConnectionFailed;
		Multiplayer.ServerDisconnected += ServerDisconnected;

		// if(OS.GetCmdlineArgs().Contains("--server")){
		// 	hostGame();
		// }
	}

	public bool GetGameStartedStatus()
	{
		return isGameStarted;
	}

	public Dictionary<long, PlayerState> GetPlayerStates()
	{
		return players;
	}

	public void UpdateUserName(string newUsername)
	{
		playerState.Name = newUsername;
	}

	private void ServerDisconnected()
	{
		GD.Print("Server disconnected");
		Multiplayer.MultiplayerPeer = null;
		gameManager.ResetWorld();
		players.Clear();
		isGameStarted = false;
		menuHandler.OpenMenu(MenuHandler.MenuType.mainmenu);
		playerState = new PlayerState { Id = 1, Name = "Jorma", IsLoading = true };
	}

	private void ConnectionFailed()
	{
		Multiplayer.MultiplayerPeer = null;
	}

	private void ConnectedToServer()
	{
		var peerId = Multiplayer.GetUniqueId();
		playerState.Id = peerId;
		players[peerId] = playerState;
		EmitSignal(SignalName.PlayerConnected, peerId, playerState);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void PeerDisconnected(long id)
	{
		GD.Print("Player: " + id + " disconnected");
		players.Remove(id);
		gameManager.RemovePlayer(id);
		EmitSignal(SignalName.PlayerDisconnected, id);
	}

	private void PeerConnected(long id)
	{
		GD.Print($"Player {id} has connected");
		RpcId(id, nameof(RegisterPlayer), playerState.Id, playerState.Name, playerState.IsLoading);
	}

	public void Disconnect()
	{
		if (!Multiplayer.IsServer())
		{
			Rpc(nameof(PeerDisconnected), Multiplayer.GetUniqueId());
			Multiplayer.MultiplayerPeer.Close();
		}
		gameManager.ResetWorld();
		players.Clear();
	}

	public void CloseConnection()
	{
		isGameStarted = false;
		if (Multiplayer.IsServer())
		{
			_host.Destroy();
			Multiplayer.MultiplayerPeer = null;
		}
		else
		{
			Multiplayer.MultiplayerPeer.Close();
		}
		playerState = new PlayerState { Id = 1, Name = "Jorma", IsLoading = true };
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

		players[1] = playerState;
		EmitSignal(SignalName.PlayerConnected, 1, playerState);
	}

	public void RemoveMultiplayerPeer()
	{
		Multiplayer.MultiplayerPeer = null;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void LoadGame()
	{
		GD.Print("Loading game");
		gameManager.LoadGame();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void PlayerLoaded()
	{
		if (Multiplayer.IsServer())
		{
			playersLoaded += 1;
			if (playersLoaded == players.Count())
			{
				GD.Print("Starting game");
				gameManager.StartGame();
				playersLoaded = 0;
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void RegisterPlayer(long id, string name, bool isLoading)
	{
		GD.Print($"Registering player {Multiplayer.GetRemoteSenderId()}");
		var newPlayerState = new PlayerState { Name = name, Id = id, IsLoading = isLoading };
		var newPlayerId = Multiplayer.GetRemoteSenderId();

		if (Multiplayer.IsServer())
		{
			foreach (var player in players)
			{
				Rpc(nameof(RegisterPlayer), player.Key, player.Value.Name, player.Value.IsLoading);
			}
		}

		players[newPlayerId] = newPlayerState;
		EmitSignal(SignalName.PlayerConnected, newPlayerId, newPlayerState);
		gameManager.StartGame();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SetLoadingStatus(bool status)
	{
		// GD.Print($"On player {Multiplayer.GetUniqueId()} the status for loading player {Multiplayer.GetRemoteSenderId()} is set to {status}");
		players[Multiplayer.GetRemoteSenderId()].IsLoading = status;
	}
}
