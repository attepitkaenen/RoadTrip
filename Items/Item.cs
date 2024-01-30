using System.Runtime.InteropServices;
using Godot;

public partial class Item : RigidBody3D
{
	public int playerHolding = 0;
	[Export] MultiplayerSynchronizer multiplayerSynchronizer;
	[Export] public int ItemId;

	public Vehicle vehicle;

	// Sync properties
	public Vector3 syncPosition;
	public Basis syncBasis;
	public Vector3 syncLinearVelocity;
	public Vector3 syncAngularVelocity;


	public override void _Ready()
	{
		ContactMonitor = true;
		MaxContactsReported = 1;
		if (!IsMultiplayerAuthority())
		{
			CustomIntegrator = true;
			return;
		};
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsMultiplayerAuthority())
		{
			return;
		};

		syncLinearVelocity = LinearVelocity;
		syncAngularVelocity = AngularVelocity;
		syncPosition = GlobalPosition;
		syncBasis = GlobalBasis;
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
	}

	public bool IsColliding()
	{
		return GetCollidingBodies().Count > 0;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void Move(Vector3 handPosition, Basis handBasis, float strength, int playerId)
	{
		if (playerHolding == 0)
		{
			playerHolding = playerId;
		}
		else if (playerHolding == playerId)
		{
			Vector3 moveForce = (handPosition - GlobalPosition) * 20;

			LinearVelocity = moveForce;

			Vector3 angularForce = GetAngularVelocity(Basis, handBasis) * strength;
			if (angularForce.Length() < 100)
			{
				AngularVelocity = angularForce;
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void Drop()
	{
		playerHolding = 0;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void Throw(Vector3 handPosition, float strength)
	{
		LinearVelocity = handPosition * strength / 3;
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
