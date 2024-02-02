using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class RagdollSynchronizer : MultiplayerSynchronizer
{
    [Export] Skeleton3D skeleton;
    List<PhysicalBone3D> bones;
    public override void _Ready()
    {
        // foreach (var child in GetChildren())
        // {
        //     if (child is Bone)
        //     {
        //         ReplicationConfig.AddProperty($"{child.GetPath()}:position");
        //         ReplicationConfig.AddProperty($"{child.GetPath()}:rotation");
        //     }
        // }
    }
}
