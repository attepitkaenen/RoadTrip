using Godot;
using System;
using System.Linq;

public partial class Seat : RigidBody3D
{
	public Marker3D seatPosition;
	public long seatedPlayerId = -1;
	public bool occupied = false;
	[Export] public bool isDriverSeat = false;
	[Signal] public delegate void PlayerSeatedEventHandler(int playerId);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		seatPosition = GetNode<Marker3D>("SeatPosition");
	}

	public override void _PhysicsProcess(double delta)
	{
		var vehicle = GetParent();
		if (vehicle is Vehicle)
		{
			SetMultiplayerAuthority(vehicle.GetMultiplayerAuthority());
		}
		MovePassenger();
	}

	public Vector3 GetPosition()
	{
		return seatPosition.GlobalPosition;
	}

	public Vector3 GetRotation()
	{
		return seatPosition.GlobalRotation;
	}

	public long GetPlayerId()
	{
		return seatedPlayerId;
	}

	public Player GetPlayer(long Id)
	{
		var players = GetTree().GetNodesInGroup("Player");
		if (players.Count() < 1) return null;
		return players.First(player => player.Name == Id.ToString()) as Player;
	}

	public void MovePassenger()
	{
		if (occupied)
		{
			var player = GetPlayer(seatedPlayerId);
			if (player is not null)
			{
				player.MovePlayer(GetPosition(), GetRotation());
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void Sit(long playerId)
	{
		if (seatedPlayerId == -1)
		{
			seatedPlayerId = playerId;
			occupied = true;
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void Stand()
	{
		if (seatedPlayerId != -1)
		{
			seatedPlayerId = -1;
			occupied = false;
		}
	}
}
