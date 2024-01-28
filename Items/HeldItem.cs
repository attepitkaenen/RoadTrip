using Godot;
using System;

public partial class HeldItem : Node
{
    public void Drop()
    {
        QueueFree();
    }
}
