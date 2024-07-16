using System.Linq;
using Godot;
using Godot.Collections;
using Riptide;

public partial class SaveManager : Node
{
	GameManager gameManager;
	static RiptideServer riptideServer;
	static RiptideClient riptideClient;
	MenuHandler menuHandler;
	public static SaveResource currentSave;
	Array<SaveResource> saves = new Array<SaveResource>();
	ResourceLoader.ThreadLoadStatus sceneLoadStatus = 0;


	[Export] Array<PackedScene> maps;
	[Export] Array<PackedScene> emptyMaps;

	static string sceneToLoad = string.Empty;
	public Array progress = new Array();

	public override void _EnterTree()
	{
		ProcessMode = ProcessModeEnum.Always;

		foreach (string fileName in DirAccess.GetFilesAt("user://"))
		{
			if (fileName.Contains(".tres"))
			{
				GD.Print("Save found: " + fileName);
				var item = GD.Load<SaveResource>($"user://{fileName}");
				saves.Add(item);
			}
		}
	}

	public override void _Ready()
	{
		gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
		riptideServer = GetTree().Root.GetNode<RiptideServer>("RiptideServer");
		riptideClient = GetTree().Root.GetNode<RiptideClient>("RiptideClient");
		menuHandler = GetTree().Root.GetNode<MenuHandler>("MenuHandler");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (sceneToLoad == string.Empty) return;

		sceneLoadStatus = ResourceLoader.LoadThreadedGetStatus(sceneToLoad, progress);

		if (sceneLoadStatus == ResourceLoader.ThreadLoadStatus.Loaded && riptideClient.IsLoading() && !riptideClient.IsHost())
		{
			GD.Print("Loading done client");

			Message message = Message.Create(MessageSendMode.Reliable, ClientToServerId.isLoadingStatus);
			message.AddBool(false);
			riptideClient.SendMessage(message);

			var newMap = ResourceLoader.LoadThreadedGet(sceneToLoad) as PackedScene;
			gameManager.InstantiateMap(newMap);
			sceneToLoad = string.Empty;
		}

		if (sceneLoadStatus == ResourceLoader.ThreadLoadStatus.Loaded && riptideClient.IsHost())
		{
			GD.Print("Loading done server");

			Message message = Message.Create(MessageSendMode.Reliable, ClientToServerId.isLoadingStatus);
			message.AddBool(false);
			riptideClient.SendMessage(message);

			var newMap = ResourceLoader.LoadThreadedGet(sceneToLoad) as PackedScene;
			gameManager.InstantiateMap(newMap);
			sceneToLoad = string.Empty;
		}
	}

	[MessageHandler((ushort)ClientToServerId.isLoadingStatus)]
	private static void SendIsLoadingStatusMessageHandler(ushort clientId, Message message)
	{
		RiptideServer.SetPlayerLoadingStatus(clientId, message.GetBool());
	}

	public string[] GetSaves()
	{
		return saves.Select(save => save.SaveName).ToArray();
	}

	public int GetSaveId(string saveName)
	{
		var tempSaves = saves.Where(save => save.SaveName == saveName);
		if (tempSaves.Count() > 0)
		{
			return tempSaves.First().Id;
		}
		return -1;
	}

	public void SaveGame()
	{
		if (!riptideClient.IsHost()) return;

		var savePath = $"user://{currentSave.Id}.tres";
		SaveResource tempSave;

		if (!ResourceLoader.Exists(savePath))
		{
			// Maps
			MapSaveResource mapSaveResource = new MapSaveResource(gameManager.world.id, GenerateItemSaves(), GenerateVehicleSaves());
			Array<MapSaveResource> maps = new Array<MapSaveResource>() { mapSaveResource };

			// Players
			Array<PlayerSaveResource> players = new Array<PlayerSaveResource>();
			foreach (var player in riptideServer.GetPlayerInstances())
			{
				PlayerSaveResource playerSaveResource = new PlayerSaveResource { Id = player.Key, Position = player.Value.Position, ItemInHandId = player.Value.playerInteraction.GetHeldItemId() };
				players.Add(playerSaveResource);
			}

			tempSave = new SaveResource(currentSave.Id, currentSave.SaveName, gameManager.world.id, maps, players);
		}
		else
		{
			var load = ResourceLoader.Load<SaveResource>(savePath, "", ResourceLoader.CacheMode.Replace);
			MapSaveResource mapSaveResource = new MapSaveResource(gameManager.world.id, GenerateItemSaves(), GenerateVehicleSaves());
			load.Maps[gameManager.world.id] = mapSaveResource;
			tempSave = load;
		}

		currentSave = tempSave;
		Error error = ResourceSaver.Save(tempSave, savePath);

		if (error != Error.Ok)
		{
			GD.Print("Save failed");
		}
		else
		{
			GD.Print("Game saved");
		}
	}

