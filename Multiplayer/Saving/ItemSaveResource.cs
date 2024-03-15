using Godot;
using System;

[GlobalClass]
public partial class ItemSaveResource : Resource
{
    [Export] public int id { get; set; }
    [Export] public Vector3 Position { get; set; }
    [Export] public Vector3 Rotation { get; set; }
    [Export] public float Condition { get; set; }

    public ItemSaveResource() { }

    public ItemSaveResource(int id, Vector3 position, Vector3 rotation, float condition)
    {
        id = id;
        Position = position;
        Rotation = rotation;
        Condition = condition;
    }
}