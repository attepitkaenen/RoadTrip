using Godot;
using System;

public partial class Tire : CarPart
{
	[Export] public float tireRadius = 0.3f;
	[Export] public float tireWidth = 0.2f;
	[Export] public float frictionSlip = 1.5f;

	// Called when the node enters the scene tree for the first time.

	public float GetRadius()
	{
		return tireRadius;
	}

	public float GetWidth()
	{
		return tireWidth;
	}

	public float GetFrictionSlip()
	{
		return frictionSlip;
	}
}
