using Godot;
using System;

public partial class StateHandler : Node
{
    private Player player;

    public MovementState movementState { get; private set; } = MovementState.idle;
    public enum MovementState
    {
        idle = 1,
        walking,
        crouching,
        jumping,
        seated,
        falling,
        unconscious
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        player = GetParent<Player>();
    }

	public void SetMovementState(ushort stateId)
	{
		movementState = (MovementState)stateId;
	}

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
		if (!player.isLocal) return;

		// Handle movementState changes
		if (player.Health <= 0)
		{
			movementState = MovementState.unconscious;
		}
		else if (!player.isGrounded && player.Velocity.Y < 0)
		{
			movementState = MovementState.falling;
		}
		else if (player.Velocity.Y > 0 && Input.IsActionPressed("jump"))
		{
			movementState = MovementState.jumping;
		}
		else if (player.isGrounded && Input.IsActionPressed("crouch"))
		{
			movementState = MovementState.crouching;
		}
		else if (player.isGrounded && new Vector2(player.Velocity.Z, player.Velocity.X).Length() > 0.1f)
		{
			movementState = MovementState.walking;
		}
		else if (player.isGrounded)
		{
			movementState = MovementState.idle;
		}
    }

    // public void HandleSeat()
    // {
    //     var newSeat = floatMachine.GetSeat();
    //     if (newSeat is Seat && Input.IsActionJustPressed("equip") && stateHandler.movementState != StateHandler.MovementState.seated && !newSeat.occupied)
    //     {
    //         _seat = newSeat;
    //         _seat.Rpc(nameof(_seat.Sit), id);
    //         stateHandler.movementState = StateHandler.MovementState.seated;
    //     }
    //     else if (Input.IsActionJustPressed("equip") && movementState == StateHandler.MovementState.seated)
    //     {
    //         _seat.Rpc(nameof(_seat.Stand));
    //         _seat = null;
    //         GlobalRotation = new Vector3(0, GlobalRotation.Y, 0);
    //         stateHandler.movementState = StateHandler.MovementState.idle;
    //     }
    // }
}
