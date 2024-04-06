using Godot;
using System;
using System.Linq;

public partial class GrassFollowing : GpuParticles3D
{
    Player player;

    public override void _Ready()
    {
        var player = GetTree().GetNodesInGroup("Player").ToList().Find(player => player.Name == $"{Multiplayer.GetUniqueId()}") as Player;
    }

    public override void _PhysicsProcess(double delta)
    {
        GlobalPosition = player.GlobalPosition;
    }
}
