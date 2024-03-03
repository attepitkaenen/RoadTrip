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
	[Export] private Camera3D camera;
	[Export] private AnimationTree animationTree;
	[Export] private FloatMachine floatMachine;
	[Export] private Label3D nameTag;
	[Export] private Node3D head;
	[Export] public CollisionShape3D collisionShape3D;
	[Export] Ragdoll ragdoll;


	[ExportGroup("Debug Nodes")]
	[Export] private Label stateLabel;
	[Export] private Label speedLabel;
	[Export] private ItemList playerList;
	[Export] private Control debugWindow;


	[ExportGroup("Movement properties")]
	[Export] private float sensitivity = 0.001f;
	[Export] private float acceleration = 4f;
	[Export] private float deceleration = 5f;
	[Export] private float speed = 3f;
	[Export] private float jumpVelocity = 4.5f;
	[Export] private float runningMultiplier = 1.5f;
	public bool isGrounded { get; set; } = false;

	[ExportGroup("Interaction properties")]
	[Export] private RayCast3D interactionCast;
	[Export] private Marker3D hand;
	[Export] private Marker3D equip;
	[Export] private StaticBody3D staticBody;
	[Export] private Node3D EquipHandPosition;
	private dynamic PickedItem;
	private ItemResource itemResource;
	private int _heldItemId;
	private HeldItem _heldItem;
	private float Strength = 20f;
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

		if (@event is InputEventMouseMotion)
		{
			if (PickedItem is not null && Input.IsActionPressed("interact"))
			{
				InputEventMouseMotion mouseMotion = @event as InputEventMouseMotion;
				staticBody.RotateY(mouseMotion.Relative.X * sensitivity);
				staticBody.RotateX(mouseMotion.Relative.Y * sensitivity);
			}
			else
			{
				InputEventMouseMotion mouseMotion = @event as InputEventMouseMotion;
				var rotationPoint = head.GetNode<Node3D>("RotationPoint");
				rotationPoint.RotateX(-mouseMotion.Relative.Y * sensitivity);

				Vector3 rotationPointRotation = rotationPoint.Rotation;
				rotationPointRotation.X = Mathf.Clamp(rotationPointRotation.X, Mathf.DegToRad(-85f), Mathf.DegToRad(85f));
				rotationPoint.Rotation = rotationPointRotation;

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
	}


	public override void _PhysicsProcess(double delta)
	{
		if (!IsMultiplayerAuthority()) return;

		Rpc(nameof(HandleRpcAnimations), movementState.ToString(), isGrounded, Velocity, GlobalBasis, IsHolding());
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

		HandleHeldItem();

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

		HandleItem();

		HandleInteraction();

		FlipCar();

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
		head.GlobalRotation = GlobalRotation;

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

	public bool IsHolding()
	{
		return _heldItemId != 0;
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

	public void FlipCar()
	{
		if (Input.IsActionPressed("leftClick") && PickedItem is null)
		{
			if (interactionCast.GetCollider() is Vehicle vehicle)
			{
				var axis = -head.GlobalBasis.X;
				vehicle.Rpc(nameof(vehicle.Flip), axis);
			}
		}
	}

	public void HandleHeldItem()
	{
		if (IsHolding() && _heldItem is null)
		{
			HeldItem item = gameManager.GetItemResource(_heldItemId).ItemInHand.Instantiate() as HeldItem;
			if (item.holdType == 0)
			{
				EquipHandPosition.Position = new Vector3(0.274f, -0.175f, -0.357f);
				EquipHandPosition.Rotation = Vector3.Zero;
			}
			else if (item.holdType == 1)
			{
				EquipHandPosition.Position = new Vector3(0.274f, -0.211f, -0.357f);
				EquipHandPosition.RotationDegrees = new Vector3(0, 0, 90);
			}
			item.SetMultiplayerAuthority((int)Id);
			equip.AddChild(item);
			_heldItem = item;
		}
		else if (_heldItem is not null && !IsHolding())
		{
			_heldItem = null;
			equip.GetChild(0).QueueFree();
		}
	}

	public void HandleInteraction()
	{
		if (PickedItem is null && _heldItem is null && Input.IsActionJustPressed("leftClick") && interactionCast.GetCollider() is Interactable interactable)
		{
			interactable.Rpc(nameof(interactable.Press));
		}
	}

	public void HandleItem()
	{
		// Install vehicle part if holding a toolbox
		if (Input.IsActionJustPressed("rightClick") && PickedItem is Installable part && part.canBeInstalled && _heldItem is Toolbox)
		{
			PickedItem = null;
			part.Install();
			GD.Print("INSTALL PART");
		}
		else if (Input.IsActionJustPressed("rightClick") && interactionCast.GetCollider() is CarPart installedPart && _heldItem is Toolbox && interactionCast.IsColliding() && PickedItem is null)
		{
			installedPart.Uninstall();
		}
		else if (Input.IsActionJustPressed("rightClick") && interactionCast.GetCollider() is Door installedDoor && _heldItem is Toolbox && interactionCast.IsColliding() && PickedItem is null)
		{
			installedDoor.Uninstall();
		}

		// Equip held item
		else if (Input.IsActionJustPressed("equip") && PickedItem is Item && _heldItem is null)
		{
			GD.Print("Item picked");
			itemResource = gameManager.GetItemResource(PickedItem.ItemId);
			if (itemResource.Equippable)
			{
				SetHeldItem(itemResource.ItemId);
				PickedItem.RpcId(1, nameof(PickedItem.QueueItemDestruction));
				PickedItem = null;
			}
		}
		// Drop held item
		else if (Input.IsActionJustPressed("equip") && PickedItem is null && _heldItem is not null && itemResource is not null)
		{
			DropHeldItem();
		}

		// Stop picking items when item held
		if (_heldItem is not Toolbox && _heldItem is not null) return;

		// Pick and drop item
		if (Input.IsActionJustPressed("leftClick"))
		{
			dynamic collider = interactionCast.GetCollider();
			if ((collider is Item || collider is Door || collider is Bone) && PickedItem is null && collider.playerHolding == 0)
			{
				PickItem(collider);
			}
			else if (PickedItem is not null)
			{
				DropPickedItem();
			}
		}

		// Throw item
		else if (Input.IsActionJustPressed("rightClick") && PickedItem is not null && _heldItem is null && PickedItem is not Door)
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
		else if ((PickedItem is Bone || PickedItem is Door) && (hand.GlobalPosition - PickedItem.GlobalPosition).Length() > 0.9f)
		{
			DropPickedItem();
		}


		// Move item
		if (PickedItem is not null)
		{
			staticBody.Position = hand.Position;
			PickedItem.Rpc("Move", hand.GlobalPosition, staticBody.GlobalBasis, int.Parse(Name));
		}

		// Move item closer and further
		if (Input.IsActionJustPressed("scrollDown") && hand.Position.Z < -0.5f)
		{
			hand.Position = new Vector3(hand.Position.X, hand.Position.Y, hand.Position.Z + 0.1f);
		}
		else if (Input.IsActionJustPressed("scrollUp") && hand.Position.Z > -2)
		{
			hand.Position = new Vector3(hand.Position.X, hand.Position.Y, hand.Position.Z - 0.1f);
		}
	}

	public void DropHeldItem()
	{
		gameManager.RpcId(1, nameof(gameManager.DropItem), Id, itemResource.ItemId, hand.GlobalPosition);
		SetHeldItem(0);
	}

	public void DropPickedItem()
	{
		PickedItem.Rpc(nameof(PickedItem.Drop));
		PickedItem = null;
		hand.Position = new Vector3(0, 0, -1);
	}

	public void PickItem(dynamic item)
	{
		PickedItem = item;
		hand.GlobalPosition = item.GlobalPosition;
		staticBody.GlobalBasis = PickedItem.GlobalBasis;
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

			if (IsHolding())
			{
				DropHeldItem();
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

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void SetPickedItem(string itemPath)
	{
		GD.Print($"Should pickup {itemPath}");
		PickedItem = GetTree().Root.GetNode<Item>(itemPath);
	}

	public void SetHeldItem(int itemId)
	{
		_heldItemId = itemId;
	}
}
