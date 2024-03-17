using System;
using Godot;

public partial class Item : RigidBody3D
{
	public int playerHolding = 0;
	[Export] public int id;
	[Export] public float condition;
	[Export] public ItemTypeEnum type;
	[Export] bool isLogging = false;
	GameManager gameManager;

	// Sync properties
	public Vector3 syncPosition;
	public Basis syncBasis;
	public Vector3 syncLinearVelocity;
	public Vector3 syncAngularVelocity;

	public Vector3 lastPosition;
	public Basis lastBasis;

	private bool _queuedForDeletion = false;

	Timer timer;


	public override void _Ready()
	{
		AddToGroup("Items");
		gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
		ContactMonitor = true;
		MaxContactsReported = 1;
		if (!IsMultiplayerAuthority())
		{
			CustomIntegrator = true;
			return;
		};
		timer = new Timer();
		AddChild(timer, true);
		timer.Timeout += SyncProperties;
		timer.Start(2);
	}

	public void SyncProperties()
	{
		timer.Start(2);
		Rpc(nameof(SyncItem), GlobalPosition, GlobalBasis, LinearVelocity, AngularVelocity);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
	public void SyncItem(Vector3 position, Basis basis, Vector3 linearVelocity, Vector3 angularVelocity)
	{
		syncPosition = position;
		syncBasis = basis;
		syncLinearVelocity = linearVelocity;
		syncAngularVelocity = angularVelocity;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsMultiplayerAuthority())
		{
			return;
		};

		Quaternion q1 = GlobalBasis.GetRotationQuaternion();
		Quaternion q2 = lastBasis.GetRotationQuaternion();

		// Quaternion that transforms q1 into q2
		Quaternion qt = q2 * q1.Inverse();

		// Angle from quatertion
		float angle = 2 * Mathf.Acos(qt.W);

		// There are two distinct quaternions for any orientation
		// Ensure we use the representation with the smallest angle
		if (angle > Mathf.Pi)
		{
			angle = Mathf.Tau - angle;
		}

		if (LinearVelocity.Length() > 0.1f || AngularVelocity.Length() > 0.1f || angle > 0.1f || (lastPosition - GlobalPosition).Length() > 0.01f && !_queuedForDeletion)
		{
			lastBasis = GlobalBasis;
			lastPosition = GlobalPosition;
			Rpc(nameof(SyncItem), GlobalPosition, GlobalBasis, LinearVelocity, AngularVelocity);
		}
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

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void QueueItemDestruction()
	{
		_queuedForDeletion = true;
		Rpc(nameof(DestroyItem));
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void DestroyItem()
	{
		QueueFree();
	}

	public void LashItemDown(Vector3 position, Vector3 rotation)
	{
		GlobalPosition = position;
		GlobalRotation = rotation;
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
	public virtual void Move(Vector3 handPosition, Basis handBasis, int playerId)
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
