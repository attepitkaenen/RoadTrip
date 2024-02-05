using Godot;
using System;

public partial class Blood : GpuParticles3D
{
    public override void _Ready()
    {
        Emitting = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Emitting == false)
        {
            QueueFree();
        }
    }
}
