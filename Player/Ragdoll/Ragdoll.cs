using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public partial class Ragdoll : Node3D
{
    [Export] MultiplayerSynchronizer synchronizer;
    [Export] Skeleton3D skeleton;
    List<PhysicalBone3D> bones;

    public override void _Ready()
    {
        skeleton.PhysicalBonesStartSimulation();
        bones = skeleton.GetChildren().Where(node => node is PhysicalBone3D).Select(node => node as PhysicalBone3D).ToList();
        GD.Print(bones.Count());
        bones.ForEach(bone =>
        {
            GD.Print($"{bone.GetPath()}:position");
            synchronizer.ReplicationConfig.AddProperty($"{bone.GetPath()}:position");
            synchronizer.ReplicationConfig.AddProperty($"{bone.GetPath()}:rotation");
        });
    }
}
