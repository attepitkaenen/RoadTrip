using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

public partial class GameManager : Node
{
	[Signal] public delegate void PlayerJoinedEventHandler(long id);
	[Export] private PackedScene playerScene;
	[Export] public ItemListResource itemList { get; set; }
	public SceneManager world;
	private List<PlayerState> Players = new List<PlayerState>();
	public float Sensitivity = 0.001f;

	public ItemResource GetItemResource(int id)
	{
		if (itemList is not null)
		{
			return itemList.items.First(item => item.ItemId == id);
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

	public void SpawnItem(int playerId, int itemId, Vector3 position)
	{
		if (world is null)
		{
			world = GetNode<SceneManager>("World");
		}
		world.Rpc(nameof(world.SpawnItem), playerId, itemId, position);
	}

	public void DestroyItem(string itemName)
	{
		if (world is null)
		{
			GD.Print("Yeah world was missing");
			world = GetNode<SceneManager>("World");
		}
		world.Rpc(nameof(world.DestroyItem), itemName);
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
