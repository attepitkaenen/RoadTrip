using Godot;
using System;

public partial class Interactable : Node3D
{
    [Signal] public delegate void PressedEventHandler();

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Press()
    {
        EmitSignal(SignalName.Pressed);
    }
}
