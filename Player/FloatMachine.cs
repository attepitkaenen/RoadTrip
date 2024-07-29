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
	private float lerpFloatOffset = 0.7f;

	private float floatHeight;



	public override void _PhysicsProcess(double delta)
	{
		floatOffset = Mathf.Lerp(floatOffset, lerpFloatOffset, 0.05f);

		// Handle Floating
		if (player.movementState == Player.MovementState.seated) return;
		if (player.movementState == Player.MovementState.unconscious) return;
		player.Velocity += FloatPlayer();
	}

	public Vector3 FloatPlayer()
	{
		if (floatCast.IsColliding())
		{
			Vector3 closestCollisionPoint = floatCast.GetCollisionPoint(GetIndexOfClosestCollider());
			float distance = Math.Abs(player.Position.Y - closestCollisionPoint.Y);
			CapsuleShape3D playerCollider = player.collisionShape3D.Shape as CapsuleShape3D;
			floatHeight = -distance + floatOffset + (playerCollider.Height / 2);

			if (floatHeight > 0 && player.movementState != Player.MovementState.jumping)
			{
				player.isGrounded = true;
				return (Vector3.Up * floatForce * player.gravity * floatHeight) - (Vector3.Down * -player.Velocity.Y * dampingSpringStrength);
			}
			else if (floatHeight > -0.3 && player.movementState != Player.MovementState.jumping && player.isGrounded)
			{
				return Vector3.Up * 0.5f * player.gravity * floatHeight;
			}
			else
			{
				player.isGrounded = false;
			}
		}
		else
		{
			player.isGrounded = false;
		}
		return Vector3.Zero;
	}

	public Seat GetSeat()
	{
		if (floatCast.IsColliding())
		{
			var closestIndex = GetIndexOfClosestCollider();

			if (floatCast.GetCollider(closestIndex) is Seat seat)
			{
				return seat;
			}
		}
		return null;
	}

	public int GetIndexOfClosestCollider()
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

		return closestIndex;
	}

	public void SetFloatOffset(float offset)
	{
		lerpFloatOffset = offset;
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
}
