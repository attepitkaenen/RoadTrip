using System;
using System.Linq;
using Godot;
using Godot.Collections;


public partial class Ragdoll : Node3D
{
    [Export] Skeleton3D skeleton;
    [Export] Camera3D camera;
    [Export] MeshInstance3D head;
    [Export] private SkeletonIK3D HeadIK;
    [Export] private SkeletonIK3D HandIK;
    Player _player;
    Array<Bone> bones = new Array<Bone>();
    bool isActive;

    public override void _Ready()
    {
        _player = GetParent<Player>();
        HeadIK.Start();
        HandIK.Start();
        if (IsMultiplayerAuthority())
        {
            SetBoneCollision(false);
            head.Visible = false;
        }

        foreach (var child in skeleton.GetChildren())
        {
            if (child is Bone bone)
            {
                bones.Add(bone);
                if (IsMultiplayerAuthority())
                {
                    bone.SetCollisionLayerValue(6, false);
                }
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsMultiplayerAuthority() && isActive)
        {
            Dictionary<Vector3, Vector3> boneInfo = new Dictionary<Vector3, Vector3>();
            foreach (var bone in bones)
            {
                boneInfo[bone.Position] = bone.Rotation;
            }
            Rpc(nameof(UpdateRagdoll), boneInfo);
        }

        if (_player.playerInteraction.IsHolding())
        {
            HandIK.Interpolation = 1;
        }
        else
        {
            HandIK.Interpolation = 0;
        }

    }


    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void UpdateRagdoll(Dictionary<Vector3, Vector3> boneInfo)
    {
        var index = 0;
        foreach (var keyValuePair in boneInfo)
        {
            bones[index].Position = keyValuePair.Key;
            bones[index].Rotation = keyValuePair.Value;
            index++;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Activate(string boneName, Vector3 bonePushDirection)
    {
        if (isActive == true)
        {
            var bone = skeleton.GetChildren().First(node => node.Name == boneName) as Bone;
            bone.Impact(bonePushDirection);
            return;
        }

        isActive = true;
        HeadIK.Stop();
        HandIK.Stop();
        Position = Vector3.Zero;
        skeleton.PhysicalBonesStartSimulation();

        if (IsMultiplayerAuthority())
        {
            SetBoneCollision(true);
            camera.Current = true;
            var bone = skeleton.GetChildren().First(node => node.Name == boneName) as Bone;
            bone.Impact(bonePushDirection);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Deactivate()
    {
        if (IsMultiplayerAuthority())
        {
            camera.Current = true;
            SetBoneCollision(false);
        }
        HeadIK.Start();
        HandIK.Start();
        isActive = false;
        skeleton.PhysicalBonesStopSimulation();
    }

    public void SetBoneCollision(bool status)
    {
        if (status)
        {
            foreach (Bone bone in bones)
            {
                bone.SetCollisionLayerValue(6, true);
            }
        }
        else
        {
            foreach (Bone bone in bones)
            {
                bone.SetCollisionLayerValue(6, false);
            }
        }
    }

    public Vector3 GetUpPosition()
    {
        return new Vector3(bones[0].GlobalPosition.X, bones[0].GlobalPosition.Y + 1, bones[0].GlobalPosition.Z);
    }
}
