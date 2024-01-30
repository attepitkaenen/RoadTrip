using Godot;
using System;

public partial class HeldItem : Node3D
{
    public override void _PhysicsProcess(double delta)
    {
        if (Input.IsActionJustPressed("leftClick"))
        {
            LeftClick();
        }
        if (Input.IsActionJustPressed("rightClick"))
        {
            rightClick();
        }
    }

    public virtual void LeftClick()
    {
        GD.Print("leftClick");
    }

    public virtual void rightClick()
    {
        GD.Print("rightClick");
    }
}
