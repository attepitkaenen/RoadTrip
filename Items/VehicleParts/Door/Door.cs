using Godot;
using System;

public partial class Door : Item
{
    [Export] private float _condition = 100f;
    public int itemId;
    HingeJoint3D _hinge;
    Vehicle _vehicle;
    MultiplayerSynchronizer _multiplayerSynchronizer;
    bool _isClosed = true;
    IMount _mount;

    // Additional sync properties
    public Vector3 syncRotation;


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
        if (!IsMultiplayerAuthority()) return;

        syncPosition = Position;
        syncRotation = Rotation;
        syncBasis = Basis;
        syncLinearVelocity = LinearVelocity;
        syncAngularVelocity = AngularVelocity;

        float angle = RotationDegrees.Y;

        if (playerHolding == 0 && angle > -5 && angle < 5)
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

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        if (!IsMultiplayerAuthority())
        {
            Position = syncPosition;
            Rotation = syncRotation;
            state.LinearVelocity = syncLinearVelocity;
            state.AngularVelocity = syncAngularVelocity;
            return;
        }
    }

    public void SetMount(IMount mount)
    {
        _mount = mount;
    }

    public float GetCondition()
    {
        return _condition;
    }

    public void SetCondition(float condition)
    {
        _condition = condition;
    }

    public void Uninstall()
    {
        if (_mount is not null)
        {
            _mount.RpcId(1, nameof(_mount.RemoveInstalledPart), itemId, _condition, GlobalPosition);
            Rpc(nameof(DestroyPart));
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void DestroyPart()
    {
        _multiplayerSynchronizer.ReplicationConfig.RemoveProperty(GetPath() + ":syncPosition");
        _multiplayerSynchronizer.ReplicationConfig.RemoveProperty(GetPath() + ":syncBasis");
        _multiplayerSynchronizer.ReplicationConfig.RemoveProperty(GetPath() + ":syncRotation");
        _multiplayerSynchronizer.ReplicationConfig.RemoveProperty(GetPath() + ":syncLinearVelocity");
        _multiplayerSynchronizer.ReplicationConfig.RemoveProperty(GetPath() + ":syncAngularVelocity");
        QueueFree();
    }
}
