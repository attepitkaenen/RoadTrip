using Godot;
using System;

public partial class SeatMount : PartMount
{
	[Export] bool isDriverSeat = false;

    // Spawns installed part and sets its condition and id
    public override IMounted SpawnInstalledPart(int id, float condition, Vector3 partPosition, Vector3 partRotation)
	{
		Seat seat = base.SpawnInstalledPart(id, condition, partPosition, partRotation) as Seat;
		seat.isDriverSeat = isDriverSeat;
		seat.vehicle = GetParent().GetParent<Vehicle>();
		return seat;
	}
}
