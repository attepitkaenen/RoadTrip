using Godot;
using System;

public partial class PartDropped : Item
{
    [Signal] public delegate void InstallPartEventHandler(int itemId, float condition);
    [Export] private float _condition;
    public bool isInstallable = false;

    public void Install()
    {
        GD.Print("Starting " + Name + " install");
        if (isInstallable)
        {
            EmitSignal(SignalName.InstallPart, ItemId, _condition);
            DestroyItem();
        }
    }

    public void SetCondition(float condition)
    {
        _condition = condition;
    }
}
