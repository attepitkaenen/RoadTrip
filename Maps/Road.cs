using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

[Tool]
public partial class Road : Path3D
{
	[Export] RoadCast rayCast;
	private bool stickToGround;
	private bool stuckToGround = false;
	[Export] public bool StickToGround 
	{ 
		get {return stickToGround;} 
		set 
		{
			stickToGround = value;
			if (!value) return;
			StickRoadToGround();
		}
	}

    public override void _Process(double delta)
    {
		if (stuckToGround) return;
        StickRoadToGround();
    }
	
    private void StickRoadToGround()
    {
		List<Vector3> points = new List<Vector3>();

		for (int i = 0; i < Curve.PointCount; i++)
		{
			points.Add(Curve.GetPointPosition(i) + GlobalPosition);
		}

		var collisionPoints = rayCast.GetCollisionYs(points.ToArray());

		if (!collisionPoints.Contains(0)) stuckToGround = true;

		for (int i = 0; i < Curve.PointCount; i++)
		{
			var newPoint = Curve.GetPointPosition(i);
			newPoint.Y = collisionPoints[i];
			GD.Print(collisionPoints[i]);
			Curve.SetPointPosition(i, newPoint);
		}
    }
}