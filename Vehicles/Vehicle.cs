using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Vehicle : VehicleBody3D
{
	[Export] private MultiplayerSynchronizer _multiplayerSynchronizer;
	[Export] public EngineBay engineBay;
	[Export] public float breakForce = 50;
	[Export] private Area3D _itemArea;
	[Export] private Seat _driverSeat;
	float enginePower = 0;
	float maxSteer = 0.8f;
	private Vector2 _inputDir;
	bool braking;
	public Dictionary<Item, Marker3D> items = new Dictionary<Item, Marker3D>();
	public List<VehicleWheel3D> wheels = new List<VehicleWheel3D>();

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

		// var seats = GetChildren().Where(node => node is Seat).Select(node => node as Seat);
		// foreach (Seat seat in seats)
		// {
		// 	_multiplayerSynchronizer.ReplicationConfig.AddProperty(seat.GetPath() + ":seatedPlayerId");
		// }

		_itemArea.BodyEntered += ItemEntered;
		_itemArea.BodyExited += ItemExited;
	}

	public override void _PhysicsProcess(double delta)
	{
		HandleItems();

		if (!IsMultiplayerAuthority()) return;


		enginePower = engineBay.GetHorsePower();

		if (_driverSeat.GetSeatedPlayerId() < 1)
		{
			Brake = 10;
			EngineForce = 0;
		}

		if (LinearVelocity.Length() < 0.1f && !_driverSeat.occupied && false) // REMOVE FALSE WHEN DONE
		{
			_multiplayerSynchronizer.ReplicationInterval = 1f;
			_multiplayerSynchronizer.DeltaInterval = 1f;
		}
		else
		{
			_multiplayerSynchronizer.ReplicationInterval = 0.016f;
			_multiplayerSynchronizer.DeltaInterval = 0.016f;
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
			Brake = breakForce;
		}
		else
		{
			Brake = 0f;
		}
	}

	public void ItemEntered(Node3D node)
	{
		if (LinearVelocity.Length() > 5) return;

		if (node is Item item)
		{
			Rpc(nameof(AddItemToList), item.GetPath());
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void AddItemToList(string itemPath)
	{
		Item item = GetNodeOrNull<Item>(itemPath);
		if ((items.Count > 0 && items.Keys.Contains(item)) || item is null)
		{
			return;
		}
		Marker3D positionInVehicle = new Marker3D();
		AddChild(positionInVehicle, true);
		items.Add(item, positionInVehicle);
	}

	public void ItemExited(Node3D node)
	{
		if (node is Item item)
		{
			if (item is not null)
			{
				Rpc(nameof(RemoveItemFromList), item.GetPath());
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void RemoveItemFromList(string itemPath)
	{
        Item item = GetNodeOrNull<Item>(itemPath);
		if (item is null)
		{
			return;
		}

		if (!items.Keys.Contains(item))
		{
			return;
		}

		items[item].QueueFree();
		items.Remove(item);
	}

	public void HandleItems()
	{
		if (LinearVelocity.Length() > 5)
		{
			foreach (var item in items.Keys)
			{
				Marker3D itemMarker = items[item];
				item.LashItemDown(itemMarker.GlobalPosition, itemMarker.GlobalRotation);
			}
		}
		else
		{
			foreach (var item in items.Keys)
			{
				Marker3D itemMarker = items[item];
				itemMarker.GlobalPosition = item.GlobalPosition;
				itemMarker.GlobalRotation = item.GlobalRotation;
			}
		}
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
