using Godot;
using System;

public partial class TransformUpdate : Node
{
	public ushort Tick { get; private set; }
	public Vector3 Position { get; private set; }

	public TransformUpdate(ushort tick, Vector3 position)
	{
		Tick = tick;
		Position = position;
	}
}
