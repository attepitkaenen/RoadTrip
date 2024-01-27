using System;
using System.Linq;
using System.Net.Http;
using Godot;

public partial class Player : CharacterBody3D
{
	GameManager gameManager;
	MenuHandler menuHandler;
	MultiplayerController multiplayerController;

	[ExportGroup("Required Nodes")]
	[Export] public Camera3D camera;
	[Export] public AnimationTree animationTree;
	[Export] public FloatMachine floatMachine;
	[Export] public Label3D nameTag;
	[Export] public Node3D head;

	[ExportGroup("Debug Nodes")]
	[Export] public Label stateLabel;
	[Export] public Label speedLabel;
	[Export] public ItemList playerList;
	[Export] public Control debugWindow;


	[ExportGroup("Movement properties")]
	[Export] public float sensitivity = 0.001f;
	[Export] public float acceleration = 4f;
	[Export] public float deceleration = 5f;
	[Export] public float speed = 3f;
	[Export] public float jumpVelocity = 4.5f;
	[Export] public float runningMultiplier = 1.5f;
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
	public Vector3 syncRotation;
	public Basis syncBasis;

	public Vector3 spawnLocation = new Vector3(0, 3, 0);
	public PlayerState playerState;


	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public override void _EnterTree()
	{
		gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
		menuHandler = GetTree().Root.GetNode<MenuHandler>("MenuHandler");
		multiplayerController = GetTree().Root.GetNode<MultiplayerController>("MultiplayerController");

		SetMultiplayerAuthority(int.Parse(Name));

		if (!IsMultiplayerAuthority())
		{
			camera.Current = false;
			return;
		}

		menuHandler.OpenMenu(MenuHandler.MenuType.none);
		GetNode<MeshInstance3D>("characterAnimated/Armature/Skeleton3D/Head").Hide();
		GetNode<MeshInstance3D>("characterAnimated/Armature/Skeleton3D/Eyes").Hide();
		GetNode<MeshInstance3D>("characterAnimated/Armature/Skeleton3D/Nose").Hide();
		nameTag.Visible = false;
		camera.Current = true;
	}

	public override void _Input(InputEvent @event)
	{
		if (!IsMultiplayerAuthority() || menuHandler.currentMenu != MenuHandler.MenuType.none) return;

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
			cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(-85f), Mathf.DegToRad(85f)); camera.Rotation = cameraRotation;

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

	public void HandleDebugLines()
	{
		if (Input.IsActionJustPressed("debug"))
		{
			debugWindow.Visible = !debugWindow.Visible;
		}

		float currentSpeed = new Vector2(Velocity.X, Velocity.Z).Length();

		stateLabel.Text = movementState.ToString();
		speedLabel.Text = Math.Round(currentSpeed, 2).ToString();

		playerList.Clear();
		while (playerList.ItemCount < gameManager.GetPlayerStates().Count)
		{
			playerList.AddItem("");
		}
		var index = 0;
		gameManager.GetPlayerStates().ForEach(player =>
		{
			playerList.SetItemText(index, player.Name);
			index++;
		});
	}

	public override void _PhysicsProcess(double delta)
	{
		if (gameManager is not null)
		{
			playerState = gameManager.GetPlayerStates().Find(x => x.Id == int.Parse(Name));
			sensitivity = gameManager.Sensitivity;
			nameTag.Text = playerState.Name;
		}

		if (movementState == MovementState.seated && !IsMultiplayerAuthority()) return;

		//Lerp movement for other players
		if (!IsMultiplayerAuthority())
		{
			var newState = Transform;
			newState.Origin = GlobalPosition.Lerp(syncPosition, 0.9f);
			var a = newState.Basis.GetRotationQuaternion().Normalized();
			var b = syncBasis.GetRotationQuaternion().Normalized();
			var c = a.Slerp(b, 0.5f);
			newState.Basis = new Basis(c);
			Transform = newState;
			return;
		};

		// Movement logic
		Vector3 velocity = Velocity;

		// Gravity
		velocity.Y -= gravity * (float)delta;

		// sync properties to lerp movement for other players
		syncPosition = GlobalPosition;
		syncRotation = GlobalRotation;
		syncBasis = GlobalBasis;

		Rpc(nameof(HandleRpcAnimations), Input.IsActionJustPressed("jump"), isGrounded, Velocity, Transform);

		float correctedSpeed = speed * floatMachine.GetCrouchHeight();
		float currentSpeed = new Vector2(Velocity.X, Velocity.Z).Length();

		PickItem();

		FlipCar();

		HandleSeat();

		HandleDebugLines();

		// Stop movement if seated
		if (movementState == MovementState.seated)
		{
			Velocity = Vector3.Zero;
			if (seat is Seat && seat.isDriverSeat)
			{
				Vehicle vehicle = seat.GetParent<Vehicle>();
				vehicle.Rpc(nameof(vehicle.Drive), Input.GetVector("left", "right", "up", "down"), Input.IsActionPressed("jump"), delta);
			}
			return;
		}


		// Makes head and body same rotation when not seated
		head.GlobalRotation = GlobalRotation;


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

		// Handle crouch height seperately so that you can crouch while airborne
		if (Input.IsActionPressed("crouch"))
		{
			floatMachine.SetFloatOffset(0.2f);
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
				correctedSpeed *= runningMultiplier;
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

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
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

	public void FlipCar()
	{
		if (Input.IsActionPressed("leftClick") && PickedItem is null)
		{
			if (interaction.GetCollider() is Vehicle vehicle)
			{
				var axis = -head.GlobalBasis.X;
				vehicle.Rpc(nameof(vehicle.Flip), axis);
			}
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
