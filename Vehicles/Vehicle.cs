using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Vehicle : VehicleBody3D
{
	[Export] ShapeCast3D itemCast;
	[Export] Area3D area;
	public Seat driverSeat;
	float enginePower = 200f;
	float maxSteer = 0.8f;
	private Vector2 _inputDir;
	bool braking;
	public List<Seat> seats;
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

		driverSeat = GetNode<Seat>("DriverSeat");
		seats = GetChildren().Where(x => x is Seat)
							.Select(x => x as Seat)
							.ToList();

		area.BodyEntered += ItemEntered;
		area.BodyExited += ItemExited;
	}

	public override void _PhysicsProcess(double delta)
	{
		MovePassengers();

		if (!IsMultiplayerAuthority()) return;
	
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

	public void ItemEntered(Node3D node)
	{
		if (node is Item item)
		{
			item.vehicle = this;
			items.Add(item);
		}
	}

	public void ItemExited(Node3D node)
	{
		if (node is Item item)
		{
			item.vehicle = null;
			items.Remove(item);
		}
	}

	public Player GetPlayer(long Id)
	{
		var players = GetTree().GetNodesInGroup("Player");
		if (players.Count() < 1) return null;
		return players.Where(player => player.Name == Id.ToString()).First() as Player;
	}

	public void MovePassengers()
	{
		seats.ForEach(seat =>
		{
			if (seat.occupied)
			{
				var player = GetPlayer(seat.GetPlayerId());
				if (player is not null)
				{
					player.Sit(seat.GetPosition(), seat.GetRotation());
				}
			}
		});
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void Drive(int playerId, Vector2 inputDir, bool space, double delta)
	{
		if (driverSeat.occupied)
		{
			Steering = Mathf.Lerp(Steering, -inputDir.X * maxSteer, (float)delta * 1f);
			EngineForce = -inputDir.Y * enginePower;

			if (space)
			{
				Brake = 5f;
			}
			else
			{
				Brake = 0f;
			}
		}
		else
		{
			Brake = 5f;
		}
	}
}
