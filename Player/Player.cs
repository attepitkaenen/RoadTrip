using Godot;

public partial class Player : CharacterBody3D
{
	[ExportGroup("Required Nodes")]
	[Export] public Camera3D camera;
	[Export] public AnimationTree animationTree;
	[Export] public FloatMachine floatMachine;

	[ExportGroup("Movement properties")]
	[Export] public float sensitivity = 0.001f;
	[Export] public float acceleration = 4f;
	[Export] public float deceleration = 5f;
	[Export] public float speed = 3f;
	[Export] public float jumpVelocity = 4.5f;
	public bool isGrounded;

	[ExportGroup("Interaction properties")]
	[Export] public RayCast3D interaction;
	[Export] public Marker3D hand;
	[Export] public StaticBody3D staticBody;
	[Export] public Generic6DofJoint3D joint;
	public Item PickedItem;
	public float Strength = 40f;

	// Sync properties
	Vector3 syncPosition;
	Vector3 syncRotation;


	public bool holdingItem;


	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public override void _EnterTree()
	{
		SetMultiplayerAuthority(int.Parse(Name));
	}

	public override void _Ready()
	{
		if (!IsMultiplayerAuthority()) return;

		camera.Current = true;
		GetNode<MeshInstance3D>("characterAnimated/Armature/Skeleton3D/Head").Hide();
		GetNode<MeshInstance3D>("characterAnimated/Armature/Skeleton3D/Eyes").Hide();
		GetNode<MeshInstance3D>("characterAnimated/Armature/Skeleton3D/Nose").Hide();
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		if (!IsMultiplayerAuthority())
		{
			return;
		}

		if (@event is InputEventMouseMotion)
		{
			InputEventMouseMotion mouseMotion = @event as InputEventMouseMotion;
			RotateY(-mouseMotion.Relative.X * sensitivity);
			camera.RotateX(-mouseMotion.Relative.Y * sensitivity);

			Vector3 cameraRotation = camera.Rotation;
			cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(-88f), Mathf.DegToRad(80f));
			camera.Rotation = cameraRotation;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsMultiplayerAuthority())
		{
			GlobalPosition = GlobalPosition.Lerp(syncPosition, 0.1f);
			GlobalRotation = new Vector3(0, Mathf.LerpAngle(GlobalRotation.Y, syncRotation.Y, 0.1f), 0);
			return;
		};

		PickObject();
		Rpc(nameof(HandleRpcAnimations), Input.IsActionJustPressed("jump"), isGrounded, Velocity, Transform);

		// Movement logic
		syncPosition = GlobalPosition;
		syncRotation = GlobalRotation;

		Vector3 velocity = Velocity;
		velocity.Y -= gravity * (float)delta;
		// Handle Jump.
		if (Input.IsActionJustPressed("jump") && isGrounded)
		{
			velocity.Y = jumpVelocity;
		}
		// Handle Crouch
		if (Input.IsActionPressed("crouch"))
		{
			floatMachine.SetFloatOffset(0.1f);
		}
		else
		{
			floatMachine.SetFloatOffset(0.7f);
		}
		// Make speed dependent on the crouchHeight
		float correctedSpeed = speed * floatMachine.GetCrouchHeight();
		// Handle sprint
		if (Input.IsActionPressed("sprint") && floatMachine.GetCrouchHeight() > 0.8f)
		{
			correctedSpeed *= 2f;
		}
		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			velocity.X = Mathf.Lerp(velocity.X, direction.X * correctedSpeed, (float)delta * acceleration);
			velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * correctedSpeed, (float)delta * acceleration);
		}
		else
		{
			velocity.X = Mathf.Lerp(velocity.X, 0, (float)delta * deceleration);
			velocity.Z = Mathf.Lerp(velocity.Z, 0, (float)delta * deceleration);
		}

		Velocity = velocity;
		MoveAndSlide();
	}


	[Rpc(CallLocal = true)]
	public void HandleRpcAnimations(bool isJumping, bool isGroundedRpc, Vector3 velocity, Transform3D transform)
	{
		if (isJumping || !isGroundedRpc)
		{
			animationTree.Set("parameters/conditions/jump", true);
			animationTree.Set("parameters/conditions/walk", false);
		}
		else
		{
			velocity *= transform.Basis;
			animationTree.Set("parameters/conditions/jump", false);
			animationTree.Set("parameters/conditions/walk", true);
			animationTree.Set("parameters/Walk/blend_position", new Vector2(-(velocity.Z / (speed * 2)), velocity.X / (speed * 2)));

		}
	}


	public void PickObject()
	{
		if (Input.IsActionJustPressed("leftClick"))
		{
			if (interaction.GetCollider() is Item item && PickedItem is null)
			{
				PickedItem = item;
			}
			else
			{
				PickedItem = null;
			}
		}

		if (PickedItem is not null)
		{
			staticBody.Position = hand.Position;

			PickedItem.Rpc(nameof(PickedItem.MoveItem), hand.GlobalPosition, Strength);
		}
	}

	// public void PickObject()
	// {
	// 	if (Input.IsActionJustPressed("leftClick"))
	// 	{
	// 		GD.Print(interaction.GetCollider());
	// 		if (interaction.GetCollider() is Item item)
	// 		{
	// 			PickedItem = PickedItem is null ? item: null;
	// 		}
	// 		else
	// 		{
	// 			PickedItem = null;
	// 		}

	// 		if (PickedItem is not null)
	// 		{
	// 			hand.GlobalPosition = PickedItem.GlobalPosition;
	// 			joint.NodeB = PickedItem.GetPath();
	// 		}
	// 		else
	// 		{
	// 			joint.NodeB = joint.GetPath();
	// 		}
	// 	}

	// 	if (PickedItem is not null)
	// 	{
	// 		MoveObject();

	// 		if (Input.IsActionJustPressed("rightClick"))
	// 		{
	// 			var moveForce = (hand.GlobalPosition - GlobalPosition).Normalized() * (Strength / 150);
	// 			PickedItem.LinearVelocity = moveForce * Strength;
	// 			PickedItem = null;
	// 			joint.NodeB = joint.GetPath();
	// 		}

	// 		// If you choose to make the item movement velocity based instead of just setting the globalPosition
	// 		// Vector3 handToItemDistance = hand.GlobalPosition - PickedItem.GlobalPosition;
	// 		// if (handToItemDistance.Length() > 1 && PickedItem.GetCollidingBodies().Count > 0)
	// 		// {
	// 		// 	PickedItem.ContactMonitor = false;
	// 		// 	PickedItem.LinearVelocity = Vector3.Zero;
	// 		// 	PickedItem = null;
	// 		// 	joint.NodeB = joint.GetPath();
	// 		// }
	// 	}

	// }


	// public void MoveObject()
	// {
	// 	hand.Position = new Vector3(0, 0, hand.Position.Z);

	// 	if (Input.IsActionJustPressed("scrollDown") && hand.Position.Z < -1)
	// 	{
	// 		hand.Position = new Vector3(hand.Position.X, hand.Position.Y, hand.Position.Z + 0.1f);
	// 	}
	// 	else if (Input.IsActionJustPressed("scrollUp") && hand.Position.Z > -3)
	// 	{
	// 		hand.Position = new Vector3(hand.Position.X, hand.Position.Y, hand.Position.Z - 0.1f);
	// 	}

	// 	PickedItem.Rpc(nameof(PickedItem.MoveItem), hand, staticBody, Strength);

	// 	// PickedItem.MoveItem(hand, staticBody);
	// }
}
