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
        foreach (var child in GetChildren())
        {
            if (child is Bone)
            {
                bones.Add(child as Bone);
                synchronizer.ReplicationConfig.AddProperty($"{child.GetPath()}:position");
                synchronizer.ReplicationConfig.AddProperty($"{child.GetPath()}:rotation");
            }
        }
    }

    public void MoveRagdoll(Vector3 position, Vector3 rotation, Vector3 linearVelocity)
    {
        Position = position;
        Rotation = new Vector3(Rotation.X, rotation.Y, Rotation.Z);
        bones.ForEach(bone => 
            bone.LinearVelocity = linearVelocity
        );
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
        // if (!Multiplayer.IsServer()) return;
        bones.ForEach(bone =>
        {
            synchronizer.ReplicationConfig.RemoveProperty($"{bone.GetPath()}:position");
            synchronizer.ReplicationConfig.RemoveProperty($"{bone.GetPath()}:rotation");
        });
        QueueFree();
    }
}
