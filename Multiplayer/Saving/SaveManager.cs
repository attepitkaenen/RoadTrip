using System.Linq;
using Godot;
using Godot.Collections;

public partial class SaveManager : Node
{
    GameManager gameManager;
    MultiplayerController multiplayerController;
    MenuHandler menuHandler;
    SaveResource currentSave;
    Array<SaveResource> saves = new Array<SaveResource>();
    ResourceLoader.ThreadLoadStatus sceneLoadStatus = 0;


    [Export] Array<PackedScene> maps;
    [Export] Array<PackedScene> emptyMaps;

    public string activeMapScenePath = string.Empty;
    string sceneToLoad = string.Empty;
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
        multiplayerController = GetTree().Root.GetNode<MultiplayerController>("MultiplayerController");
        menuHandler = GetTree().Root.GetNode<MenuHandler>("MenuHandler");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (sceneToLoad == string.Empty || !PlayerManager.localPlayerState.IsLoading) return;

        sceneLoadStatus = ResourceLoader.LoadThreadedGetStatus(sceneToLoad, progress);

        if (sceneLoadStatus == ResourceLoader.ThreadLoadStatus.Loaded && !Multiplayer.IsServer())
        {
            GD.Print("Loading done client");
            multiplayerController.Rpc(nameof(multiplayerController.SetLoadingStatus), false);
            sceneToLoad = string.Empty;
        }

        if (sceneLoadStatus == ResourceLoader.ThreadLoadStatus.Loaded && Multiplayer.IsServer())
        {
            GD.Print("Loading done server");
            var newScene = ResourceLoader.LoadThreadedGet(sceneToLoad) as PackedScene;
            gameManager.InstantiateMap(sceneToLoad);
            multiplayerController.Rpc(nameof(multiplayerController.SetLoadingStatus), false);
            sceneToLoad = string.Empty;
        }
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
        var savePath = $"user://{currentSave.Id}.tres";
        SaveResource tempSave;

        if (!ResourceLoader.Exists(savePath))
        {
            MapSaveResource mapSaveResource = new MapSaveResource(gameManager.world.id, GenerateItemSaves(), GenerateVehicleSaves());
            Array<MapSaveResource> maps = new Array<MapSaveResource>() { mapSaveResource };
            tempSave = new SaveResource(currentSave.Id, currentSave.SaveName, gameManager.world.id, maps);
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
        string scenePath;
        var saveId = GetSaveId(saveName);
        var savePath = $"user://{saveId}.tres";
        if (ResourceLoader.Exists(savePath))
        {
            GD.Print("Pre-existing save found");
            currentSave = saves.ToList().Find(save => save.Id == saveId);
            scenePath = gameManager.mapList.ToList().Find(map => map.Id == currentSave.ActiveMap).EmptyMap.ResourcePath;
            SpawnEntities();
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
            currentSave = new SaveResource(newId, saveName, newId, new Array<MapSaveResource>());
            scenePath = gameManager.mapList.ToList().Find(map => map.Id == 0).Map.ResourcePath;
        }

        Rpc(nameof(InstantiateLoad), scenePath);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void InstantiateLoad(string scenePath)
    {
        activeMapScenePath = scenePath;
        sceneToLoad = scenePath;
        menuHandler.OpenMenu(MenuHandler.MenuType.loading);
        ResourceLoader.LoadThreadedRequest(scenePath);
    }
}
