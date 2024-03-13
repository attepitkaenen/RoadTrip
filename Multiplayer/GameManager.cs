using Godot;
using Godot.Collections;
using System.IO;
using System.Linq;

public partial class GameManager : Node
{
	[Signal] public delegate void GameStartedEventHandler(long id);
	[Export] private PackedScene playerScene;
	[Export] public Array<ItemResource> itemList = new Array<ItemResource>();
	[Export] public MultiplayerSpawner multiplayerSpawner;
	public MenuHandler menuHandler;
	public SceneManager world;
	private MultiplayerController multiplayerController;
	public float Sensitivity = 0.001f;

	public override void _Ready()
	{
		// Load all itemResources
		foreach (string fileNameRemap in DirAccess.GetFilesAt("res://ItemData"))
		{
			var fileName = fileNameRemap.Replace(".remap", "");
			var item = GD.Load<ItemResource>("res://ItemData/" + fileName);
			itemList.Add(item);
		}
		multiplayerController = GetNode<MultiplayerController>("/root/MultiplayerController");
		menuHandler = GetNode<MenuHandler>("/root/MenuHandler");

		Callable spawnCallable = new Callable(this, MethodName.SpawnNode);
		multiplayerSpawner.SpawnFunction = spawnCallable;
	}


	public Dictionary<long, PlayerState> GetPlayerStates()
	{
		return multiplayerController.GetPlayerStates();
	}

	public int GetPlayerIndex(long id)
	{
		return GetPlayerStates().Keys.ToList().IndexOf(id);
	}

	public void Respawn()
	{
		GD.Print($"Respawning {Multiplayer.GetUniqueId()}");
		var player = GetTree().GetNodesInGroup("Player").ToList().Find(player => player.Name == $"{Multiplayer.GetUniqueId()}") as Player;
		int playerIndex = GetPlayerIndex(player.Id);
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

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SpawnPart(int itemId, float condition, Vector3 position, Vector3 rotation)
	{
		var item = multiplayerSpawner.Spawn(itemId) as Installable;
		item.SetCondition(condition);
		item.GlobalPosition = position;
		item.GlobalRotation = rotation;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SpawnItem(int playerId, int itemId, Vector3 position)
	{
		var item = multiplayerSpawner.Spawn(itemId) as Item;
		item.GlobalPosition = position;

		if (playerId == 0)
		{
			return;
		}
		var player = GetNode<Player>($"{playerId}");
		player.playerInteraction.RpcId(playerId, nameof(player.playerInteraction.SetPickedItem), item.GetPath());
	}

	Node SpawnNode(int itemId)
	{
		var node = GetItemResource(itemId).ItemOnFloor.Instantiate();
		return node;
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
		var scene = ResourceLoader.Load<PackedScene>("res://Scenes/Menus/LoadingScreen.tscn").Instantiate<LoadingScreen>();
		AddChild(scene, true);
		menuHandler.OpenMenu(MenuHandler.MenuType.none);
	}

	public void StartGame()
	{
		if (Multiplayer.IsServer())
		{
			foreach (var playerState in GetPlayerStates().Values)
			{
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

			GD.Print($"Spawning player {playerState.Name} with id: {playerState.Id}");
			AddChild(currentPlayer, true);

			int playerIndex = GetPlayerStates().Values.ToList().IndexOf(playerState);
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
