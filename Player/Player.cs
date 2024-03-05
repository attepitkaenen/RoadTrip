using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Godot;

public partial class Player : CharacterBody3D
{
	public long Id;
	public string userName;
	GameManager gameManager;
	MenuHandler menuHandler;
	MultiplayerSynchronizer multiplayerSynchronizer;

	[ExportGroup("Required Nodes")]
	[Export] public PlayerInteraction playerInteraction;
	[Export] private Camera3D camera;
	[Export] private AnimationTree animationTree;
	[Export] private FloatMachine floatMachine;
	[Export] private Label3D nameTag;
	[Export] public CollisionShape3D collisionShape3D;
	[Export] Ragdoll ragdoll;


	[ExportGroup("Debug Nodes")]
	[Export] private Label stateLabel;
	[Export] private Label speedLabel;
	[Export] private ItemList playerList;
	[Export] private Control debugWindow;


	[ExportGroup("Movement properties")]
	[Export] public float sensitivity = 0.001f;
	[Export] private float acceleration = 4f;
	[Export] private float deceleration = 5f;
	[Export] private float speed = 3f;
	[Export] private float jumpVelocity = 4.5f;
	[Export] private float runningMultiplier = 1.5f;
	public bool isGrounded { get; set; } = false;




	public float strength = 20f;
	private int Health = 10;

	Seat seat;

	public MovementState movementState { get; set; } = MovementState.idle;
	public enum MovementState
	{
		idle,
		walking,
		running,
		crouching,
		jumping,
		seated,
		falling,
		unconscious
	}

	// Sync properties
	public Vector3 syncPosition;
	// public Vector3 syncRotation;
	public Basis syncBasis;

	public Vector3 spawnLocation = new Vector3(0, 3, 0);


	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public override void _EnterTree()
	{
		gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
		menuHandler = GetTree().Root.GetNode<MenuHandler>("MenuHandler");
		multiplayerSynchronizer = GetNode<MultiplayerSynchronizer>("PlayerSynchronizer");

		SetMultiplayerAuthority(int.Parse(Name));

		if (!IsMultiplayerAuthority())
		{
			camera.Current = false;
			return;
		}

		menuHandler.OpenMenu(MenuHandler.MenuType.none);
		nameTag.Visible = false;
		camera.Current = true;
		sensitivity = gameManager.Sensitivity;
	}

