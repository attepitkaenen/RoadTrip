using Godot;
using System;

public partial class CarPart : Node3D
{
    [Export] private float _condition = 100f;
    public int itemId;
    EngineBay _engineBay;

    public override void _Ready()
    {
        _engineBay = GetParent<EngineBay>();
    }

    public void Damage(float amount)
    {
        _condition -= amount;
    }

    public float GetCondition()
    {
        return _condition;
    }

    public void SetCondition(float condition)
    {
        _condition = condition;
    }

    public void SetEngineBay(EngineBay engineBay)
    {
        _engineBay = engineBay;
    }

    public void Uninstall()
    {
        _engineBay.RpcId(1, nameof(_engineBay.RemoveInstalledPart), itemId, _condition, GlobalPosition);
        Rpc(nameof(DestroyPart));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void DestroyPart()
    {
        QueueFree();
    }
}
