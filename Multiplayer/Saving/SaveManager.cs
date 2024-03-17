using System.Linq;
using Godot;
using Godot.Collections;

public partial class SaveManager : Node
{
    GameManager gameManager;
    SaveResource currentSave;

    string savePath = "user://save.tres";

    public override void _Ready()
    {
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
    }

    public void SaveGame()
    {
        SaveResource tempSave;
        if (!ResourceLoader.Exists(savePath))
        {
            MapSaveResource mapSaveResource = new MapSaveResource(gameManager.world.id, GenerateItemSaves(), GenerateVehicleSaves());
            Array<MapSaveResource> maps = new Array<MapSaveResource>() { mapSaveResource };
            tempSave = new SaveResource(0, gameManager.world.id, maps);
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

    public void LoadGame()
    {
        var load = ResourceLoader.Load<SaveResource>(savePath, "", ResourceLoader.CacheMode.Replace);
        
        foreach (ItemSaveResource item in load.Maps.Where(map => map.MapId == load.ActiveMap).First().Items)
        {
            gameManager.SpawnItem(0, item.Id, item.Condition, item.Position, item.Rotation);
        }   

        foreach (VehicleSaveResource vehicle in load.Maps.Where(map => map.MapId == load.ActiveMap).First().Vehicles)
        {
            gameManager.SpawnVehicle(vehicle.Id, vehicle.Position, vehicle.Rotation, vehicle.VehicleParts);
        }
    }
}
