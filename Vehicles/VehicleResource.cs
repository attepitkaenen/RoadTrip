using Godot;
using System;

[GlobalClass]
public partial class VehicleResource : Resource
{
    [Export] public int Id { get; set; }
    [Export] public string VehicleName { get; set; }
    [Export] public string Description { get; set; }
    [Export] public PackedScene VehicleBody { get; set; }

    // Make sure you provide a parameterless constructor.
    // In C#, a parameterless constructor is different from a
    // constructor with all default values.
    // Without a parameterless constructor, Godot will have problems
    // creating and editing your resource via the inspector.
    public VehicleResource() { }
    public VehicleResource(int id) 
    {
        Id = id;
    }
}
