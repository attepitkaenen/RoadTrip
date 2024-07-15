using Godot;
using System;
using System.Linq;

public partial class Seat : CarPart
{
	public Vehicle vehicle;
	public Marker3D seatPosition;
	private Player _seatedPlayer;
	private ushort _seatedPlayerId = 0;
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
		if (_seatedPlayer is null && _seatedPlayerId != 0)
		{
			_seatedPlayer = GetPlayer(_seatedPlayerId);
		}
		else if (_seatedPlayerId == 0)
		{
			_seatedPlayer = null;
		}

		if (_seatedPlayer is not null)
		{
			MovePassenger(_seatedPlayer);
		}
	}

	public Vehicle GetVehicle()
	{
		return vehicle;
	}

	public Vector3 GetPosition()
	{
		return seatPosition.GlobalPosition;
	}

	public Vector3 GetRotation()
	{
		return seatPosition.GlobalRotation;
	}

	public long GetSeatedPlayerId()
	{
		return _seatedPlayerId;
	}

	public Player GetPlayer(ushort id)
	{
		var players = GetTree().GetNodesInGroup("Player");
		if (players.Count() < 1) return null;
		return players.First(player => player.Name == id.ToString()) as Player;
	}

	public void MovePassenger(Player player)
	{
		// player.MovePlayer(GetPosition(), GetRotation());
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void Sit(ushort playerId)
	{
		GD.Print("Sit");
		if (_seatedPlayerId == 0)
		{
			_seatedPlayerId = playerId;
			occupied = true;
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void Stand()
	{
		if (_seatedPlayerId != 0)
		{
			_seatedPlayerId = 0;
			occupied = false;
		}
	}
}
