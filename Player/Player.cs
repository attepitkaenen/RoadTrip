using System.Linq;
using System.Net.Http;
using Godot;

public partial class Player : CharacterBody3D
{
	[ExportGroup("Required Nodes")]
	[Export] public Camera3D camera;
	[Export] public AnimationTree animationTree;
	[Export] public FloatMachine floatMachine;
	[Export] public Label3D nameTag;
	[Export] public Label stateLabel;
	[Export] public Label speedLabel;
	[Export] public Node3D head;

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
	private dynamic PickedItem;
	public float Strength = 40f;

	Seat seat;

	public MovementState movementState = MovementState.idle;
	public enum MovementState
	{
		idle,
		walking,
		running,
		crouching,
		jumping,
		seated,
		falling
	}

	// Sync properties
	public Vector3 syncPosition;
	Vector3 syncRotation;

	public Vector3 spawnLocation = new Vector3(0, 3, 0);
	public PlayerState playerState;


	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public override void _EnterTree()
	{
		SetMultiplayerAuthority(int.Parse(Name));
		Time.GetTicksMsec();
	}

	public override void _Ready()
	{
		playerState = GameManager.Players.Find(x => x.Id == int.Parse(Name));
		int playerIndex = GameManager.Players.FindIndex(x => x.Id == int.Parse(Name));
		sensitivity = GameManager.Sensitivity;

		foreach (Node3D spawnPoint in GetTree().GetNodesInGroup("SpawnPoints"))
		{
			if (int.Parse(spawnPoint.Name) == playerIndex)
			{
				spawnLocation = spawnPoint.GlobalPosition;

			}
		}

		nameTag.Text = playerState.Name;

		if (!IsMultiplayerAuthority())
		{
			return;
		}

		GlobalPosition = spawnLocation;

		camera.Current = true;
		GetNode<MeshInstance3D>("characterAnimated/Armature/Skeleton3D/Head").Hide();
		GetNode<MeshInstance3D>("characterAnimated/Armature/Skeleton3D/Eyes").Hide();
		GetNode<MeshInstance3D>("characterAnimated/Armature/Skeleton3D/Nose").Hide();
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		if (!IsMultiplayerAuthority()) return;
		
		if (PickedItem is not null &&
			Input.IsActionPressed("interact") &&
			@event is InputEventMouseMotion)
		{
			InputEventMouseMotion mouseMotion = @event as InputEventMouseMotion;
			staticBody.RotateY(mouseMotion.Relative.X * sensitivity);
			staticBody.RotateX(mouseMotion.Relative.Y * sensitivity);
		}
		else if (@event is InputEventMouseMotion)
		{
			InputEventMouseMotion mouseMotion = @event as InputEventMouseMotion;
			camera.RotateX(-mouseMotion.Relative.Y * sensitivity);

			Vector3 cameraRotation = camera.Rotation;
			cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(-85f), Mathf.DegToRad(85f));
			camera.Rotation = cameraRotation;

			if (movementState == MovementState.seated)
			{
				head.RotateY(-mouseMotion.Relative.X * sensitivity);
			}
			else
			{
				RotateY(-mouseMotion.Relative.X * sensitivity);
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (movementState == MovementState.seated && !IsMultiplayerAuthority()) return;
		
		//Lerp movement for other players
		if (!IsMultiplayerAuthority())
		{
			GlobalPosition = GlobalPosition.Lerp(syncPosition, 0.05f);
			GlobalRotation = new Vector3(Mathf.LerpAngle(GlobalRotation.X, syncRotation.X, 0.05f),
										Mathf.LerpAngle(GlobalRotation.Y, syncRotation.Y, 0.05f),
										Mathf.LerpAngle(GlobalRotation.Z, syncRotation.Z, 0.05f));
			return;
		};

		PickItem();

		HandleSeat();

		Rpc(nameof(HandleRpcAnimations), Input.IsActionJustPressed("jump"), isGrounded, Velocity, Transform);


		// sync properties to lerp movement for other players
		syncPosition = GlobalPosition;
		syncRotation = GlobalRotation;

		float correctedSpeed = speed * floatMachine.GetCrouchHeight();
		float currentSpeed = new Vector2(Velocity.X, Velocity.Z).Length();

		stateLabel.Text = movementState.ToString();
		speedLabel.Text = currentSpeed.ToString();

		// Stop movement if seated
		if (movementState == MovementState.seated)
		{
			Velocity = Vector3.Zero;
			if (seat is Seat && seat.isDriverSeat)
			{
				Vehicle vehicle = seat.GetParent<Vehicle>();
				vehicle.Rpc(nameof(vehicle.Drive), int.Parse(Name), Input.GetVector("left", "right", "up", "down"), Input.IsActionPressed("jump"), delta);
			}
			return;
		}


		head.GlobalRotation = GlobalRotation;

		// Movement logic
		Vector3 velocity = Velocity;

		// Gravity
		velocity.Y -= gravity * (float)delta;

		// Handle movementState changes
		if (Input.IsActionJustPressed("jump") && isGrounded)
		{
			velocity.Y = jumpVelocity;
			movementState = MovementState.jumping;
		}
		else if (!isGrounded && Velocity.Y < 0)
		{
			movementState = MovementState.falling;
		}
		else if (Input.IsActionPressed("crouch") && isGrounded)
		{
			movementState = MovementState.crouching;
		}
		else if (Input.IsActionPressed("sprint") && floatMachine.GetCrouchHeight() > 0.8f && currentSpeed > 0.1f && isGrounded)
		{
			movementState = MovementState.running;
		}
		else if (currentSpeed > 0.1f && isGrounded)
		{
			movementState = MovementState.walking;
		}
		else if (isGrounded)
		{
			movementState = MovementState.idle;
		}

		// Handle property changes depenging on the movementState
		switch (movementState)
		{
			case MovementState.idle:
				floatMachine.SetFloatOffset(0.7f);
				break;
			case MovementState.walking:
				floatMachine.SetFloatOffset(0.7f);
				break;
			case MovementState.running:
				correctedSpeed *= 2f;
				break;
			case MovementState.jumping:
				correctedSpeed *= 1.5f;
				isGrounded = false;
				break;
			case MovementState.falling:
				correctedSpeed *= 1.5f;
				break;
			case MovementState.crouching:
				floatMachine.SetFloatOffset(0.2f);
				break;
		}


		// Get the input direction and handle the movement/deceleration.
		Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero && isGrounded)
		{
			velocity.X = Mathf.Lerp(velocity.X, direction.X * correctedSpeed, (float)delta * acceleration);
			velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * correctedSpeed, (float)delta * acceleration);
		}
		else if (direction != Vector3.Zero && !isGrounded)
		{
			velocity.X = Mathf.Lerp(velocity.X, direction.X * correctedSpeed, (float)delta * acceleration);
			velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * correctedSpeed, (float)delta * acceleration);
		}
		else if (isGrounded)
		{
			velocity.X = Mathf.Lerp(velocity.X, 0, (float)delta * deceleration);
			velocity.Z = Mathf.Lerp(velocity.Z, 0, (float)delta * deceleration);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	public void HandleSeat()
	{
		var newSeat = floatMachine.GetSeat();
		if (newSeat is Seat && Input.IsActionJustPressed("equip") && movementState != MovementState.seated && !newSeat.occupied)
		{
			seat = newSeat;
			seat.Rpc(nameof(seat.Sit), int.Parse(Name));
			movementState = MovementState.seated;
		}
		else if (Input.IsActionJustPressed("equip") && movementState == MovementState.seated)
		{
			seat.Rpc(nameof(seat.Stand));
			seat = null;
			GlobalRotation = new Vector3(0, GlobalRotation.Y, 0);
			movementState = MovementState.idle;
		}
	}

	public void MovePlayer(Vector3 position, Vector3 rotation)
	{
		GlobalPosition = position;
		GlobalRotation = rotation;
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


	public void PickItem()
	{
		// Pick and drop item
		if (Input.IsActionJustPressed("leftClick"))
		{
			if (interaction.GetCollider() is Item item && PickedItem is null && item.playerHolding == 0)
			{
				PickedItem = item;
				hand.GlobalPosition = item.GlobalPosition;
				staticBody.GlobalBasis = PickedItem.GlobalBasis;
			}
			else if (interaction.GetCollider() is Bone bone && PickedItem is null && bone.playerHolding == 0)
			{
				PickedItem = bone;
				hand.GlobalPosition = bone.GlobalPosition;
				staticBody.GlobalBasis = PickedItem.GlobalBasis;
			}
			else if (PickedItem is not null)
			{
				DropPickedItem();
			}
		}
		// Throw item
		else if (Input.IsActionJustPressed("rightClick") && PickedItem is not null)
		{
			Vector3 throwDirection = (hand.GlobalPosition - camera.GlobalPosition).Normalized();
			PickedItem.Rpc("Throw", throwDirection, Strength);
			PickedItem = null;
		}
		// Drop item if forced into a wall
		else if (PickedItem is Item && (hand.GlobalPosition - PickedItem.GlobalPosition).Length() > 1 && PickedItem.IsColliding())
		{
			DropPickedItem();
		}
		else if (PickedItem is Bone && (hand.GlobalPosition - PickedItem.GlobalPosition).Length() > 1)
		{
			DropPickedItem();
		}

		// Move item
		if (PickedItem is not null)
		{
			staticBody.Position = hand.Position;
			PickedItem.Rpc("Move", hand.GlobalPosition, staticBody.GlobalBasis, Strength, int.Parse(Name));
		}

		// Move item closer and further
		if (Input.IsActionJustPressed("scrollDown") && hand.Position.Z < -1)
		{
			hand.Position = new Vector3(hand.Position.X, hand.Position.Y, hand.Position.Z + 0.1f);
		}
		else if (Input.IsActionJustPressed("scrollUp") && hand.Position.Z > -3)
		{
			hand.Position = new Vector3(hand.Position.X, hand.Position.Y, hand.Position.Z - 0.1f);
		}
	}

	public void DropPickedItem()
	{
		PickedItem.Rpc("Move", Vector3.Zero, Vector3.Zero, 0, 0);
		PickedItem = null;
	}
}
