using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Ragdoll : Node3D
{
    [Export] MultiplayerSynchronizer synchronizer;
    List<PhysicalBone3D> bones;

    public override void _Ready()
    {
        var skeleton = GetNode<Skeleton3D>("Armature/Skeleton3D");
        var boneCount = skeleton.GetBoneCount();
        bones = skeleton.GetChildren().Where(node => node is PhysicalBone3D).Select(node => node as PhysicalBone3D).ToList();
        bones.ForEach(bone => {
            synchronizer.ReplicationConfig.AddProperty($"{bone.GetPath()}:position");
            synchronizer.ReplicationConfig.AddProperty($"{bone.GetPath()}:rotation");
        });
    }
}
