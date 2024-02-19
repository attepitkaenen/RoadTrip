using Godot;
using System;

public partial class Tire : CarPart
{
	[Export] public TireResource stats;

	// Called when the node enters the scene tree for the first time.

	public float GetRadius()
	{
		return stats.TireRadius;
	}

	public float GetWidth()
	{
		return stats.TireWidth;
	}
}
