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
    public int playerId;
    List<PhysicalBone3D> bones;

    public override void _Ready()
    {
        skeleton.PhysicalBonesStartSimulation();
        bones = skeleton.GetChildren().Where(node => node is PhysicalBone3D).Select(node => node as PhysicalBone3D).ToList();
        bones.ForEach(bone =>
        {
            synchronizer.ReplicationConfig.AddProperty($"{bone.GetPath()}:position");
            synchronizer.ReplicationConfig.AddProperty($"{bone.GetPath()}:rotation");
        });
    }

    public void MoveRagdoll(Vector3 position, Vector3 rotation, Vector3 linearVelocity)
    {
        Position = position;
        Rotation = new Vector3(Rotation.X, rotation.Y, Rotation.Z);
        var bones = skeleton.GetChildren().Where(node => node is Bone).Select(node => node as Bone).ToList();
        GD.Print(bones.Count());
        bones.ForEach(bone =>
        {
            // bone.LinearVelocity = linearVelocity;
            // bone.Rpc(nameof(bone.SetLinearVelocity), linearVelocity);
            bone.SetLinearVelocity(linearVelocity);
        });
    }

    public void SwitchCamera()
    {
        camera.Current = !camera.Current;
        head.Visible = false;
    }

    public Vector3 GetUpPosition()
    {
        return  new Vector3(bones[0].GlobalPosition.X, bones[0].GlobalPosition.Y + 1, bones[0].GlobalPosition.Z);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void Destroy()
    {
        // if (!Multiplayer.IsServer()) return;
        bones.ForEach(bone =>
        {
            synchronizer.ReplicationConfig.RemoveProperty($"{bone.GetPath()}:position");
            synchronizer.ReplicationConfig.RemoveProperty($"{bone.GetPath()}:rotation");
        });
        QueueFree();
    }
}
