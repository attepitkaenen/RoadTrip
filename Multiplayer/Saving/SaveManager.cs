using System;
using System.ComponentModel;
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
        SaveResource tempSave = new SaveResource(1, GenerateItemSaves(), GenerateVehicleSaves());

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
            var itemSaveResource = new ItemSaveResource(item.id, item.GlobalPosition, item.GlobalRotation, item.condition);
            itemSaveResources.Add(itemSaveResource);
        }

        return itemSaveResources;
    }
    public Array<VehicleSaveResource> GenerateVehicleSaves()
    {
        Array<VehicleSaveResource> vehicleSaveResources = new Array<VehicleSaveResource>();

        var vehicles = GetTree().GetNodesInGroup("Vehicles");
        // GD.Print(items.Count);
        foreach (Vehicle vehicle in vehicles)
        {
            // var vehicleSaveResource = new VehicleSaveResource(vehicle.id, vehicle.GlobalPosition, vehicle.GlobalRotation, );
            // vehicleSaveResources.Add(vehicleSaveResource);
        }

        return vehicleSaveResources;
    }

    public void LoadGame()
    {
        var load = ResourceLoader.Load<SaveResource>(savePath, "", ResourceLoader.CacheMode.Replace);
        // GD.Print(load.Items[0].id);

    }
}
