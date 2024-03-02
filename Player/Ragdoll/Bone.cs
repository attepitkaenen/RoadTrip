using System;
using Godot;

public partial class Bone : PhysicalBone3D
{
	public int playerHolding = 0;
	MultiplayerSynchronizer multiplayerSynchronizer;
	Vector3 syncPos;
	Vector3 syncRot;

    // public override void _Ready()
    // {
    //     var ragdoll = GetParent().GetParent().GetParent().GetParent<Ragdoll>();
	// 	multiplayerSynchronizer = ragdoll.multiplayerSynchronizer;
    // }

	// public void ActivateSync()
	// {
	// 	multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":syncPos");
    //     multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":syncRot");
	// }

	// public void DeactivateSync()
	// {
    //     multiplayerSynchronizer.ReplicationConfig.RemoveProperty(GetPath() + ":syncPos");
    //     multiplayerSynchronizer.ReplicationConfig.RemoveProperty(GetPath() + ":syncRot");
	// }

    // public override void _PhysicsProcess(double delta)
    // {
    //     if (!IsMultiplayerAuthority())
	// 	{
	// 		Position = Position.Lerp(syncPos, 0.1f);
	// 		Rotation = Position.Lerp(syncRot, 0.1f);;
	// 		return;
	// 	}
	// 	syncPos = Position;
	// 	syncRot = Rotation;
    // }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void Move(Vector3 handPosition, Basis handBasis, int playerId)
	{
		if (playerHolding == 0)
		{
			playerHolding = playerId;
		}
		else if (playerHolding == playerId)
		{
			Vector3 moveForce = (handPosition - GlobalPosition) * 20;

			LinearVelocity = moveForce;

			Vector3 angularForce = GetAngularVelocity(Basis, handBasis) * 40;
			if (angularForce.Length() < 100)
			{
				AngularVelocity = angularForce;
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void Throw(Vector3 handPosition, float strength)
	{
		LinearVelocity = handPosition * strength / 3;
		playerHolding = 0;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SetLinearVelocity(Vector3 linearVelocity)
	{
		LinearVelocity = linearVelocity;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void Hit(int damage, Vector3 bulletTravelDirection)
	{
		GD.Print($"{Name} was hit for {damage}");

		if (playerHolding == 0)
		{
			LinearVelocity += bulletTravelDirection * 10;
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void Drop()
	{
		playerHolding = 0;
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