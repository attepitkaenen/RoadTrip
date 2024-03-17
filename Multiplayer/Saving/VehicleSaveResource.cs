using Godot;
using Godot.Collections;
using System;

[GlobalClass]
public partial class VehicleSaveResource : Resource
{
    [Export] public int Id { get; set; }
    [Export] public Vector3 Position { get; set; }
    [Export] public Vector3 Rotation { get; set; }
    [Export] public Array<VehiclePartSaveResource> VehicleParts { get; set; }

    public VehicleSaveResource() { }

    public VehicleSaveResource(int vehicleId, Vector3 position, Vector3 rotation, Array<VehiclePartSaveResource> vehicleParts) 
    {
        Id = vehicleId;
        Position = position;
        Rotation = rotation;
        VehicleParts = vehicleParts;
    }
}