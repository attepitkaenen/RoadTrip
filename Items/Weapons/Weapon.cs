using Godot;
using System;

public partial class Weapon : HeldItem
{
    [Export] public WeaponResource stats;
    [Export] private RayCast3D bulletRay;

    public override void LeftClick()
    {
        GD.Print("Bang!");
        var collider = bulletRay.GetCollider();
        if (collider is Player player)
        {
            player.RpcId(int.Parse(player.Name), nameof(player.Hit), stats.Damage);
        }
    }
}
