using Godot;
using System;

public partial class Installable : Item
{
    [Signal] public delegate void InstallPartEventHandler(int id, float condition);
    public bool canBeInstalled = false;

    public void Install()
    {
        GD.Print("Starting " + Name + " install with Id: " + id + " and condition: " + condition);
        if (canBeInstalled)
        {
            EmitSignal(SignalName.InstallPart, id, condition);
            RpcId(1, nameof(QueueItemDestruction));
        }
    }

    public void SetCondition(float newCondition)
    {
        condition = newCondition;
    }
}
