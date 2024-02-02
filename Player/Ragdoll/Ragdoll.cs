using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public partial class Ragdoll : Node3D
{
    [Export] MultiplayerSynchronizer synchronizer;
    [Export] Skeleton3D skeleton;
    [Export] Camera3D camera;
    [Export] MeshInstance3D head;
    private Vector3 _spawnVelocity = Vector3.Zero;
    public int playerId;
    List<Bone> bones = new List<Bone>();

    public override void _Ready()
    {
        skeleton.PhysicalBonesStartSimulation();
        foreach (var child in skeleton.GetChildren())
        {
            if (child is Bone)
            {
                bones.Add(child as Bone);
                synchronizer.ReplicationConfig.AddProperty($"{child.GetPath()}:position");
                synchronizer.ReplicationConfig.AddProperty($"{child.GetPath()}:rotation");
            }
        }

        foreach (Bone bone in bones)
        {
            bone.SetLinearVelocity(_spawnVelocity);
        }
    }


    public void MoveRagdoll(Vector3 position, Vector3 rotation, Vector3 linearVelocity)
    {
        Position = position;
        Rotation = new Vector3(Rotation.X, rotation.Y, Rotation.Z);
        _spawnVelocity = linearVelocity;
        // bones.ForEach(bone =>
        // {
        //     GD.Print(linearVelocity + " for bone " + bone.Name);
        //     bone.Rpc(nameof(bone.SetLinearVelocity), linearVelocity);
        // }
        // );
    }

    public void SwitchCamera()
    {
        camera.Current = !camera.Current;
        head.Visible = false;
    }

    public Vector3 GetUpPosition()
    {
        return new Vector3(bones[0].GlobalPosition.X, bones[0].GlobalPosition.Y + 1, bones[0].GlobalPosition.Z);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void Destroy()
    {
        bones.ForEach(bone =>
        {
            synchronizer.ReplicationConfig.RemoveProperty($"{bone.GetPath()}:position");
            synchronizer.ReplicationConfig.RemoveProperty($"{bone.GetPath()}:rotation");
        });
        QueueFree();
    }
}
