using System;
using System.Linq;
using Godot;
using Godot.Collections;

public partial class Moped : Vehicle
{
    [Export] private Seat _driverSeat;
    [Export] private Node3D doorsAndPanels;
    float maxSteer = 0.8f;
    private Vector2 _inputDir;
    bool braking;
    bool steerLean = false;
    bool grounded = false;

    // Inputs
    float steerInput = 0;
    float gasInput = 0;
    bool handBrake = false;

    // Wheel references
    VehicleWheel3D frontWheel;
    VehicleWheel3D backWheel;

    // Sync properties
    Vector3 syncPosition;
    Vector3 syncRotation;
    Basis syncBasis;
    Vector3 syncLinearVelocity;
    Vector3 syncAngularVelocity;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        backWheel = GetNode<VehicleWheel3D>("BackWheel");
        frontWheel = GetNode<VehicleWheel3D>("FrontWheel");

        enginePower = 50;
        breakForce = 10;
        
        if (!IsMultiplayerAuthority())
        {
            CustomIntegrator = true;
        };
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority()) return;

        HandleBalance();

        if (_driverSeat is null)
        {
            Brake = 1;
        }
        if (_driverSeat is not null && _driverSeat.GetSeatedPlayerId() < 1)
        {
            Brake = 1;
        }

        var steeringReducer = 1 / LinearVelocity.Length() * 10;
        steeringReducer = Mathf.Clamp(steeringReducer, 0.1f, 1);

        Steering = Mathf.Lerp(Steering, steerInput * steeringReducer * maxSteer, (float)delta * 1f);
        EngineForce = gasInput * enginePower;

        if (handBrake)
        {
            Brake = breakForce;
        }
        else
        {
            Brake = 0f;
        }

        Rpc(nameof(SyncProperties), GlobalPosition, GlobalRotation, GlobalBasis, LinearVelocity, AngularVelocity);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void SyncProperties(Vector3 syncPosition, Vector3 syncRotation, Basis syncBasis, Vector3 syncLinearVelocity, Vector3 syncAngularVelocity)
    {
        this.syncPosition = syncPosition;
        this.syncRotation = syncRotation;
        this.syncBasis = syncBasis;
        this.syncLinearVelocity = syncLinearVelocity;
        this.syncAngularVelocity = syncAngularVelocity;
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        if (!IsMultiplayerAuthority())
        {
            var newState = state.Transform;
            newState.Origin = GlobalPosition.Lerp(syncPosition, 0.9f);
            var a = newState.Basis.GetRotationQuaternion().Normalized();
            var b = syncBasis.GetRotationQuaternion().Normalized();
            var c = a.Slerp(b, 0.5f);
            newState.Basis = new Basis(c);
            state.Transform = newState;
            state.LinearVelocity = syncLinearVelocity;
            state.AngularVelocity = syncAngularVelocity;
            return;
        }

        // GD.Print($"Gas: {gasInput}, Steering: {steerInput}");


    }

    public void HandleBalance()
    {
        // ground check
        if (frontWheel.IsInContact() || backWheel.IsInContact())
        {
            grounded = true;
        }
        else
        {
            grounded = false;
        }

        // steer lean
        var speed = new Vector2(LinearVelocity.X, LinearVelocity.Z).Length();
        GD.Print(speed);
        if (speed >= 10)
        {
            steerLean = true;
        }
        else if (speed < 8)
        {
            steerLean = false;
        }


        if (grounded)
        {
            if (steerLean)
            {
                // GD.Print("Should lean");
                AngularVelocity = AngularVelocity.Lerp(-Transform.Basis.Z * steerInput, 0.1f);
                Steering = Mathf.Lerp(Steering, Rotation.Z, 0.1f);
            }
            if (Mathf.Abs(RotationDegrees.Z) >= 1)
            {
                // GD.Print("Should stand up right");
                AngularVelocity = AngularVelocity.Lerp(-Transform.Basis.Z * Mathf.Sign(RotationDegrees.Z), 0.1f);
            }
            else
            {
                // AngularVelocity = new Vector3(0, AngularVelocity.Y, 0);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public override void Drive(float inputDir, float gas, bool space, double delta)
    {
        steerInput = inputDir;
        gasInput = gas;
        handBrake = space;
    }
}
