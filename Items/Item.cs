using Godot;
using System;

public partial class Item : RigidBody3D
{	
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void MoveItem(Vector3 hand, float Strength)
	{
		Vector3 moveForce = hand - GlobalPosition;
		LinearVelocity = moveForce * Strength;

		ContactMonitor = true;
		MaxContactsReported = 1;
	}
}
