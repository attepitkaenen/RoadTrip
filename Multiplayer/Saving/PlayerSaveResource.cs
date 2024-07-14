using Godot;
using System;

[GlobalClass]
public partial class PlayerSaveResource : Resource
{
    [Export] public int Id { get; set; }
    [Export] public Vector3 Position { get; set; }
    [Export] public int ItemInHandId { get; set; }

    public PlayerSaveResource() { }

    public PlayerSaveResource(int id, Vector3 position, int itemInHandId)
    {
        Id = id;
        Position = position;
        ItemInHandId = itemInHandId;
    }
}
