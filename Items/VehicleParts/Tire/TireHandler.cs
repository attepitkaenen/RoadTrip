using Godot;
using System;

public partial class TireHandler : VehicleWheel3D
{
    [ExportGroup("Tire properties")]
    Tire _tire;
    PartMount _tireMount;
    [Export] private int _tireId;
    [Export] private float _tireCondition;


    public override void _EnterTree()
    {
        _tireMount = GetNode<PartMount>("TireMount");
        _tireMount.PartChanged += PartChanged;
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleTire();
    }

    public void PartChanged(int id, float condition, string partType)
    {
        _tireId = id;
        _tireCondition = condition;
    }

    public void HandleTire()
    {
        if (_tireId != 0 && _tire is null)
        {
            _tire = _tireMount.GetPart() as Tire;
        }
        else if (_tireId == 0)
        {
            _tire = null;
        }

        if (_tire is not null)
        {
            SuspensionTravel = 0.15f;
            WheelRadius = _tire.GetRadius();
            WheelFrictionSlip = _tire.GetFrictionSlip();
        }
        else
        {
            SuspensionTravel = 0f;
            WheelRadius = 0.1f;
        }
    }
}
