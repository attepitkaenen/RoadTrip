using Godot;
using System;

public partial class Installable : Item
{
    [Signal] public delegate void InstallPartEventHandler(int id, float condition);
    public bool canBeInstalled = false;


    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Install()
    {
        GD.Print("Starting " + Name + " install with Id: " + id + " and condition: " + condition);
        if (canBeInstalled)
        {
            EmitSignal(SignalName.InstallPart, id, condition);
            DestroyItem();
        }
    }

    public void SetCondition(float newCondition)
    {
        condition = newCondition;
    }
}
