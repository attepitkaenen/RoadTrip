using Godot;
using System.Collections.Generic;

[Tool]
public partial class RoadCast : RayCast3D
{
	[Export]
	public bool CheckCollisionPoint
	{
		get { return false;}
		set
		{
			PhysicsServer3D.SetActive(true);
			GD.Print("Currently colliding at " + GetCollisionPoint());
		}
	}
	public override void _Ready()
	{
		PhysicsServer3D.SetActive(true);
		GD.Print("Currently colliding at " + GetCollisionPoint());
	}
	public float[] GetCollisionYs(Vector3[] points)
	{
		List<float> collisionPoints = new List<float>();

		foreach (var point in points)
		{
			GD.Print("Currently checking point: " + point);
			GlobalPosition = point;
			GD.Print(GlobalPosition);
			GD.Print("Colliding at: " + GetCollisionPoint());
			collisionPoints.Add(GetCollisionPoint().Y);
		}
		return collisionPoints.ToArray();
	}
}
