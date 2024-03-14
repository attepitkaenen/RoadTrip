using Godot;
using System;

[GlobalClass]
public partial class VehicleResource : Resource
{
    [Export] public int SaveId { get; set; }

    // Make sure you provide a parameterless constructor.
    // In C#, a parameterless constructor is different from a
    // constructor with all default values.
    // Without a parameterless constructor, Godot will have problems
    // creating and editing your resource via the inspector.
    public VehicleResource() { }
    public VehicleResource(int id) 
    {
        SaveId = id;
    }
}
