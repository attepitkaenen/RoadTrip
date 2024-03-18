using Godot;
using Godot.Collections;
using System.Linq;

public partial class GameManager : Node
{
	[Signal] public delegate void GameStartedEventHandler(long id);
	[Export] private PackedScene playerScene;
	[Export] public Array<ItemResource> itemList = new Array<ItemResource>();
	[Export] public Array<VehicleResource> vehicleList = new Array<VehicleResource>();
	[Export] public Array<MapResource> mapList = new Array<MapResource>();
	[Export] public MultiplayerSpawner multiplayerSpawner;
	public int saveId;
	public MenuHandler menuHandler;
	public Map world;
	private MultiplayerController multiplayerController;
	public float Sensitivity = 0.001f;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		// Load all items, vehicles and maps
		foreach (string fileNameRemap in DirAccess.GetFilesAt("res://ItemData"))
		{
			var fileName = fileNameRemap.Replace(".remap", "");
			var item = GD.Load<ItemResource>("res://ItemData/" + fileName);
			itemList.Add(item);
		}

		foreach (string fileNameRemap in DirAccess.GetFilesAt("res://VehicleData"))
		{
			var fileName = fileNameRemap.Replace(".remap", "");
			var vehicle = GD.Load<VehicleResource>("res://VehicleData/" + fileName);
			vehicleList.Add(vehicle);
		}

		foreach (string fileNameRemap in DirAccess.GetFilesAt("res://MapData"))
		{
			var fileName = fileNameRemap.Replace(".remap", "");
			var map = GD.Load<MapResource>("res://MapData/" + fileName);
			mapList.Add(map);
		}


		multiplayerController = GetNode<MultiplayerController>("/root/MultiplayerController");
		menuHandler = GetNode<MenuHandler>("/root/MenuHandler");

		Callable spawnCallable = new Callable(this, MethodName.SpawnNode);
		multiplayerSpawner.SpawnFunction = spawnCallable;
	}

	public override void _Process(double delta)
	{
		if (GetChildren().Count < 2 && multiplayerController.isGameStarted)
		{
			multiplayerController.CloseConnection();
		}
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
			return itemList.FirstOrDefault(item => item.Id == id);
		}
		return null;
	}

	public VehicleResource GetVehicleResource(int id)
	{
		if (vehicleList is not null)
		{
			return vehicleList.FirstOrDefault(vehicle => vehicle.Id == id);
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
	public void SpawnItem(int playerId, int id, float condition, Vector3 position, Vector3 rotation)
	{
		var scene = GetItemResource(id).ItemOnFloor.ResourcePath;
		var item = multiplayerSpawner.Spawn(scene) as Item;

		item.GlobalPosition = position;
		item.GlobalRotation = rotation;
		item.condition = condition;

		if (playerId == 0)
		{
			return;
		}
		var player = GetNode<Player>($"{playerId}");
		player.playerInteraction.RpcId(playerId, nameof(player.playerInteraction.SetPickedItem), item.GetPath());
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SpawnVehicle(int id, Vector3 position, Vector3 rotation, Array<VehiclePartSaveResource> vehicleParts)
	{
		var scene = GetVehicleResource(id).VehicleBody.ResourcePath;
		var vehicle = multiplayerSpawner.Spawn(scene) as Vehicle;

		vehicle.GlobalPosition = position;
		vehicle.GlobalRotation = rotation;

		if (vehicleParts.Count < 1) return;

		var partMounts = vehicle.GetPartMounts();
		foreach (PartMount partMount in partMounts)
		{
			foreach (VehiclePartSaveResource partSave in vehicleParts)
			{
				if (partSave.PartMountName == partMount.Name)
				{
					partMount.partId = partSave.Id;
					partMount.partCondition = partSave.Condition;
				}
			}
		}
	}


	public void InstantiateMap(string mapPath)
	{
		var map = multiplayerSpawner.Spawn(mapPath) as Map;
		world = map;
		GD.Print("Setting world to " + world);
	}

	Node SpawnNode(string nodePath)
	{
		return (ResourceLoader.Load(nodePath) as PackedScene).Instantiate();
	}

	public void ResetWorld()
	{
		GD.Print("Resetting world");
		world = null;
		var destroyList = GetChildren().Where(node => node is not MultiplayerSpawner).ToList();
		if (destroyList.Count > 0)
		{
			destroyList.ForEach(node => node.QueueFree());
		}
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
		GD.Print("World is " + world);
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
