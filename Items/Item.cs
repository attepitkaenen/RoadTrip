using Godot;

public partial class Item : RigidBody3D
{
	public int playerHolding = 0;

	// Sync properties
	public Vector3 syncPosition;
	Vector3 syncRotation;

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority())
		{
			GlobalPosition = syncPosition;
			GlobalRotation = syncRotation;
			// GlobalPosition = GlobalPosition.Lerp(syncPosition, 0.01f);
			// GlobalRotation = new Vector3(Mathf.LerpAngle(GlobalRotation.X, syncRotation.X, 0.01f), Mathf.LerpAngle(GlobalRotation.Y, syncRotation.Y, 0.01f), Mathf.LerpAngle(GlobalRotation.Z, syncRotation.Z, 0.01f));
			return;
		};

		syncPosition = GlobalPosition;
		syncRotation = GlobalRotation;
	}

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void MoveItem(Vector3 handPosition, Basis handBasis, float strength, int player)
	{
		if (player == 0)
		{
			playerHolding = 0;
		}
		else if (playerHolding == 0)
		{
			playerHolding = player;
		}
		else if (playerHolding == player)
		{
			Vector3 moveForce = (handPosition - GlobalPosition) * strength;
			LinearVelocity = moveForce;

			Vector3 angularForce = GetAngularVelocity(Basis, handBasis) * strength;
			if (angularForce.Length() < 100)
			{
				AngularVelocity = angularForce;
			}

			ContactMonitor = true;
			MaxContactsReported = 1;
		}
		return;
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
