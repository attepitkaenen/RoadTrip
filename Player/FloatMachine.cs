using Godot;
using System;
using System.Linq;

public partial class FloatMachine : Node
{
	[Export] public ShapeCast3D floatCast;
	[Export] public Player player;

	[ExportGroup("Floating properties")]
	[Export] private float floatForce = 0.2f;
	[Export] private float dampingSpringStrength = 0.2f;
	[Export] private float floatOffset = 0.7f;

	private float floatHeight;

	public Vector3 Float()
	{
		if (floatCast.IsColliding())
		{
			var position = player.Position;
			var distance = 2f;
			int closestIndex = 0;

			for (int i = 0; i < floatCast.GetCollisionCount(); i++)
			{
				var newPoint = floatCast.GetCollisionPoint(i);
				var newDistance = new Vector3(newPoint.X, position.Y, newPoint.Z).DistanceTo(newPoint);
				if (newDistance < distance)
				{
					distance = newDistance;
					closestIndex = i;
				}
			}

			if (floatCast.GetCollider(closestIndex) is RigidBody3D)
			{
				var body = (RigidBody3D)floatCast.GetCollider(0);
				var moveForce = Vector3.Down * floatForce * player.gravity;
				body.ApplyForce(moveForce);
			}


			// the number is supposed to be half of the hitbox height
			floatHeight = -distance + floatOffset + 0.6f;

			if (floatHeight > 0)
			{
				player.isGrounded = true;
				return (Vector3.Up * floatForce * player.gravity * floatHeight) - (Vector3.Down * -player.Velocity.Y * dampingSpringStrength);
			}

			player.isGrounded = false;
		}

		return Vector3.Zero;
	}

	public void SetFloatOffset(float offset)
	{
		floatOffset = offset;
	}

	public float GetCrouchHeight()
	{
		if (floatCast.IsColliding())
		{
			var position = player.Position;
			var collisionPoint = floatCast.GetCollisionPoint(0);
			var distance = new Vector3(collisionPoint.X, position.Y, collisionPoint.Z).DistanceTo(collisionPoint);
			return Math.Clamp(distance, 0f, 1f);
		}
		return 1.15f;
	}

	public override void _PhysicsProcess(double delta)
	{
		// Handle Floating
		player.Velocity += Float();
	}
}
