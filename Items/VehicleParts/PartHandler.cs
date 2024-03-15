using Godot;
using System;
using System.Collections;

public partial class PartHandler : Node3D
{
    [Signal] public delegate void PartInstalledEventHandler(int id, float condition, Vector3 partPosition);
    private RayCast3D _rayCast;
    private Marker3D _marker;
    private int _partId;
    private float _partCondition;
    // private dynamic part;
    [Export] Parts partToHandle;

    private enum Parts
    {
        engine,
        intake,
        carburetor,
        battery,
        radiator,
        alternator
    }

    public override void _Ready()
    {
        _rayCast = GetNode<RayCast3D>("RayCast3D");
        _marker = GetNode<Marker3D>("Marker3D");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_rayCast.IsColliding())
        {
            switch (partToHandle)
            {
                case Parts.engine:
                    {
                        // if (_rayCast.GetCollider() is EngineDropped engine)
                        // {
                        //     engine.InstallPart += InstallPart;
                        //     engine.canBeInstalled = true;
                        // }
                        break;
                    }
                // case Parts.intake:
                //     {
                //         if (_rayCast.GetCollider() is EngineDropped engine)
                //         {
                //             _partId = engine.id;
                //             _partCondition = engine.GetCondition();
                //         }
                //         break;
                //     }
                // case Parts.battery:
                //     {
                //         if (_rayCast.GetCollider() is EngineDropped engine)
                //         {
                //             _partId = engine.id;
                //             _partCondition = engine.GetCondition();
                //         }
                //         break;
                //     }
                // case Parts.carburetor:
                //     {
                //         if (_rayCast.GetCollider() is EngineDropped engine)
                //         {
                //             _partId = engine.id;
                //             _partCondition = engine.GetCondition();
                //         }
                //         break;
                //     }
                // case Parts.radiator:
                //     {
                //         if (_rayCast.GetCollider() is EngineDropped engine)
                //         {
                //             _partId = engine.id;
                //             _partCondition = engine.GetCondition();
                //         }
                //         break;
                //     }
                // case Parts.alternator:
                //     {
                //         if (_rayCast.GetCollider() is EngineDropped engine)
                //         {
                //             _partId = engine.id;
                //             _partCondition = engine.GetCondition();
                //         }
                //         break;
                //     }
                default:
                    break;
            }
        }

    }

    private void InstallPart(int id, float condition)
    {
        GD.Print("Trying to install engine");
        _partId = id;
        _partCondition = condition;
        EmitSignal(SignalName.PartInstalled, id, condition, _marker.Position);
    }
}
