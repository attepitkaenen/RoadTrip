using Godot;
using System;

public partial class Installable : Item
{
    [Signal] public delegate void InstallPartEventHandler(int itemId, float condition);
    public bool canBeInstalled = false;

    public void Install()
    {
        GD.Print("Starting " + Name + " install with Id: " + itemId + " and condition: " + condition);
        if (canBeInstalled)
        {
            EmitSignal(SignalName.InstallPart, itemId, condition);
            RpcId(1, nameof(QueueItemDestruction));
        }
    }

    public void SetCondition(float newCondition)
    {
        condition = newCondition;
    }
}
