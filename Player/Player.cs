using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Godot;
using Riptide;

public partial class Player : CharacterBody3D
{
	public ushort id;
	public bool isLocal = false;
	public string userName;
	GameManager gameManager;
	MenuHandler menuHandler;
	RiptideClient riptideClient;
	RiptideServer riptideServer;

	[ExportGroup("Required Nodes")]
	[Export] public PlayerInteraction playerInteraction;
	[Export] private Camera3D camera;
	[Export] private AnimationTree animationTree;
	[Export] private FloatMachine floatMachine;
	[Export] private Label3D nameTag;
	[Export] public CollisionShape3D collisionShape3D;
	[Export] Ragdoll ragdoll;
	public Node3D rotationPoint;


	[ExportGroup("Debug Nodes")]
	[Export] private VBoxContainer _debugContainer;
	[Export] private VBoxContainer _lobbyContainer;
	[Export] private Control debugWindow;
	private Label speedDebug = new Label();
	private Label fpsDebug = new Label();
	private Label movementStateDebug = new Label();
	private Label holdingItemDebug = new Label();


	[ExportGroup("Movement properties")]
	[Export] public float sensitivity = 0.001f;
	[Export] private float acceleration = 4f;
	[Export] private float deceleration = 5f;
	[Export] public float speed = 3f;
	[Export] private float jumpVelocity = 4.5f;
	[Export] private float runningMultiplier = 1.5f;
	public bool isGrounded { get; set; } = false;


	public float strength = 20f;
	private int Health = 10;

	Seat _seat;

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
	private bool[] inputs = new bool[7];
	private Vector3 proxyGlobalRotation;


	public override void _EnterTree()
	{
		gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
		menuHandler = GetTree().Root.GetNode<MenuHandler>("MenuHandler");
		riptideClient = GetTree().Root.GetNode<RiptideClient>("RiptideClient");
		riptideServer = GetTree().Root.GetNode<RiptideServer>("RiptideServer");


		if (!isLocal)
		{
			camera.Current = false;
			return;
		}

		camera.Current = true;
		sensitivity = gameManager.Sensitivity;
	}

	public override void _Ready()
	{
		AddDebugLine(fpsDebug);
		AddDebugLine(movementStateDebug);
		AddDebugLine(speedDebug);
		AddDebugLine(holdingItemDebug);

		nameTag.Text = userName;

		rotationPoint = playerInteraction.GetNode<Node3D>("RotationPoint");
	}