	public override void _Ready()
	{
		if (GetMultiplayerAuthority() == Id)
		{
			menuHandler.OpenMenu(MenuHandler.MenuType.none);
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (!IsMultiplayerAuthority() || menuHandler.currentMenu != MenuHandler.MenuType.none) return;

		if (@event is InputEventMouseMotion && movementState != MovementState.unconscious)
		{
			if (!playerInteraction.IsMovingItem() || !Input.IsActionPressed("interact"))
			{
				InputEventMouseMotion mouseMotion = @event as InputEventMouseMotion;
				var rotationPoint = playerInteraction.GetNode<Node3D>("RotationPoint");
				rotationPoint.RotateX(-mouseMotion.Relative.Y * sensitivity);

				Vector3 rotationPointRotation = rotationPoint.Rotation;
				rotationPointRotation.X = Mathf.Clamp(rotationPointRotation.X, Mathf.DegToRad(-85f), Mathf.DegToRad(85f));
				rotationPoint.Rotation = rotationPointRotation;

				if (movementState == MovementState.seated)
				{
					playerInteraction.RotateY(-mouseMotion.Relative.X * sensitivity);
				}
				else
				{
					RotateY(-mouseMotion.Relative.X * sensitivity);
				}
			}
		}
	}


	public override void _PhysicsProcess(double delta)
	{
		if (!IsMultiplayerAuthority()) return;

		Rpc(nameof(HandleRpcAnimations), movementState.ToString(), isGrounded, Velocity, GlobalBasis, playerInteraction.IsHolding());
	}

	public override void _Process(double delta)
	{
		if (movementState == MovementState.seated && !IsMultiplayerAuthority()) return;

		if (movementState == MovementState.unconscious)
		{
			collisionShape3D.Disabled = true;
		}
		else
		{
			collisionShape3D.Disabled = false;
		}

		//Lerp movement for other players
		if (!IsMultiplayerAuthority())
		{
			var newState = Transform;
			newState.Origin = GlobalPosition.Lerp(syncPosition, 0.2f);
			var a = newState.Basis.GetRotationQuaternion().Normalized();
			var b = syncBasis.GetRotationQuaternion().Normalized();
			var c = a.Slerp(b, 0.5f);
			newState.Basis = new Basis(c);
			Transform = newState;
			return;
		};

		if (movementState == MovementState.unconscious)
		{
			camera.Current = false;
			if (Input.IsActionJustPressed("jump"))
			{
				Health = 10;
				movementState = MovementState.idle;
				GlobalPosition = ragdoll.GetUpPosition();
				ragdoll.Rpc(nameof(ragdoll.Deactivate));
			}
			return;
		}
		else
		{
			camera.Current = true;
		}

		// Movement logic
		Vector3 velocity = Velocity;

		// Gravity
		velocity.Y -= gravity * (float)delta;

		// sync properties to lerp movement for other players
		syncPosition = GlobalPosition;
		// syncRotation = GlobalRotation;
		syncBasis = GlobalBasis;

		float correctedSpeed = speed * floatMachine.GetCrouchHeight();
		float currentSpeed = new Vector2(Velocity.X, Velocity.Z).Length();

		HandleSeat();

		HandleDebugLines();

		// Stop movement if seated and handle driving
		if (movementState == MovementState.seated)
		{
			Velocity = Vector3.Zero;
			if (seat is Seat && seat.isDriverSeat)
			{
				Vehicle vehicle = seat.GetParent<Vehicle>();
				vehicle.RpcId(1, nameof(vehicle.Drive), Input.GetActionStrength("left") - Input.GetActionStrength("right"), Input.GetActionStrength("up") - Input.GetActionStrength("down"), Input.IsActionPressed("jump"), delta);
			}
			return;
		}


		// Makes head and body same rotation when not seated
		playerInteraction.GlobalRotation = GlobalRotation;

		// Handle movementState changes
		if (Health <= 0)
		{
			movementState = MovementState.unconscious;
		}
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
		foreach (var (id, playerState) in gameManager.GetPlayerStates())
		{
			playerList.SetItemText(index, $"{playerState.Name} : {playerState.Id}");
			index++;
		}
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

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SetPlayerState(long id, string name)
	{
		nameTag.Text = name;
		userName = name;
		Id = id;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void MovePlayer(Vector3 position, Vector3 rotation)
	{
		GlobalPosition = position;
		GlobalRotation = rotation;
	}

	[Rpc(CallLocal = true)]
	public void HandleRpcAnimations(string state, bool isGroundedRpc, Vector3 velocity, Basis basis, bool isHolding)
	{
		animationTree.Set("parameters/HoldingBlend/blend_amount", isHolding);

		if (state == "seated")
		{
			animationTree.Set("parameters/StateMachine/conditions/walk", false);
			animationTree.Set("parameters/StateMachine/conditions/jump", false);
			animationTree.Set("parameters/StateMachine/conditions/sit", true);
			return;
		}
		else if (state == "jumping" || !isGroundedRpc)
		{
			animationTree.Set("parameters/StateMachine/conditions/walk", false);
			animationTree.Set("parameters/StateMachine/conditions/sit", false);
			animationTree.Set("parameters/StateMachine/conditions/jump", true);
		}
		else
		{
			velocity *= basis;
			Vector2 walkingIntensity = new Vector2(-(velocity.Z / (speed * 2)), velocity.X / (speed * 2));
			animationTree.Set("parameters/StateMachine/conditions/walk", true);
			animationTree.Set("parameters/StateMachine/walking/blend_position", walkingIntensity);
			animationTree.Set("parameters/StateMachine/conditions/sit", false);
			animationTree.Set("parameters/StateMachine/conditions/jump", false);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void Hit(int damage, string boneName, Vector3 bulletDirection)
	{
		if (!IsMultiplayerAuthority()) return;
		GD.Print($"Player {Name} was hit for {damage}");
		Health -= damage;

		if (Health <= 0)
		{
			movementState = MovementState.unconscious;

			if (playerInteraction.IsHolding())
			{
				playerInteraction.DropHeldItem();
			}

			// Stop sitting if seated
			if (movementState == MovementState.seated)
			{
				seat.Rpc(nameof(seat.Stand));
				seat = null;
				GlobalRotation = new Vector3(0, GlobalRotation.Y, 0);
			}
			ragdoll.Rpc(nameof(ragdoll.Activate), boneName, bulletDirection);
			collisionShape3D.Disabled = true;
		}
	}
}
