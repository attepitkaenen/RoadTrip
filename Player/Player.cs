using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Godot;
using Riptide;

public partial class Player : CharacterBody3D
{
    public ushort id { get; private set; }
    public bool isLocal { get; private set; } = false;
    public string userName { get; private set; }
    GameManager gameManager;
    MenuHandler menuHandler;
    RiptideClient riptideClient;
    RiptideServer riptideServer;
    public StateHandler stateHandler;

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
    private Label customLabel = new Label();

    [ExportGroup("Movement properties")]
    [Export] public float sensitivity = 0.001f;
    [Export] private float acceleration = 4f;
    [Export] private float deceleration = 5f;
    [Export] public float speed = 3f;
    [Export] private float jumpVelocity = 4.5f;
    [Export] private float runningMultiplier = 1.5f;
    public bool isGrounded { get; set; } = false;


    public float strength = 20f;
    public int Health { get; private set; } = 10;

    Seat _seat;


    // Sync properties
    public Vector3 syncVelocity;
    private Vector3 _syncPosition;
    // public Vector3 syncRotation;
    private Vector3 _syncRotation;
    private Vector3 _syncHeadRotation;

    public Vector3 spawnLocation = new Vector3(0, 3, 0);




    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    private bool[] inputs = new bool[7];
    private Vector3 proxyGlobalRotation;
    private bool didTeleport;

    public void SetPlayerProperties(ushort id, string userName, bool isLocal)
    {
        this.id = id;
        this.userName = userName;
        this.isLocal = isLocal;
    }

    public override void _EnterTree()
    {
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
        menuHandler = GetTree().Root.GetNode<MenuHandler>("MenuHandler");
        riptideClient = GetTree().Root.GetNode<RiptideClient>("RiptideClient");
        riptideServer = GetTree().Root.GetNode<RiptideServer>("RiptideServer");
        // interpolator = GetNode<Interpolator>("Interpolator");


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
        AddDebugLine(customLabel);

        nameTag.Text = userName;

        rotationPoint = playerInteraction.GetNode<Node3D>("RotationPoint");
        stateHandler = GetNode<StateHandler>("StateHandler");
    }

    public override void _Input(InputEvent @event)
    {
        if (!isLocal || MenuHandler.currentMenu != MenuHandler.MenuType.none) return;

        if (@event is InputEventMouseMotion && stateHandler.movementState != StateHandler.MovementState.unconscious)
        {
            if (!playerInteraction.IsMovingItem() || !Input.IsActionPressed("interact"))
            {
                InputEventMouseMotion mouseMotion = @event as InputEventMouseMotion;

                rotationPoint.RotateX(-mouseMotion.Relative.Y * sensitivity);

                Vector3 rotationPointRotation = rotationPoint.Rotation;
                rotationPointRotation.X = Mathf.Clamp(rotationPointRotation.X, Mathf.DegToRad(-85f), Mathf.DegToRad(85f));
                rotationPoint.Rotation = rotationPointRotation;

                if (stateHandler.movementState == StateHandler.MovementState.seated)
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

        if (isLocal)
        {
            HandleDebugUpdate(delta);

            // Movement logic
            Vector3 velocity = Velocity;

            // Gravity
            velocity.Y -= gravity * (float)delta;

            float correctedSpeed = speed * floatMachine.GetCrouchHeight();

            if (Input.IsActionPressed("crouch") && !Input.IsActionPressed("sprint"))
            {
                floatMachine.SetFloatOffset(0.2f);
            }

            if (Input.IsActionPressed("sprint") && !Input.IsActionPressed("crouch") && isGrounded)
            {
                correctedSpeed *= 1.5f;
            }

            if (!Input.IsActionPressed("crouch"))
            {
                floatMachine.SetFloatOffset(0.7f);
            }

            if (Input.IsActionJustPressed("jump") && isGrounded)
            {
                velocity.Y = jumpVelocity;
            }

            // Get the input direction and handle the movement/deceleration.

            Vector2 inputDir = Input.GetVector("left", "right", "up", "down"); ;
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
        else
        {
            if (stateHandler.movementState == StateHandler.MovementState.seated)
            {
                Velocity = Vector3.Zero;
                return;
            }

		    GlobalPosition = GlobalPosition.Lerp(_syncPosition, 0.2f);
			GlobalRotation = new Vector3(0, Mathf.LerpAngle(GlobalRotation.Y, _syncRotation.Y, 0.2f), 0);
			rotationPoint.Rotation = new Vector3(Mathf.LerpAngle(rotationPoint.Rotation.X, _syncHeadRotation.X, 0.2f), 0, 0);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (RiptideServer.IsServerRunning() && isLocal)
        {
            syncVelocity = Velocity;
            _syncPosition = GlobalPosition;
            _syncRotation = GlobalRotation;
            _syncHeadRotation = rotationPoint.Rotation;
        }

        if (RiptideServer.IsServerRunning())
        {
            if (riptideServer.currentTick % 2 != 0) return;
            SendServerMovement();
        }
        else if (isLocal)
        {
            if (RiptideClient.ServerTick % 2 != 0) return;
            SendPlayerMovement();
        }
    }

    public void HandleMovementUpdate(Vector3 velocity, Vector3 position, Vector3 rotation, Vector3 headRotation)
    {
        // GD.Print($"Moving player: {id} on client: {RiptideClient.GetId()} to {position}");
        if (!isLocal || RiptideServer.IsServerRunning())
        {
            syncVelocity = velocity;
            _syncPosition = position;
            _syncRotation = rotation;
            _syncHeadRotation = headRotation;
        }
    }

    public void SendPlayerMovement()
    {
        Message message = Message.Create(MessageSendMode.Unreliable, ClientToServerId.movement);
        message.AddUShort((ushort)stateHandler.movementState);
        message.AddVector3(Velocity);
        message.AddVector3(GlobalPosition);
        message.AddVector3(GlobalRotation);
        message.AddVector3(rotationPoint.Rotation);
        riptideClient.SendMessage(message);
    }

    private void SendServerMovement()
    {
        Message message = Message.Create(MessageSendMode.Unreliable, ServerToClientId.playerMovement);
        message.AddUShort(id);
        message.AddUShort((ushort)stateHandler.movementState);
        message.AddVector3(syncVelocity);
        message.AddVector3(_syncPosition);
        message.AddVector3(_syncRotation);
        message.AddVector3(_syncHeadRotation);
        riptideServer.SendMessageToAll(message);
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

        movementStateDebug.Text = "Movement state: " + stateHandler.movementState.ToString();
        speedDebug.Text = "Velocity: " + Math.Round(currentSpeed, 2).ToString();
        fpsDebug.Text = "FPS: " + Math.Round(1.0f / delta);
        holdingItemDebug.Text = "Holding item: " + playerInteraction._heldItem;
        customLabel.Text = "IsGrounded: " + isGrounded;

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

            // movementState = MovementState.unconscious;
            ragdoll.Rpc(nameof(ragdoll.Activate), boneName, bulletDirection);
        }
    }
}
