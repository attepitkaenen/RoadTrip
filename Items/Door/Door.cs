using Godot;
using System;

public partial class Door : Item
{
    HingeJoint3D _hinge;
    Vehicle _vehicle;
    MultiplayerSynchronizer _multiplayerSynchronizer;
    bool _isClosed = true;

    // 	public Vector3 syncPosition;
    public Vector3 syncRotation;

    // public Basis syncBasis;
    // public Vector3 syncLinearVelocity;
    // public Vector3 syncAngularVelocity;

    public override void _Ready()
    {
        _hinge = GetParent().GetNode<HingeJoint3D>("HingeJoint3D");
        _vehicle = GetParent().GetParent().GetParent<Vehicle>();
        _multiplayerSynchronizer = _vehicle.GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer");

        _multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":syncPosition");
        _multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":syncBasis");
        _multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":syncRotation");
        _multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":syncLinearVelocity");
        _multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":syncAngularVelocity");
    }

    public override void _PhysicsProcess(double delta)
    {
        // base._PhysicsProcess(delta);
        if (!IsMultiplayerAuthority()) return;

        syncPosition = Position;
        syncRotation = Rotation;
        syncBasis = Basis;
        syncLinearVelocity = LinearVelocity;
        syncAngularVelocity = AngularVelocity;

        float angle = RotationDegrees.Y;

        if (playerHolding == 0 && angle > -5)
        {
            _isClosed = true;
        }
        else
        {
            _isClosed = false;
        }

        if (_isClosed)
        {
            Position = Vector3.Zero;
            Rotation = Vector3.Zero;
            _hinge.NodeB = null;
            _hinge.NodeA = null;
            Freeze = true;
        }
        else
        {
            Freeze = false;
            _hinge.NodeB = GetPath();
            _hinge.NodeA = _vehicle.GetPath();
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public override void Move(Vector3 handPosition, Basis handBasis, int playerId)
    {
        if (playerHolding == 0)
        {
            playerHolding = playerId;
        }
        else if (playerHolding == playerId)
        {
            Vector3 moveForce = (handPosition - GlobalPosition) * 20;

            LinearVelocity = moveForce;
        }
    }

    // public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    // {
    //     if (IsMultiplayerAuthority()) return;
    //     GD.Print(syncRotation);
    //     Position = syncPosition;
    //     Rotation = syncRotation;
    // }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        if (!IsMultiplayerAuthority())
        {
            // var newState = state.Transform;
            // newState.Origin = GlobalPosition.Lerp(syncPosition, 0.9f);
            // var a = newState.Basis.GetRotationQuaternion().Normalized();
            // var b = syncBasis.GetRotationQuaternion().Normalized();
            // var c = a.Slerp(b, 0.5f);
            // newState.Basis = new Basis(c);
            // state.Transform = newState;
            Position = syncPosition;
            Rotation = syncRotation;
            state.LinearVelocity = syncLinearVelocity;
            state.AngularVelocity = syncAngularVelocity;
            return;
        }
    }
}
