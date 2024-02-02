using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Godot;

public partial class Player : CharacterBody3D
{
	public long Id;
	GameManager gameManager;
	MenuHandler menuHandler;
	MultiplayerController multiplayerController;
	MultiplayerSynchronizer multiplayerSynchronizer;

	[ExportGroup("Required Nodes")]
	[Export] private Camera3D camera;
	[Export] private AnimationTree animationTree;
	[Export] private FloatMachine floatMachine;
	[Export] private Label3D nameTag;
	[Export] private Node3D head;
	[Export] private PackedScene ragdollScene;
	[Export] private CollisionShape3D collisionShape3D;

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
	[Export] private RayCast3D interaction;
	[Export] private Marker3D hand;
	[Export] private Marker3D equip;
	[Export] private StaticBody3D staticBody;
	private dynamic PickedItem;
	private ItemResource itemResource;
	private HeldItem heldItem;
	private float Strength = 40f;
	private int Health = 10;

	Seat seat;
	Ragdoll ragdoll;

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
		multiplayerSynchronizer = GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer");

		SetMultiplayerAuthority(int.Parse(Name));

		if (!IsMultiplayerAuthority())
		{
			camera.Current = false;
			return;
		}

		menuHandler.OpenMenu(MenuHandler.MenuType.none);
		GetNode<MeshInstance3D>("character/Armature/Skeleton3D/Head/Head").Hide();
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
		foreach (var (id, playerState) in gameManager.GetPlayerStates())
		{
			playerList.SetItemText(index, playerState.Name);
			index++;
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SetPlayerState(long id, string name)
	{
		nameTag.Text = name;
		Id = id;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (movementState == MovementState.seated && !IsMultiplayerAuthority()) return;

		if (movementState == MovementState.unconscious)
		{
			Visible = false;
			collisionShape3D.Disabled = true;
		}
		else
		{
			Visible = true;
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
				if (ragdoll is not null)
				{
					GlobalPosition = ragdoll.GetUpPosition();
					ragdoll.SwitchCamera();
					ragdoll.Rpc(nameof(ragdoll.Destroy));
				}
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
		syncRotation = GlobalRotation;
		syncBasis = GlobalBasis;

		Rpc(nameof(HandleRpcAnimations), movementState.ToString(), isGrounded, Velocity, Transform);

		float correctedSpeed = speed * floatMachine.GetCrouchHeight();
		float currentSpeed = new Vector2(Velocity.X, Velocity.Z).Length();

		HandleItem();

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
				vehicle.Rpc(nameof(vehicle.Drive), Input.GetActionStrength("left") - Input.GetActionStrength("right"), Input.GetActionStrength("up") - Input.GetActionStrength("down"), Input.IsActionPressed("jump"), delta);
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
	public void HandleRpcAnimations(string state, bool isGroundedRpc, Vector3 velocity, Transform3D transform)
	{
		if (state == "seated")
		{
			animationTree.Set("parameters/conditions/sit", true);
			animationTree.Set("parameters/conditions/walk", false);
			animationTree.Set("parameters/conditions/jump", false);
			return;
		}
		else if (state == "jumping" || !isGroundedRpc)
		{
			animationTree.Set("parameters/conditions/jump", true);
			animationTree.Set("parameters/conditions/sit", false);
			animationTree.Set("parameters/conditions/walk", false);
		}
		else
		{
			velocity *= transform.Basis;
			animationTree.Set("parameters/conditions/walk", true);
			animationTree.Set("parameters/Walk/blend_position", new Vector2(-(velocity.Z / (speed * 2)), velocity.X / (speed * 2)));
			animationTree.Set("parameters/conditions/sit", false);
			animationTree.Set("parameters/conditions/jump", false);

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

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void Hit(int damage, Vector3 bulletForce)
	{
		GD.Print($"Player {Name} was hit for {damage}");
		Health -= damage;

		if (Health <= 0)
		{
			Rpc(nameof(SpawnRagdoll), int.Parse(Name), Velocity + bulletForce);
			if (movementState == MovementState.seated)
			{
				seat.Rpc(nameof(seat.Stand));
				seat = null;
				GlobalRotation = new Vector3(0, GlobalRotation.Y, 0);
			}
			movementState = MovementState.unconscious;
			GD.Print($"Spawn ragdoll for {Name}");
			Visible = false;
			collisionShape3D.Disabled = true;
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SpawnRagdoll(int playerId, Vector3 velocity)
	{
		if (Multiplayer.IsServer())
		{
			var ragdoll = ragdollScene.Instantiate<Ragdoll>();
			ragdoll.MoveRagdoll(new Vector3(GlobalPosition.X, GlobalPosition.Y - 1.2f, GlobalPosition.Z), GlobalRotation, velocity);
			GetParent().AddChild(ragdoll, true);
			ragdoll.playerId = playerId;

			RpcId(playerId, nameof(SetRagdoll), ragdoll.GetPath());
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void SetRagdoll(string ragdollPath)
	{
		ragdoll = GetNode<Ragdoll>(ragdollPath);
		ragdoll.SwitchCamera();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void SetPickedItem(string itemPath)
	{
		GD.Print($"Should hold {itemPath}");
		PickedItem = GetTree().Root.GetNode<Item>(itemPath);
	}

	public void HandleItem()
	{
		// Equip held item
		if (Input.IsActionJustPressed("equip") && PickedItem is Item && heldItem is null)
		{
			GD.Print("Item picked");
			itemResource = gameManager.GetItemResource(PickedItem.ItemId);
			if (itemResource.Equippable)
			{
				gameManager.Rpc(nameof(gameManager.HoldItem), Id, itemResource.ItemId, equip.GetPath());
				gameManager.Rpc(nameof(gameManager.DestroyItem), PickedItem.GetPath());
				heldItem = equip.GetChild<HeldItem>(0);
				PickedItem = null;
			}
		}
		// Drop held item
		else if (Input.IsActionJustPressed("equip") && PickedItem is null && heldItem is not null && itemResource is not null)
		{
			gameManager.RpcId(1, nameof(gameManager.DropItem), int.Parse(Name), itemResource.ItemId, hand.GlobalPosition);
			gameManager.Rpc(nameof(gameManager.DestroyItem), heldItem.GetPath());
			heldItem = null;
			equip.GetChild(0).QueueFree();

		}

		// Stop picking items when item held
		if (heldItem is not null) return;

		// Pick and drop item
		if (Input.IsActionJustPressed("leftClick"))
		{
			if (interaction.GetCollider() is Item item && PickedItem is null && item.playerHolding == 0)
			{
				PickItem(item);
			}
			else if (interaction.GetCollider() is Bone bone && PickedItem is null && bone.playerHolding == 0)
			{
				PickItem(bone);
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
		PickedItem.Rpc(nameof(PickedItem.Drop));
		PickedItem = null;
	}

	public void PickItem(dynamic item)
	{
		PickedItem = item;
		hand.GlobalPosition = item.GlobalPosition;
		staticBody.GlobalBasis = PickedItem.GlobalBasis;
	}
}
