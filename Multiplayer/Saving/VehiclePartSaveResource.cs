using Godot;
using Godot.Collections;
using System;


[GlobalClass]
public partial class VehiclePartSaveResource : Resource
{
    [Export] public int Id { get; set; }
    [Export] public float Condition { get; set; }
    [Export] public string PartMountName { get; set; }


    public VehiclePartSaveResource() { }

    public VehiclePartSaveResource(int vehicleId, float condition, string partMountName) 
    {
        Id = vehicleId;
        Condition = condition;
        PartMountName = partMountName;
    }
}