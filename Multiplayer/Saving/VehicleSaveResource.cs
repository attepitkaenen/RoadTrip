using Godot;
using Godot.Collections;
using System;

[GlobalClass]
public partial class VehicleSaveResource : Resource
{
    [Export] public int VehicleId { get; set; }
    [Export] public Vector3 Position { get; set; }
    [Export] public Vector3 Rotation { get; set; }
    [Export] public Dictionary<string, int> VehicleParts { get; set; }

    public VehicleSaveResource() { }

    public VehicleSaveResource(int vehicleId, Vector3 position, Vector3 rotation, Dictionary<string, int> vehicleParts) 
    {
        VehicleId = vehicleId;
        Position = position;
        Rotation = rotation;
        VehicleParts = vehicleParts;
    }
}