	public Array<ItemSaveResource> GenerateItemSaves()
	{
		Array<ItemSaveResource> itemSaveResources = new Array<ItemSaveResource>();

		var items = GetTree().GetNodesInGroup("Items");
		GD.Print(items.Count);
		foreach (Item item in items)
		{
			if (item.id == 0)
			{
				continue;
			}
			var itemSaveResource = new ItemSaveResource(item.id, item.GlobalPosition, item.GlobalRotation, item.condition);
			itemSaveResources.Add(itemSaveResource);
		}

		return itemSaveResources;
	}
	public Array<VehicleSaveResource> GenerateVehicleSaves()
	{
		Array<VehicleSaveResource> vehicleSaveResources = new Array<VehicleSaveResource>();

		var vehicles = GetTree().GetNodesInGroup("Vehicles");
		GD.Print(vehicles.Count);
		foreach (Vehicle vehicle in vehicles)
		{
			var vehicleSaveResource = new VehicleSaveResource(vehicle.id, vehicle.GlobalPosition, vehicle.GlobalRotation, GetVehicleParts(vehicle));
			vehicleSaveResources.Add(vehicleSaveResource);
		}

		return vehicleSaveResources;
	}

	public Array<VehiclePartSaveResource> GetVehicleParts(Vehicle vehicle)
	{
		var partMounts = vehicle.GetPartMounts();
		var parts = new Array<VehiclePartSaveResource>();
		foreach (PartMount partMount in partMounts)
		{
			if (partMount.GetPartId() != 0)
			{
				parts.Add(new VehiclePartSaveResource(partMount.GetPartId(), partMount.GetPartCondition(), partMount.Name));
			}
		}
		return parts;
	}

	public void SpawnEntities()
	{
		foreach (ItemSaveResource item in currentSave.Maps.Where(map => map.MapId == currentSave.ActiveMap).First().Items)
		{
			gameManager.SpawnItem(0, item.Id, item.Condition, item.Position, item.Rotation);
		}

		foreach (VehicleSaveResource vehicle in currentSave.Maps.Where(map => map.MapId == currentSave.ActiveMap).First().Vehicles)
		{
			gameManager.SpawnVehicle(vehicle.Id, vehicle.Position, vehicle.Rotation, vehicle.VehicleParts);
		}
	}

	public void LoadGame(string saveName)
	{
		string scenePath = "";
		var saveId = GetSaveId(saveName);
		var savePath = $"user://{saveId}.tres";

		Message message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.startLoad);

		if (ResourceLoader.Exists(savePath))
		{
			GD.Print("Pre-existing save found");
			currentSave = saves.ToList().Find(save => save.Id == saveId);
			scenePath = GameManager.mapList.ToList().Find(map => map.Id == currentSave.ActiveMap).EmptyMap.ResourcePath;
			SpawnEntities();

			message.AddUShort((ushort)currentSave.ActiveMap);
		}
		else
		{
			GD.Print("No save found, creating new one");
			var random = new RandomNumberGenerator();
			int newId = (int)random.Randi();
			while (saves.Select(save => save.Id).Contains(newId))
			{
				newId = (int)random.Randi();
			}
			currentSave = new SaveResource(newId, saveName, newId, new Array<MapSaveResource>(), new Array<PlayerSaveResource>());
			scenePath = GameManager.mapList.ToList().Find(map => map.Id == 0).Map.ResourcePath;

			message.AddUShort(0);
		}

		riptideServer.SendMessageToAll(message);
	}

	[MessageHandler((ushort)ServerToClientId.startLoad)]
	private static void StartLoadMessageHandler(Message message)
	{
		StartLoad(message.GetUShort());
	}

	public static void StartLoad(ushort mapId)
	{
		GD.Print("Start load message received");
		MenuHandler.OpenMenu(MenuHandler.MenuType.loading);
		sceneToLoad = GameManager.mapList.ToList().Find(map => map.Id == mapId).Map.ResourcePath;
		ResourceLoader.LoadThreadedRequest(sceneToLoad);
	}
}
