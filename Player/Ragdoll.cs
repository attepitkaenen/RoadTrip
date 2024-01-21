using Godot;
using System;
using System.Collections.Generic;

public partial class Ragdoll : Node3D
{
    [Export] MultiplayerSynchronizer synchronizer;
    List<PhysicalBone3D> bones;

    public override void _Ready()
    {
        var skeleton = GetNode<Skeleton3D>("Armature/Skeleton3D");
        var boneCount = skeleton.GetBoneCount();

        for (int i = 0; i < boneCount; i++)
        {

        }
    }
}
