
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class MapSaveResource : Resource
{
    [Export] public int MapId { get; set; }
    [Export] public Array<ItemSaveResource> Items { get; set; }
    [Export] public Array<VehicleSaveResource> Vehicles { get; set; }

    // Make sure you provide a parameterless constructor.
    // In C#, a parameterless constructor is different from a
    // constructor with all default values.
    // Without a parameterless constructor, Godot will have problems
    // creating and editing your resource via the inspector.
    public MapSaveResource() { }
    public MapSaveResource(int id, Array<ItemSaveResource> items, Array<VehicleSaveResource> vehicles) 
    {
        MapId = id;
        Items = items;
        Vehicles = vehicles;
    }
}
