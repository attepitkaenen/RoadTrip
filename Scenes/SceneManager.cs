using Godot;
using System;
using System.Linq;

public partial class SceneManager : Node3D
{
	[Export] private PackedScene playerScene;
	GameManager gameManager;
	MultiplayerController multiplayerController;
	// Called when the node enters the scene tree for the first time.

	public override void _EnterTree()
	{
		gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
		multiplayerController = GetTree().Root.GetNode<MultiplayerController>("MultiplayerController");
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SpawnItem(int playerId, int itemId, Vector3 position)
	{
		if (!Multiplayer.IsServer()) return;
		Item item = gameManager.GetItemResource(itemId).ItemOnFloor.Instantiate<Item>();
		AddChild(item, true);
		item.GlobalPosition = position;
		item.playerHolding = playerId;
		var player = GetTree().GetNodesInGroup("Player").First(player => int.Parse(player.Name) == playerId) as Player;
		player.RpcId(playerId, nameof(player.SetPickedItem), item.GetPath());
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void DestroyItem(string itemName)
	{
		if (!Multiplayer.IsServer()) return;
		GetChildren().First(item => item.Name == itemName).QueueFree();
	}
}
