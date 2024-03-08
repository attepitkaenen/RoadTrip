using Godot;
using System;

public partial class PlayerAnimationHandler : AnimationTree
{
    [Export] Player _player;
    [Export] PlayerInteraction _playerInteraction;

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority()) return;
        if (_player.movementState == Player.MovementState.unconscious) return;
		Rpc(nameof(HandleRpcAnimations), _player.movementState.ToString(), _player.isGrounded, _player.Velocity, _player.GlobalBasis, _playerInteraction.IsHolding());
    }

    [Rpc(CallLocal = true)]
    public void HandleRpcAnimations(string state, bool isGroundedRpc, Vector3 velocity, Basis basis, bool isHolding)
    {
        Set("parameters/HoldingBlend/blend_amount", isHolding);

        if (state == "seated")
        {
            Set("parameters/StateMachine/conditions/walk", false);
            Set("parameters/StateMachine/conditions/jump", false);
            Set("parameters/StateMachine/conditions/sit", true);
            return;
        }
        else if (state == "jumping" || !isGroundedRpc)
        {
            Set("parameters/StateMachine/conditions/walk", false);
            Set("parameters/StateMachine/conditions/sit", false);
            Set("parameters/StateMachine/conditions/jump", true);
        }
        else
        {
            velocity *= basis;
            Vector2 walkingIntensity = new Vector2(-(velocity.Z / (_player.speed * 2)), velocity.X / (_player.speed * 2));
            Set("parameters/StateMachine/conditions/walk", true);
            Set("parameters/StateMachine/walking/blend_position", walkingIntensity);
            Set("parameters/StateMachine/conditions/sit", false);
            Set("parameters/StateMachine/conditions/jump", false);
        }
    }
}
