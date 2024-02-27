using Godot;
using System;

public partial class Installable : Item
{
    [Signal] public delegate void InstallPartEventHandler(int itemId, float condition);
    [Export] private float _condition;
    public bool canBeInstalled = false;

    public void Install()
    {
        GD.Print("Starting " + Name + " install with Id: " + ItemId + " and condition: " + _condition);
        if (canBeInstalled)
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
