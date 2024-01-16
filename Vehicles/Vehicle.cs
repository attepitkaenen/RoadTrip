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
	public Vector3 syncPosition;
	Vector3 syncRotation;

	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		driverSeat = GetNode<Seat>("DriverSeat");
		// driverSeat.PlayerSeated += SetAuthority;
		seats = GetChildren().Where(x => x is Seat)
							.Select(x => x as Seat)
							.ToList();

		area.BodyEntered += ItemEntered;
		area.BodyExited += ItemExited;
	}

	public override void _PhysicsProcess(double delta)
	{
		MovePassengers();

		if (!IsMultiplayerAuthority())
		{
			GlobalPosition = GlobalPosition.Lerp(syncPosition, 0.01f);
			GlobalRotation = new Vector3(Mathf.LerpAngle(GlobalRotation.X, syncRotation.X, 0.01f), Mathf.LerpAngle(GlobalRotation.Y, syncRotation.Y, 0.01f), Mathf.LerpAngle(GlobalRotation.Z, syncRotation.Z, 0.01f));
			return;
		};

		syncPosition = GlobalPosition;
		syncRotation = GlobalRotation;
	}

	public void ItemEntered(Node3D node)
	{
		if (node is Item item)
		{
			// item.CustomIntegrator = true;
			items.Add(item);
			// item.SetMultiplayerAuthority(GetMultiplayerAuthority());
		}
	}

	public void ItemExited(Node3D node)
	{
		if (node is Item item)
		{
			GD.Print($"item exited area: {node}");
			// item.SetMultiplayerAuthority(1);
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
				// GD.Print(seat.GetPlayerId());
				var player = GetPlayer(seat.GetPlayerId());
				if (player is not null)
				{
					// player.Rpc(nameof(player.Sit), seat.GetPosition(), seat.GetRotation());
					player.Sit(seat.GetPosition(), seat.GetRotation());
				}
			}
		});
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void Drive(int playerId, Vector2 inputDir, bool space, double delta)
	{
		// SetMultiplayerAuthority(playerId);
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
