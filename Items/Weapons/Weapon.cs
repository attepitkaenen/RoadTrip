using Godot;
using System;
using System.Text;

public partial class Weapon : HeldItem
{
    [ExportGroup("Required nodes")]
    [Export] public WeaponResource stats;
    [Export] private RayCast3D _bulletRay;
    [Export] private AudioStreamPlayer3D _audioPlayer;
    [Export] private GpuParticles3D _particles;
    [Export] private AnimationPlayer _animationPlayer;
    [Export] private Timer _timer;
    [Export] private PackedScene _bloodHit;
    private bool _isReadyToShoot;

    public override void _Ready()
    {
        _timer.Timeout += ReadyToShoot;
        _isReadyToShoot = true;
    }

    private void ReadyToShoot()
    {
        _isReadyToShoot = true;
    }

    public override void LeftClick()
    {
        if (!_isReadyToShoot) return;
        _timer.Start(stats.Firerate);
        _isReadyToShoot = false;
        dynamic collider = _bulletRay.GetCollider();
        var bulletTravelDirection = (_bulletRay.GetCollisionPoint() - GlobalPosition).Normalized();
        Rpc(nameof(PlayEffects));
        
        if (collider is Item || collider is Bone)
        {
            collider.Rpc(nameof(collider.Hit), stats.Damage, bulletTravelDirection);
            if (collider is Bone)
            {
                Rpc(nameof(SpawnHitEffect), _bulletRay.GetCollisionPoint(), _bulletRay.GetCollisionNormal());
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SpawnHitEffect(Vector3 position, Vector3 hitNormal)
    {
        var effect = _bloodHit.Instantiate<GpuParticles3D>();

        var result = new Basis();
        var scale = Basis.Scale;

        result.X = hitNormal.Cross(Basis.Z);
        result.Y = hitNormal;
        result.Z = Basis.X.Cross(hitNormal);

        result = result.Orthonormalized();
        result.X *= scale.X;
        result.Y *= scale.Y;
        result.Z *= scale.Z;

        effect.Basis = result;

        effect.Position = position;
        GetTree().Root.AddChild(effect, true);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void PlayEffects()
    {
        _animationPlayer.Stop();
        _animationPlayer.Play("fire");
        _audioPlayer.Play();
        _particles.Emitting = false;
        _particles.Emitting = true;
    }
}
