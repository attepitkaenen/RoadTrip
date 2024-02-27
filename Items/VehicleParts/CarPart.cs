using Godot;
using System;

public partial class CarPart : Node3D, IMounted
{
    [Export] private float _condition = 100f;
    private int _itemId;
    PartMount _mount;

    public override void _Ready()
    {
        var parent = GetParent();
        if (parent is PartMount mount)
        {
            _mount = mount;
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

    public int GetId()
    {
        return _itemId;
    }

    public void SetId(int itemId)
    {
        _itemId = itemId;
    }

    public void SetMount(PartMount mount)
    {
        _mount = mount;
    }

    public void SetPositionAndRotation(Vector3 position, Vector3 rotation)
    {
        Position = position;
        Rotation = rotation;
    }

    public void Uninstall()
    {
        if (_mount is not null)
        {
            _mount.Rpc(nameof(_mount.RemoveInstalledPart), _itemId, _condition, GlobalPosition, GlobalRotation);
            Rpc(nameof(DestroyPart));
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void DestroyPart()
    {
        QueueFree();
    }
}
