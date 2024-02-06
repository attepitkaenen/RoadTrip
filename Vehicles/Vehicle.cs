using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Vehicle : VehicleBody3D
{
	[Export] private MultiplayerSynchronizer _synchronizer;
	[Export] private Label3D _speedometer;
	[Export] private EngineBay _engineBay;
	private Seat _driverSeat;
	float enginePower = 0;
	float maxSteer = 0.8f;
	private Vector2 _inputDir;
	bool braking;
	public List<Item> items = new List<Item>();

	// Sync properties
	Vector3 syncPosition;
	Vector3 syncRotation;
	Basis syncBasis;
	Vector3 syncLinearVelocity;
	Vector3 syncAngularVelocity;


	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (!IsMultiplayerAuthority())
		{
			CustomIntegrator = true;
		};

		_driverSeat = GetNode<Seat>("DriverSeat");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsMultiplayerAuthority()) return;

		enginePower = _engineBay.GetHorsePower();

		if (_driverSeat.seatedPlayerId == -1)
		{
			Brake = 5f;
		}

		if (LinearVelocity.Length() < 0.1f && !_driverSeat.occupied)
		{
			_synchronizer.ReplicationInterval = 1f;
			_synchronizer.DeltaInterval = 1f;
		}
		else
		{
			_synchronizer.ReplicationInterval = 0.016f;
			_synchronizer.DeltaInterval = 0.016f;
		}

		if (_speedometer is not null)
		{
			_speedometer.Text = Math.Round(LinearVelocity.Length() * 3.6f, 0).ToString();
		}
		syncLinearVelocity = LinearVelocity;
		syncAngularVelocity = AngularVelocity;
		syncPosition = GlobalPosition;
		syncRotation = GlobalRotation;
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

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void Drive(float inputDir, float gas, bool space, double delta)
	{
		var steeringReducer = 1 / LinearVelocity.Length() * 10;
		steeringReducer = Mathf.Clamp(steeringReducer, 0.1f, 1);

		Steering = Mathf.Lerp(Steering, inputDir * steeringReducer * maxSteer, (float)delta * 1f);
		EngineForce = gas * enginePower;

		if (space)
		{
			Brake = 5f;
		}
		else
		{
			Brake = 0f;
		}
	}

	public void ItemEntered(Node3D node)
	{
		// if (node is Item item)
		// {
		// 	item.vehicle = this;
		// 	items.Add(item);
		// }
	}

	public void ItemExited(Node3D node)
	{
		// if (node is Item item)
		// {
		// 	item.vehicle = null;
		// 	items.Remove(item);
		// }
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void Flip(Vector3 axel)
	{
		Vector3 angularForce = GetAngularVelocity(Basis, Basis.Rotated(axel.Normalized(), 0.5f));
		if (angularForce.Length() < 100)
		{
			AngularVelocity = angularForce;
		}
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
