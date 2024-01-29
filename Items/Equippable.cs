using Godot;
using System;

public partial class Equippable : Item
{
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void PickUp()
	{
		QueueFree();
	}
}
