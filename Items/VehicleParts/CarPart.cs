using Godot;
using System;

public partial class CarPart : Node3D
{
    [Export] private float _condition = 100f;
    public int itemId;
    IMount _mount;

    public override void _Ready()
    {
        var parent = GetParent();
        if (parent is EngineBay engineBay)
        {
            _mount = engineBay;
        }
        else if (parent is TireMount tireMount)
        {
            _mount = tireMount;
        }
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

    public void SetMount(IMount mount)
    {
        _mount = mount;
    }

    public void Uninstall()
    {
        if (_mount is not null)
        {
            _mount.RpcId(1, nameof(_mount.RemoveInstalledPart), itemId, _condition, GlobalPosition);
            Rpc(nameof(DestroyPart));
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void DestroyPart()
    {
        QueueFree();
    }
}
