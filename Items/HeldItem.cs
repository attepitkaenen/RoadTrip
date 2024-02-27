using Godot;
using System;

public partial class HeldItem : Node3D
{
    [Export] public int holdType = 0;
    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority()) return;
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

    }

    public virtual void rightClick()
    {

    }
}
