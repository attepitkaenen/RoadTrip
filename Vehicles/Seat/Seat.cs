using Godot;
using System;
using System.Linq;

public partial class Seat : CarPart
{
    public Vehicle vehicle;
    public Marker3D seatPosition;
    private Player _seatedPlayer;
    private int _seatedPlayerId = 0;
    public bool occupied = false;
    [Export] public bool isDriverSeat = false;

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
    }

    public override void _Process(double delta)
    {
        if (_seatedPlayer is not null)
        {
            MovePassenger(_seatedPlayer);
        }
    }



    public Vehicle GetVehicle()
    {
        return vehicle;
    }

    public long GetSeatedPlayerId()
    {
        return _seatedPlayerId;
    }

    public Player GetPlayer(long id)
    {
        var players = GetTree().GetNodesInGroup("Player");
        if (players.Count() < 1) return null;
        return players.First(player => ((Player)player).id == id) as Player;
    }

    public void MovePassenger(Player player)
    {
        player.MovePlayer(seatPosition.GlobalPosition, seatPosition.GlobalRotation);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Sit(int playerId)
    {
        if (_seatedPlayerId == 0)
        {
            _seatedPlayerId = playerId;
            occupied = true;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Stand()
    {
        GD.Print("Stand up");
        if (_seatedPlayerId != 0)
        {
            _seatedPlayerId = 0;
            occupied = false;
        }
    }
}
