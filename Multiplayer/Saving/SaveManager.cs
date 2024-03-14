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
        SaveResource tempSave = new SaveResource(1);
        Array<ItemSaveResource> itemSaveResources = new Array<ItemSaveResource>();

        var items = GetTree().GetNodesInGroup("Items");
        GD.Print(items.Count);
        foreach (Item item in items)
        {
            var ItemSaveResource = new ItemSaveResource(item.itemId, item.GlobalPosition, item.GlobalRotation, item.condition);
            itemSaveResources.Add(ItemSaveResource);
        }
        tempSave.Items = itemSaveResources;

        currentSave = tempSave;
        GD.Print(tempSave.Items[0].Position);
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

    public void LoadGame()
    {
        var load = ResourceLoader.Load<SaveResource>(savePath, "", ResourceLoader.CacheMode.Replace);
        GD.Print(load.Items[0].ItemId);
        
    }
}
