using System;
using System.Linq;
using Godot;
using Godot.Collections;


public partial class Ragdoll : Node3D
{
    [Export] public MultiplayerSynchronizer multiplayerSynchronizer;
    [Export] Skeleton3D skeleton;
    [Export] Camera3D camera;
    [Export] MeshInstance3D head;
    private Vector3 _spawnVelocity = Vector3.Zero;
    public int playerId;
    Array<Bone> bones = new Array<Bone>();
    bool isActive;

    public override void _Ready()
    {
        Visible = false;

        foreach (var child in skeleton.GetChildren())
        {
            if (child is Bone bone)
            {
                bones.Add(bone);
                bone.SetCollisionLayerValue(6, false);
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
    public void Activate(Vector3 position)
    {
        if (IsMultiplayerAuthority())
        {
            camera.Current = true;
            head.Visible = false;
        }

        isActive = true;

        SetBoneCollision(true);
        Position = Vector3.Zero;
        skeleton.PhysicalBonesStartSimulation();

        Visible = true;

    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Deactivate()
    {
        isActive = false;

        skeleton.PhysicalBonesStopSimulation();
        SetBoneCollision(false);
        Visible = false;
        if (!IsMultiplayerAuthority()) return;
        camera.Current = true;
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
