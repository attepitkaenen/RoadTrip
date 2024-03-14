using Godot;
using System;

[GlobalClass]
public partial class ItemSaveResource : Resource
{
    [Export] public int ItemId { get; set; }
    [Export] public Vector3 Position { get; set; }
    [Export] public Vector3 Rotation { get; set; }
    [Export] public float Condition { get; set; }

    public ItemSaveResource() { }

    public ItemSaveResource(int itemId, Vector3 position, Vector3 rotation, float condition) 
    {
        ItemId = itemId;
        Position = position;
        Rotation = rotation;
        Condition = condition;
    }
}