using Godot;
using System.Linq;


public partial class MultiplayerController : Control
{
	[Export]
	private int port = 8910;

	[Export]
	private string address = "127.0.0.1";

	private ENetMultiplayerPeer peer;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Multiplayer.PeerConnected += PeerConnected;
		Multiplayer.PeerDisconnected += PeerDisconnected;
		Multiplayer.ConnectedToServer += ConnectedToServer;
		Multiplayer.ConnectionFailed += ConnectionFailed;
		// if(OS.GetCmdlineArgs().Contains("--server")){
		// 	hostGame();
		// }
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
		RpcId(1, nameof(sendPlayerInformation), GetNode<LineEdit>("NameEdit").Text, Multiplayer.GetUniqueId());
    }

	/// <summary>
	/// Runs when a player disconnects and runs on all peers
	/// </summary>
	/// <param name="id">id of the player that disconnected</param>
    private void PeerDisconnected(long id)
    {
        GD.Print("Player Disconnected: " + id.ToString());
		GameManager.Players.Remove(GameManager.Players.Where(i => i.Id == id).First<PlayerInfo>());
		var players = GetTree().GetNodesInGroup("Player");
		
		foreach (var item in players)
		{
			if(item.Name == id.ToString()){
				item.QueueFree();
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

	private void hostGame(){
		peer = new ENetMultiplayerPeer();
		var error = peer.CreateServer(port, 3);
		if(error != Error.Ok){
			GD.Print("error cannot host! :" + error.ToString());
			return;
		}
		peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);

		Multiplayer.MultiplayerPeer = peer;
		GD.Print("Waiting For Players!");
	}

	public void _on_host_pressed(){
		hostGame();
		sendPlayerInformation(GetNode<LineEdit>("NameEdit").Text, 1);
	}
	
	public void _on_join_pressed(){
		peer = new ENetMultiplayerPeer();
		peer.CreateClient(address, port);

		peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
		Multiplayer.MultiplayerPeer = peer;
		GD.Print("Joining Game!");
	}
	public void _on_start_pressed(){
		if(peer is not null)
		{
			GD.Print("Already a host");
			Rpc(nameof(startGame));
		}
		else
		{
			GD.Print("Need to host");
			hostGame();
			sendPlayerInformation(GetNode<LineEdit>("NameEdit").Text, 1);
			Rpc(nameof(startGame));
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void startGame(){

		var scene = ResourceLoader.Load<PackedScene>("res://Scenes/World.tscn").Instantiate<Node3D>();
		GetTree().Root.AddChild(scene);
		this.Hide();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void sendPlayerInformation(string name, int id){
		PlayerInfo playerInfo = new PlayerInfo(){
			Name = name,
			Id = id
		};
		
		if(!GameManager.Players.Contains(playerInfo)){
			
			GameManager.Players.Add(playerInfo);
			
		}

		if(Multiplayer.IsServer()){
			foreach (var item in GameManager.Players)
			{
				Rpc(nameof(sendPlayerInformation), item.Name, item.Id);
			}
		}
	}
}
