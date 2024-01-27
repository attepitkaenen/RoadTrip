using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public partial class Ragdoll : Node3D
{
    [Export] MultiplayerSynchronizer synchronizer;
    List<Bone> bones;

    public override void _Ready()
    {
        var skeleton = GetNode<Skeleton3D>("Armature/Skeleton3D");
        var boneCount = skeleton.GetBoneCount();
        bones = skeleton.GetChildren().Where(node => node is Bone).Select(node => node as Bone).ToList();
        bones.ForEach(bone =>
        {
            synchronizer.ReplicationConfig.AddProperty($"{bone.GetPath()}:position");
            synchronizer.ReplicationConfig.AddProperty($"{bone.GetPath()}:rotation");
        });
    }

}
