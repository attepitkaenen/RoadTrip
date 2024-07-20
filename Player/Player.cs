using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Godot;

public partial class Player : CharacterBody3D
{
    public int id;
    public string userName;
    public bool isLocal = false;
    GameManager gameManager;
    MenuHandler menuHandler;
    MultiplayerSynchronizer multiplayerSynchronizer;
    PlayerManager playerManager;

    [ExportGroup("Required Nodes")]
    [Export] public PlayerInteraction playerInteraction;
    [Export] private Camera3D camera;
    [Export] private AnimationTree animationTree;
    [Export] private FloatMachine floatMachine;
    [Export] private Label3D nameTag;
    [Export] public CollisionShape3D collisionShape3D;
    [Export] Ragdoll ragdoll;
    Node3D rotationPoint;


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
    public Vector3 spawnPosition = Vector3.Zero;
    public bool isPlayerPrepared = false;

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
    private short Health = 100;
    public Vector3 syncVelocity;
    public Vector3 syncPosition;
    public float syncRotation;
    public float syncHeadRotation;
    private ushort syncMovementStateId;
    private ushort syncHeldItemId;


    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    public override void _EnterTree()
    {
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
        menuHandler = GetTree().Root.GetNode<MenuHandler>("MenuHandler");
        playerManager = GetTree().Root.GetNode<PlayerManager>("PlayerManager");
        multiplayerSynchronizer = GetNode<MultiplayerSynchronizer>("PlayerSynchronizer");
        rotationPoint = playerInteraction.GetNode<Node3D>("RotationPoint");
        camera.Current = false;
    }

    public override void _Ready()
    {
        AddDebugLine(fpsDebug);
        AddDebugLine(movementStateDebug);
        AddDebugLine(speedDebug);
        AddDebugLine(holdingItemDebug);
    }

    public override void _Input(InputEvent @event)
    {
        if (!isLocal || menuHandler.currentMenu != MenuHandler.MenuType.none) return;

        if (@event is InputEventMouseMotion && movementState != MovementState.unconscious)
        {
            if (!playerInteraction.IsMovingItem() || !Input.IsActionPressed("interact"))
            {
                InputEventMouseMotion mouseMotion = @event as InputEventMouseMotion;
                rotationPoint.RotateX(-mouseMotion.Relative.Y * sensitivity);

                Vector3 rotationPointRotation = rotationPoint.Rotation;
                rotationPointRotation.X = Mathf.Clamp(rotationPointRotation.X, Mathf.DegToRad(-85f), Mathf.DegToRad(85f));
                rotationPoint.Rotation = rotationPointRotation;

                // Sync head rotation
                syncHeadRotation = rotationPoint.Rotation.X;

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
        if (id == Multiplayer.GetUniqueId() && !isLocal)
        {
            GD.Print($"Local player of id: {id} is ready!");
            menuHandler.OpenMenu(MenuHandler.MenuType.none);
            isLocal = true;
            camera.Current = true;
            sensitivity = gameManager.Sensitivity;
            playerManager.RpcId(1, nameof(playerManager.LocalPlayerReady), id);
        }

        if (isLocal)
        {
            var rotation = (short)(GlobalRotation.Y * 100);
            var headRotation = (short)(rotationPoint.Rotation.X * 100);
            Rpc(nameof(SetSyncProperties), Velocity, GlobalPosition, rotation, headRotation, (ushort)movementState, 0);
        }
    }


    public override void _Process(double delta)
    {
        nameTag.Text = userName;

        if (movementState == MovementState.unconscious)
        {
            collisionShape3D.Disabled = true;
        }
        else
        {
            collisionShape3D.Disabled = false;
        }

        // Handle property syncing and Lerping for non local players
        if (!isLocal)
        {
            if (movementState == MovementState.seated)
            {
                Velocity = Vector3.Zero;
                return;
            }
            GlobalPosition = GlobalPosition.Lerp(syncPosition, 0.2f);
            GlobalRotation = new Vector3(0, Mathf.LerpAngle(GlobalRotation.Y, syncRotation, 0.2f), 0);
            rotationPoint.Rotation = new Vector3(Mathf.LerpAngle(rotationPoint.Rotation.X, syncHeadRotation, 0.2f), 0, 0);
            movementState = (MovementState)syncMovementStateId;
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

        camera.Current = true;

        // Movement logic
        Vector3 velocity = Velocity;

        // Gravity
        velocity.Y -= gravity * (float)delta;

        // sync properties to lerp movement for other players
        syncPosition = GlobalPosition;
        syncRotation = GlobalRotation.Y;

        float correctedSpeed = speed * floatMachine.GetCrouchHeight();
        float currentSpeed = new Vector2(Velocity.X, Velocity.Z).Length();

        HandleSeat();

        HandleDebugUpdate(delta);

        // Stop movement if seated and handle driving
        if (movementState == MovementState.seated)
        {
            Velocity = Vector3.Zero;
            if (_seat.isDriverSeat)
            {
                Vehicle vehicle = _seat.GetVehicle();
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
        syncVelocity = Velocity;
        MoveAndSlide();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void SetSyncProperties(Vector3 velocity, Vector3 position, float rotationTimes100, short headRotationTimes100, ushort movementStateId, ushort heldItemId)
    {
        if (isLocal) return;
        syncVelocity = velocity;
        syncPosition = position;
        syncRotation = ((float)rotationTimes100) / 100;
        syncHeadRotation = ((float)headRotationTimes100) / 100;
        syncMovementStateId = movementStateId;
        syncHeldItemId = heldItemId;
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
    public void SetPlayerState(int id, string name)
    {
        userName = name;
        this.id = id;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void MovePlayer(Vector3 position, Vector3 rotation)
    {
        GD.Print($"Moving player {GetMultiplayerAuthority()} to {position} for {Multiplayer.GetUniqueId()}");
        GlobalPosition = position;
        GlobalRotation = rotation;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void MovePlayerReliable(Vector3 position, Vector3 rotation)
    {
        GD.Print($"Moving player {GetMultiplayerAuthority()} to {position} for {Multiplayer.GetUniqueId()}");
        GlobalPosition = position;
        GlobalRotation = rotation;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void Hit(int damage, string boneName, Vector3 bulletDirection)
    {
        if (!IsMultiplayerAuthority()) return;
        GD.Print($"Player {Name} was hit for {damage}");
        Health -= (short)damage;

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
