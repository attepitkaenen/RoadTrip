using Godot;
using System;

public partial class CarPart : Node3D
{
    [Export] private float _condition = 100f;

    public void Damage(float amount)
    {
        _condition -= amount;
    }

    public float GetCondition()
    {
        return _condition;
    }

    public void SetCondition(float condition)
    {
        _condition = condition;
    }
}
