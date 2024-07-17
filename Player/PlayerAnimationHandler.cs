using Godot;
using System;

public partial class PlayerAnimationHandler : AnimationTree
{
    [Export] Player _player;
    [Export] PlayerInteraction _playerInteraction;

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority()) return;
        if (_player.stateHandler.movementState == StateHandler.MovementState.unconscious) return;
        if (_player.isLocal)
        {
		    HandleRpcAnimations(_player.stateHandler.movementState.ToString(), _player.Velocity, _player.GlobalBasis, _playerInteraction.IsHolding());
        }
        else
        {
		    HandleRpcAnimations(_player.stateHandler.movementState.ToString(), _player.syncVelocity, _player.GlobalBasis, _playerInteraction.IsHolding());
        }
    }

    public void HandleRpcAnimations(string state, Vector3 velocity, Basis basis, bool isHolding)
    {
        Set("parameters/HoldingBlend/blend_amount", isHolding);

        if (state == "seated")
        {
            Set("parameters/StateMachine/conditions/walk", false);
            Set("parameters/StateMachine/conditions/jump", false);
            Set("parameters/StateMachine/conditions/sit", true);
            return;
        }
        else if (state == "jumping")
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
