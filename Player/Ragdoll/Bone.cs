using System;
using Godot;

public partial class Bone : PhysicalBone3D
{
    public Player playerHolding;
    Player player;
    Vector3 syncPos;
    Vector3 syncRot;

    public override void _Ready()
    {
        player = GetParent().GetParent().GetParent().GetParent().GetParent<Player>();
        SetCollisionLayerValue(6, true);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (playerHolding != null)
        {
            Move(playerHolding.playerInteraction.syncHandPosition, playerHolding.playerInteraction.syncHandBasis);
        }
    }

    public void Move(Vector3 handPosition, Basis handBasis)
    {
        Vector3 moveForce = (handPosition - GlobalPosition) * 20;

        LinearVelocity = moveForce;

        Vector3 angularForce = GetAngularVelocity(Basis, handBasis) * 40;
        if (angularForce.Length() < 100)
        {
            AngularVelocity = angularForce;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void PickItem(int playerId)
    {
        GD.Print($"Item picked by: {playerId}");
        if (playerHolding == null)
        {
            if (PlayerManager.playerInstances.TryGetValue(playerId, out var player))
            {
                playerHolding = player;
                player.playerInteraction.RpcId(playerId, nameof(player.playerInteraction.SetPickedItem), GetPath());
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Throw(Vector3 handPosition, float strength)
    {
        LinearVelocity = handPosition * strength / 3;
        playerHolding = null;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetLinearVelocity(Vector3 linearVelocity)
    {
        LinearVelocity = linearVelocity;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Hit(int damage, Vector3 bulletTravelDirection)
    {
        GD.Print("Bone hit");
        player.Hit(damage, Name, bulletTravelDirection);
    }

    public void Impact(Vector3 bulletTravelDirection)
    {
        if (playerHolding == null)
        {
            LinearVelocity += bulletTravelDirection * 10;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Drop()
    {
		GD.Print("Drop bone");
        playerHolding = null;
    }

    public Vector3 GetAngularVelocity(Basis fromBasis, Basis toBasis)
    {
        Quaternion q1 = fromBasis.GetRotationQuaternion();
        Quaternion q2 = toBasis.GetRotationQuaternion();

        // Quaternion that transforms q1 into q2
        Quaternion qt = q2 * q1.Inverse();

        // Angle from quatertion
        float angle = 2 * Mathf.Acos(qt.W);

        // There are two distinct quaternions for any orientation
        // Ensure we use the representation with the smallest angle
        if (angle > Mathf.Pi)
        {
            qt = -qt;
            angle = Mathf.Tau - angle;
        }

        // Prevent divide by zero
        if (angle < 0.0001f)
        {
            return Vector3.Zero;
        }

        // Axis from quaternion
        Vector3 axis = new Vector3(qt.X, qt.Y, qt.Z) / Mathf.Sqrt(1 - qt.W * qt.W);

        return axis * angle;
    }
}