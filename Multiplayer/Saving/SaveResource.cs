
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class SaveResource : Resource
{
    [Export] int SaveId { get; set; }
    [Export] Array<ItemSaveResource> Items { get; set; }
    [Export] Array<VehicleSaveResource> Vehicles { get; set; }

    // Make sure you provide a parameterless constructor.
    // In C#, a parameterless constructor is different from a
    // constructor with all default values.
    // Without a parameterless constructor, Godot will have problems
    // creating and editing your resource via the inspector.
    public SaveResource() { }
    public SaveResource(int id, Array<ItemSaveResource> items, Array<VehicleSaveResource> vehicles) 
    {
        SaveId = id;
        Items = items;
        Vehicles = vehicles;
    }
}
