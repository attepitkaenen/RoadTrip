using Godot;
using System;

public partial class Weapon : HeldItem
{
    [Export] public WeaponResource stats;
    [Export] private RayCast3D _bulletRay;
    [Export] private AudioStreamPlayer3D _audioPlayer;

    public override void LeftClick()
    {
        GD.Print("Bang!");
        dynamic collider = _bulletRay.GetCollider();
        var bulletTravelDirection = (_bulletRay.GetCollisionPoint() - GlobalPosition).Normalized();
        Rpc(nameof(PlaySound));
        if (collider is Player player)
        {
            player.RpcId(int.Parse(player.Name), nameof(player.Hit), stats.Damage, bulletTravelDirection * stats.Damage / 3);
        }
        else if (collider is Item || collider is Bone)
        {
            collider.Rpc(nameof(collider.Hit), stats.Damage, bulletTravelDirection);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void PlaySound()
    {
        _audioPlayer.Play();
    }
}
