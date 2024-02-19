using Godot;
using System;

public partial class CarPart : Node3D
{
    [Export] private float _condition = 100f;
    public int itemId;
    EngineBay _engineBay;
    TireMount _tireMount;

    public override void _Ready()
    {
        var parent = GetParent();
        if (parent is EngineBay engineBay)
        {
            _engineBay = engineBay;
        }
        else if (parent is TireMount tireMount)
        {
            _tireMount = tireMount;
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

    public void SetEngineBay(EngineBay engineBay)
    {
        _engineBay = engineBay;
    }

    public void SetTireMount(TireMount tireMount)
    {
        _tireMount = tireMount;
    }

    public void Uninstall()
    {
        if (_engineBay is null)
        {
            _tireMount.RpcId(1, nameof(_tireMount.RemoveInstalledPart), itemId, _condition, GlobalPosition);
        }
        else if (_tireMount is null)
        {
            _engineBay.RpcId(1, nameof(_engineBay.RemoveInstalledPart), itemId, _condition, GlobalPosition);
        }
        Rpc(nameof(DestroyPart));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void DestroyPart()
    {
        QueueFree();
    }
}
