using Godot;
using System.Collections.Generic;
using System.Linq;


public partial class MultiplayerController : Control
{
	[Export] private int port = 25565;
	private ENetMultiplayerPeer peer;
	private string userName;
	private bool isGameStarted = false;
	MenuHandler menuHandler;
	GameManager gameManager;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
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

	/// <summary>
	/// runs when this MultiplayerAPI's MultiplayerApi.MultiplayerPeer disconnects from server. Only emitted on clients.
	/// </summary>
	private void ServerDisconnected()
	{
		menuHandler.OpenMenu(MenuHandler.MenuType.mainmenu);
		ResetGameState();
		GD.Print("Server disconnected");
	}

	/// <summary>
	/// runs when the connection fails and it runs onlyh on the client
	/// </summary>
	private void ConnectionFailed()
	{
		GD.Print("CONNECTION FAILED");
	}

	/// <summary>
	/// runs when the connection is successful and only runs on the clients
	/// </summary>
	private void ConnectedToServer()
	{
		GD.Print("Connected To Server");
		RpcId(1, nameof(sendPlayerInformation), userName, Multiplayer.GetUniqueId());
	}

	/// <summary>
	/// Runs when a player disconnects and runs on all peers
	/// </summary>
	/// <param name="id">id of the player that disconnected</param>
	private void PeerDisconnected(long id)
	{
		GD.Print("Player Disconnected: " + id.ToString());
		gameManager.RemovePlayerState(id);
		var players = GetTree().GetNodesInGroup("Player");

		foreach (var player in players)
		{
			GD.Print(player.Name);
			if (player.Name == id.ToString())
			{
				player.QueueFree();
			}
		}
	}

	/// <summary>
	/// Runs when a player connects and runs on all peers
	/// </summary>
	/// <param name="id">id of the player that connected</param>
	private void PeerConnected(long id)
	{
		GD.Print("Player Connected! " + id.ToString());
	}

	private void hostGame()
	{
		peer = new ENetMultiplayerPeer();
		var error = peer.CreateServer(port, 3);
		if (error != Error.Ok)
		{
			GD.Print("error cannot host! :" + error.ToString());
			return;
		}
		peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);

		Multiplayer.MultiplayerPeer = peer;
		sendPlayerInformation(userName, 1);
		GD.Print("Waiting For Players!");
		// UpnpSetup();
	}

	public void ResetGameState()
	{
		isGameStarted = false;
		peer = null;
		GetTree().Root.GetNode<Node3D>("World").QueueFree();
		gameManager.ResetPlayerStates();
	}

	public void SetUserName(string name)
	{
		userName = name;
	}

	public bool GetGameStartedStatus()
	{
		return isGameStarted;
	}

	public void OnHostPressed()
	{
		hostGame();
	}

	public void OnJoinPressed(string address)
	{
		peer = new ENetMultiplayerPeer();
		peer.CreateClient(address, port);

		peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
		Multiplayer.MultiplayerPeer = peer;
		GD.Print("Joining Game!");
	}
	public void OnStartPressed()
	{
		if (peer is not null)
		{
			GD.Print("Already a host");
			Rpc(nameof(startGame));
		}
		else
		{
			GD.Print("Need to host");
			hostGame();
			Rpc(nameof(startGame));
		}
	}

	public void UpnpSetup()
	{
		// Fix later
		var upnp = new Upnp();

		var discoverResult = upnp.Discover();
		GD.Print($"Discovery code: {discoverResult}");
		if (discoverResult != 0)
		{
			GD.Print("upnp discovery failed!");
			if (discoverResult == 16)
			{
				GD.Print("Invalid gateway");
			}
			return;
		}

		var mapResult = upnp.AddPortMapping(port, 0, "", "UDP", 100);
		if (mapResult != 0)
		{
			GD.Print($"UPNP Port Mapping Failed: {mapResult}");
		}
		else
		{
			GD.Print($"Success! Join address: {upnp.QueryExternalAddress()}");
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void startGame()
	{
		var scene = ResourceLoader.Load<PackedScene>("res://Scenes/World.tscn").Instantiate<Node3D>();
		GetTree().Root.AddChild(scene);
		isGameStarted = true;
		menuHandler.OpenMenu(MenuHandler.MenuType.none);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void sendPlayerInformation(string name, int id)
	{
		PlayerState playerInfo = new PlayerState()
		{
			Name = name,
			Id = id
		};

		if (!gameManager.GetPlayerStates().Contains(playerInfo))
		{
			gameManager.AddPlayerState(playerInfo);
		}

		if (Multiplayer.IsServer())
		{
			foreach (var item in gameManager.GetPlayerStates())
			{
				Rpc(nameof(sendPlayerInformation), item.Name, item.Id);
			}
		}
	}
}
