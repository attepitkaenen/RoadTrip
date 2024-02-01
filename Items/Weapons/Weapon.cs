using Godot;
using System;

public partial class Weapon : HeldItem
{
    [Export] public WeaponResource stats;
    [Export] private RayCast3D bulletRay;

    public override void LeftClick()
    {
        GD.Print("Bang!");
        dynamic collider = bulletRay.GetCollider();
        var bulletTravelDirection = (bulletRay.GetCollisionPoint() - GlobalPosition).Normalized();
        if (collider is Player player)
        {
            player.RpcId(int.Parse(player.Name), nameof(player.Hit), stats.Damage, bulletTravelDirection);
        }
        else if (collider is Item || collider is Bone)
        {
            collider.Rpc(nameof(collider.Hit), stats.Damage, bulletTravelDirection);
        }
    }
}