	public override void _Input(InputEvent @event)
	{
		if (!isLocal || MenuHandler.currentMenu != MenuHandler.MenuType.none) return;

		if (@event is InputEventMouseMotion && movementState != MovementState.unconscious)
		{
			if (!playerInteraction.IsMovingItem() || !Input.IsActionPressed("interact"))
			{
				InputEventMouseMotion mouseMotion = @event as InputEventMouseMotion;
				
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

	public override void _Process(double delta)
	{

		// if (movementState == MovementState.unconscious)
		// {
		// 	collisionShape3D.Disabled = true;
		// }
		// else
		// {
		// 	collisionShape3D.Disabled = false;
		// }

		// //Lerp movement for other players
		// if (!isLocal)
		// {
		// 	if (movementState == MovementState.seated)
		// 	{
		// 		Velocity = Vector3.Zero;
		// 		return;
		// 	}
		// 	var newState = Transform;
		// 	newState.Origin = GlobalPosition.Lerp(syncPosition, 0.2f);
		// 	var a = newState.Basis.GetRotationQuaternion().Normalized();
		// 	var b = syncBasis.GetRotationQuaternion().Normalized();
		// 	var c = a.Slerp(b, 0.5f);
		// 	newState.Basis = new Basis(c);
		// 	Transform = newState;
		// 	return;
		// };

		// if (movementState == MovementState.unconscious)
		// {
		// 	camera.Current = false;
		// 	if (Input.IsActionJustPressed("jump"))
		// 	{
		// 		Health = 10;
		// 		movementState = MovementState.idle;
		// 		GlobalPosition = ragdoll.GetUpPosition();
		// 		ragdoll.Rpc(nameof(ragdoll.Deactivate));
		// 	}
		// 	return;
		// }

		// camera.Current = true;


		// // sync properties to lerp movement for other players
		// syncPosition = GlobalPosition;
		// // syncRotation = GlobalRotation;
		// syncBasis = GlobalBasis;

		// float currentSpeed = new Vector2(Velocity.X, Velocity.Z).Length();

		// HandleSeat();

		// HandleDebugUpdate(delta);

		// // Stop movement if seated and handle driving
		// if (movementState == MovementState.seated)
		// {
		// 	Velocity = Vector3.Zero;
		// 	if (_seat.isDriverSeat)
		// 	{
		// 		Vehicle vehicle = _seat.GetVehicle();
		// 		vehicle.RpcId(1, nameof(vehicle.Drive), Input.GetActionStrength("left") - Input.GetActionStrength("right"), Input.GetActionStrength("up") - Input.GetActionStrength("down"), Input.IsActionPressed("jump"), delta);
		// 	}
		// 	return;
		// }


		// // Makes head and body same rotation when not seated
		// playerInteraction.GlobalRotation = GlobalRotation;

		// // Handle movementState changes
		// if (Health <= 0)
		// {
		// 	movementState = MovementState.unconscious;
		// }
		// if (Input.IsActionJustPressed("jump") && isGrounded)
		// {
		// 	velocity.Y = jumpVelocity;
		// 	movementState = MovementState.jumping;
		// }
		// else if (!isGrounded && Velocity.Y < 0)
		// {
		// 	movementState = MovementState.falling;
		// }
		// else if (Input.IsActionPressed("crouch") && isGrounded)
		// {
		// 	movementState = MovementState.crouching;
		// }
		// else if (Input.IsActionPressed("sprint") && floatMachine.GetCrouchHeight() > 0.8f && currentSpeed > 0.1f && isGrounded)
		// {
		// 	movementState = MovementState.running;
		// }
		// else if (currentSpeed > 0.1f && isGrounded)
		// {
		// 	movementState = MovementState.walking;
		// }
		// else if (isGrounded)
		// {
		// 	movementState = MovementState.idle;
		// }

		if (riptideServer.IsServerRunning())
		{
			ServerHandleMovement(delta);
		}
	}

	private void ServerHandleMovement(double delta)
	{
		// Movement logic
		Vector3 velocity = Velocity;

		// Gravity
		velocity.Y -= gravity * (float)delta;

		float correctedSpeed = speed * floatMachine.GetCrouchHeight();

		if (inputs[6] && !inputs[5])
		{
			floatMachine.SetFloatOffset(0.2f);
		}
		
		if (inputs[5] && !inputs[6] && isGrounded)
		{
			correctedSpeed *= 1.5f;
		}

		if (!inputs[6])
		{
			floatMachine.SetFloatOffset(0.7f);
		}

		if (inputs[4] && isGrounded)
		{
			velocity.Y = jumpVelocity;
		}

		// Get the input direction and handle the movement/deceleration.

		Vector2 inputDir = Vector2.Zero;

		if (inputs[0])
			inputDir.Y -= 1;
		if (inputs[1])
			inputDir.Y += 1;
		if (inputs[2])
			inputDir.X -= 1;
		if (inputs[3])
			inputDir.X += 1;


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

	public override void _PhysicsProcess(double delta)
	{
		SendMovement();
	}

	public void Move(Vector3 position, Vector3 rotation, Vector3 headRotation)
	{
		GlobalPosition = position;
		if (!isLocal && !riptideClient.IsHost())
		{
			GlobalRotation = rotation;
			rotationPoint.Rotation = headRotation;
		}
	}

	private void SendMovement()
	{
		Message message = Message.Create(MessageSendMode.Unreliable, ServerToClientId.playerMovement);
		message.AddUShort(id);
		message.AddVector3(GlobalPosition);
		message.AddVector3(GlobalRotation);
		message.AddVector3(rotationPoint.Rotation);
		riptideServer.SendMessageToAll(message);
	}

	public void SetInput(bool[] inputs, Vector3 globalRotation, Vector3 headRotation)
	{
		this.inputs = inputs;
		GlobalRotation = globalRotation;
		rotationPoint.Rotation = headRotation;
	}

	public void AddDebugLine(Label label)
	{
		_debugContainer.AddChild(label);
	}

	public void HandleDebugUpdate(double delta)
	{
		if (Input.IsActionJustPressed("debug"))
		{
			debugWindow.Visible = !debugWindow.Visible;
		}

		float currentSpeed = new Vector2(Velocity.X, Velocity.Z).Length();

		movementStateDebug.Text = "Movement state: " + movementState.ToString();
		speedDebug.Text = "Velocity: " + Math.Round(currentSpeed, 2).ToString();
		fpsDebug.Text = "FPS: " + Math.Round(1.0f / delta);
		holdingItemDebug.Text = "Holding item: " + playerInteraction._heldItem;

		// playerList.Clear();
		// while (playerList.ItemCount < gameManager.GetPlayerStates().Count)
		// {
		// 	playerList.AddItem("");
		// }
		// var index = 0;
		// foreach (var (id, playerState) in gameManager.GetPlayerStates())
		// {
		// 	playerList.SetItemText(index, $"{playerState.Name} : {playerState.Id}");
		// 	index++;
		// }
	}

	public void HandleSeat()
	{
		var newSeat = floatMachine.GetSeat();
		if (newSeat is Seat && Input.IsActionJustPressed("equip") && movementState != MovementState.seated && !newSeat.occupied)
		{
			_seat = newSeat;
			_seat.Rpc(nameof(_seat.Sit), id);
			movementState = MovementState.seated;
		}
		else if (Input.IsActionJustPressed("equip") && movementState == MovementState.seated)
		{
			_seat.Rpc(nameof(_seat.Stand));
			_seat = null;
			GlobalRotation = new Vector3(0, GlobalRotation.Y, 0);
			movementState = MovementState.idle;
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SetPlayerState(ushort id, string name)
	{
		userName = name;
		this.id = id;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	public void Hit(int damage, string boneName, Vector3 bulletDirection)
	{
		if (!isLocal) return;
		GD.Print($"Player {Name} was hit for {damage}");
		Health -= damage;

		if (Health <= 0)
		{
			if (playerInteraction.IsHolding())
			{
				playerInteraction.DropHeldItem(false);
			}

			// Stop sitting if seated
			if (_seat is not null)
			{
				_seat.Rpc(nameof(_seat.Stand));
				_seat = null;
				GlobalRotation = new Vector3(0, GlobalRotation.Y, 0);
			}

			movementState = MovementState.unconscious;
			ragdoll.Rpc(nameof(ragdoll.Activate), boneName, bulletDirection);
		}
	}
}
