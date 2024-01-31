using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public partial class GameManager : Node
{
	[Signal] public delegate void PlayerJoinedEventHandler(long id);
	[Export] private PackedScene playerScene;
	[Export] public Array<ItemResource> itemList = new Array<ItemResource>();
	[Export] public MultiplayerSpawner spawner;
	public SceneManager world;
	private List<PlayerState> Players = new List<PlayerState>();
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
			GD.Print(itemList[0].ItemName);
		}
	}

	public void Respawn()
	{
		GD.Print($"Respawning {Multiplayer.GetUniqueId()}");
		var player = GetTree().GetNodesInGroup("Player").ToList().Find(player => player.Name == $"{Multiplayer.GetUniqueId()}") as Player;

		int playerIndex = Players.FindIndex(x => x.Id == int.Parse(player.Name));

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

	public void AddPlayerState(PlayerState playerState)
	{
		Players.Add(playerState);
		EmitSignal(SignalName.PlayerJoined, playerState.Id);
	}

	public void RemovePlayer(long id)
	{
		var players = GetTree().GetNodesInGroup("Player");
		if (players.Count > 0)
		{
			var player = Players.FirstOrDefault(i => i.Id == id);
			if (player is not null)
			{
				Players.Remove(player);
			}
			players.First(player => player.Name == $"{id}").QueueFree();
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void HoldItem(int itemId, string equipPath)
	{
		var item = GetItemResource(itemId).ItemInHand.Instantiate<HeldItem>();
		GetNode<Marker3D>(equipPath).AddChild(item);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void DropItem(int playerId, int itemId, Vector3 position)
	{
		var item = GetItemResource(itemId).ItemOnFloor.Instantiate<Item>();
		AddChild(item, true);
		item.GlobalPosition = position;
		var player = GetNode<Player>($"{playerId}");
		player.RpcId(playerId, nameof(player.SetPickedItem), item.GetPath());
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void DestroyItem(string itemPath)
	{
		// if (Multiplayer.IsServer())
		GetNode(itemPath).QueueFree();
	}


	public void ResetWorld()
	{
		Players = new List<PlayerState>();
		world = null;
		EmitSignal(SignalName.PlayerJoined, 0);
		var destroyList = GetChildren().Where(node => node is not MultiplayerSpawner).ToList();
		if (destroyList.Count > 0)
		{
			destroyList.ForEach(node => node.QueueFree());
		}
	}

	public List<PlayerState> GetPlayerStates()
	{
		return Players;
	}

	public void InitiateWorld()
	{
		if (Multiplayer.IsServer())
		{
			var scene = ResourceLoader.Load<PackedScene>("res://Scenes/World.tscn").Instantiate<SceneManager>();
			AddChild(scene);
			world = scene;
			foreach (var playerState in Players)
			{
				SpawnPlayer(playerState);
			}
		}
	}

	public void SpawnPlayer(PlayerState playerState)
	{
		var world = GetNodeOrNull("World");
		if (world is not null && Multiplayer.IsServer())
		{
			Player currentPlayer = playerScene.Instantiate<Player>();
			currentPlayer.Name = playerState.Id.ToString();

			int playerIndex = Players.FindIndex(x => x.Id == int.Parse(playerState.Id.ToString()));

			AddChild(currentPlayer);

			var spawnPoints = GetTree().GetNodesInGroup("SpawnPoints");
			foreach (Node3D spawnPoint in spawnPoints)
			{
				if (int.Parse(spawnPoint.Name) == playerIndex)
				{
					currentPlayer.Rpc(nameof(currentPlayer.MovePlayer), spawnPoint.GlobalPosition, Vector3.Zero);
				}
			}
		}
	}
}
