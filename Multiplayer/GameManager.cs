using Godot;
using Godot.Collections;
using System;
using System.IO;
using System.Linq;

public partial class GameManager : Node
{
	[Signal] public delegate void GameStartedEventHandler(long id);
	[Export] private PackedScene playerScene;
	[Export] public Array<ItemResource> itemList = new Array<ItemResource>();
	[Export] public MultiplayerSpawner multiplayerSpawner;
	public SceneManager world;
	private MultiplayerController multiplayerController;
	public float Sensitivity = 0.001f;

	public override void _Ready()
	{
		// Load all itemResources
		foreach (string fileNameRemap in DirAccess.GetFilesAt("res://ItemData"))
		{
			GD.Print(fileNameRemap);
			var fileName = fileNameRemap.Replace(".remap", "");
			var item = GD.Load<ItemResource>("res://ItemData/" + fileName);
			itemList.Add(item);
			GD.Print(item.ItemId);
			GD.Print(itemList[0].ItemName);
		}
		multiplayerController = GetNode<MultiplayerController>("/root/MultiplayerController");

		Callable spawnCallable = new Callable(this, MethodName.SpawnItem);
		multiplayerSpawner.SpawnFunction = spawnCallable;
	}


	public Dictionary<long, PlayerState> GetPlayerStates()
	{
		return multiplayerController.GetPlayerStates();
	}

	public PlayerState GetPlayerState(long id)
	{
		return multiplayerController.GetPlayerStates()[id];
	}

	public void Respawn()
	{
		GD.Print($"Respawning {Multiplayer.GetUniqueId()}");
		var player = GetTree().GetNodesInGroup("Player").ToList().Find(player => player.Name == $"{Multiplayer.GetUniqueId()}") as Player;

		int playerIndex = GetPlayerStates().Values.ToList().IndexOf(player.playerState);

		var spawnPoints = GetTree().GetNodesInGroup("SpawnPoints");
		foreach (Node3D spawnPoint in spawnPoints)
		{
			if (int.Parse(spawnPoint.Name) == playerIndex)
			{
				player.Rpc(nameof(player.MovePlayer), spawnPoint.GlobalPosition, Vector3.Zero);
			}
		}
	}

	public ItemResource GetItemResource(int id)
	{
		if (itemList is not null)
		{
			return itemList.First(item => item.ItemId == id);
		}
		return null;
	}

	public void RemovePlayer(long id)
	{
		var players = GetTree().GetNodesInGroup("Player");
		if (players.Count > 0)
		{
			var player = players.FirstOrDefault(i => (i as Player).Id == id);
			if (player is not null)
			{
				player.QueueFree();
			}
		}
	}

	// [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	// public void HoldItem(long playerId, int itemId, string equipPath)
	// {
	// 	GD.Print($"{playerId} : {itemId} : {equipPath}");
	// 	Dictionary<int, bool> properties = new Dictionary<int, bool>();
	// 	properties[itemId] = true;
	// 	HeldItem item = multiplayerSpawner.Spawn(properties) as HeldItem;
	// 	item.Reparent(GetNode<Marker3D>(equipPath));
	// 	// item.SetMultiplayerAuthority((int)playerId);
	// 	var player = GetNode<Player>($"{playerId}");
	// 	player.Rpc(nameof(player.SetHeldItem), item.GetPath());
	// }

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void DropItem(int playerId, int itemId, Vector3 position)
	{
		Dictionary<int, bool> properties = new Dictionary<int, bool>();
		properties[itemId] = false;
		var item = multiplayerSpawner.Spawn(properties) as Item;
		item.GlobalPosition = position;
		var player = GetNode<Player>($"{playerId}");
		player.RpcId(playerId, nameof(player.SetPickedItem), item.GetPath());
	}

	Node SpawnItem(Dictionary<int, bool> properties)
	{
		Node item;
		if (properties.Values.First())
		{
			item = GetItemResource(properties.Keys.First()).ItemInHand.Instantiate();
		}
		else
		{
			item = GetItemResource(properties.Keys.First()).ItemOnFloor.Instantiate();
		}
		return item;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void DestroyItem(string itemPath)
	{
		// if (Multiplayer.IsServer())
		GetNode(itemPath).QueueFree();
	}


	public void ResetWorld()
	{
		world = null;
		var destroyList = GetChildren().Where(node => node is not MultiplayerSpawner).ToList();
		if (destroyList.Count > 0)
		{
			destroyList.ForEach(node => node.QueueFree());
		}
	}

	public void LoadGame()
	{
		if (Multiplayer.IsServer())
		{
			var scene = ResourceLoader.Load<PackedScene>("res://Scenes/World.tscn").Instantiate<SceneManager>();
			AddChild(scene);
			world = scene;
		}
	}

	public void StartGame()
	{
		if (Multiplayer.IsServer())
		{
			GD.Print(GetPlayerStates().Values.Count());
			foreach (var playerState in GetPlayerStates().Values)
			{
				GD.Print(playerState.Name + " " + playerState.Id);
				SpawnPlayer(playerState);
			}
		}
		EmitSignal(SignalName.GameStarted);
	}

	public void SpawnPlayer(PlayerState playerState)
	{
		foreach (var player in GetTree().GetNodesInGroup("Player"))
		{
			if (int.Parse((player as Player).Name) == playerState.Id)
			{
				GD.Print("Player: " + (player as Player).Name + " has already been spawned");
				return;
			}
		}
		var world = GetNodeOrNull("World");
		if (world is not null && Multiplayer.IsServer())
		{
			Player currentPlayer = playerScene.Instantiate<Player>();
			currentPlayer.Name = playerState.Id.ToString();

			int playerIndex = GetPlayerStates().Values.ToList().IndexOf(playerState);
			GD.Print($"Spawning player {playerState.Name} with id: {playerState.Id}");
			AddChild(currentPlayer);

			var spawnPoints = GetTree().GetNodesInGroup("SpawnPoints");
			foreach (Node3D spawnPoint in spawnPoints)
			{
				if (int.Parse(spawnPoint.Name) == playerIndex)
				{
					currentPlayer.Rpc(nameof(currentPlayer.MovePlayer), spawnPoint.GlobalPosition, Vector3.Zero);
				}
			}
			currentPlayer.Rpc(nameof(currentPlayer.SetPlayerState), playerState.Id, playerState.Name);
		}
	}
}
