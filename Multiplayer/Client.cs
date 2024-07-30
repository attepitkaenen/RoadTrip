using Godot;
using System;
using System.Collections;
using System.Collections.Generic;


public partial class Client : Node
{
    Server _server;

    // Shared
    private float timer;
    private int currentTick;
    private ushort tickDivergenceTolerance = 1;
    private float minTimeBetweenTicks;
    private const float SERVER_TICK_RATE = 30f;
    private const int BUFFER_SIZE = 1024;
    private float[] localInputs;

    // Client specific
    private PlayerStatePayload[] stateBuffer;
    private InputPayload[] inputBuffer;
    private PlayerStatePayload latestServerState;
    private PlayerStatePayload lastProcessedState;

    public override void _Ready()
    {
        minTimeBetweenTicks = 1f / SERVER_TICK_RATE;

        stateBuffer = new PlayerStatePayload[BUFFER_SIZE];
        inputBuffer = new InputPayload[BUFFER_SIZE];

        _server = GetTree().Root.GetNode<Server>("Server");
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
    }

    public override void _Process(double delta)
    {
        timer += (float)delta;

        localInputs = GetInputs();

        while (timer >= minTimeBetweenTicks)
        {
            timer -= minTimeBetweenTicks;
            HandleTick();
            currentTick++;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void SyncTick(int serverTick)
    {
        if (Mathf.Abs(serverTick - currentTick) > tickDivergenceTolerance)
        {
            GD.Print($"Syncing current tick {currentTick} to server tick {serverTick}");
            currentTick = serverTick;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void OnServerMovementState(int tick, int playerId, short health, Vector3 syncVelocity, Vector3 syncPosition, float syncRotation, float syncHeadRotationX, float syncHeadRotationY, ushort syncMovementStateId, ushort syncHeldItemId)
    {
        if (playerId == Multiplayer.GetUniqueId())
        {
            latestServerState = new PlayerStatePayload()
            {
                tick = tick,
                playerId = playerId,
                health = health,
                syncVelocity = syncVelocity,
                syncPosition = syncPosition,
                syncRotation = syncRotation,
                syncHeadRotationX = syncHeadRotationX,
                syncHeadRotationY = syncHeadRotationY,
                syncMovementStateId = syncMovementStateId,
                syncHeldItemId = syncHeldItemId
            };
        }
        else if (PlayerManager.playerInstances.TryGetValue(playerId, out Player player))
        {
            player.Health = health;
            player.syncVelocity = syncVelocity;
            player.syncPosition = syncPosition;
            player.syncRotation = syncRotation;
            player.syncHeadRotationX = syncHeadRotationX;
            player.syncHeadRotationY = syncHeadRotationY;
            player.syncMovementStateId = syncMovementStateId;
            player.syncHeldItemId = syncHeldItemId;
        }
    }

    void HandleTick()
    {
        if (!Multiplayer.IsServer() &&
            !latestServerState.Equals(default(PlayerStatePayload)) &&
            (lastProcessedState.Equals(default(PlayerStatePayload)) ||
            !latestServerState.Equals(lastProcessedState)))
        {
            HandleServerReconciliation();
        }

        int bufferIndex = currentTick % BUFFER_SIZE;

        // Add payload to inputBuffer
        InputPayload inputPayload = new InputPayload();
        inputPayload.tick = currentTick;
        inputPayload.playerId = Multiplayer.GetUniqueId();
        inputPayload.inputs = localInputs;
        var player = PlayerManager.localPlayerInstance;
        if (player is not null)
        {
            inputPayload.rotation = player.GlobalRotation.Y;
        }
        inputBuffer[bufferIndex] = inputPayload;

        // Add payload to stateBuffer if not host
        if (!Multiplayer.IsServer())
        {
            stateBuffer[bufferIndex] = ProcessMovement(inputPayload);
        }

        // Send input to server
        _server.Rpc(nameof(_server.OnClientInput),
                            inputPayload.tick,
                            inputPayload.playerId,
                            inputPayload.rotation,
                            inputPayload.inputs[(int)Inputs.up],
                            inputPayload.inputs[(int)Inputs.down],
                            inputPayload.inputs[(int)Inputs.left],
                            inputPayload.inputs[(int)Inputs.right],
                            inputPayload.inputs[(int)Inputs.jump],
                            inputPayload.inputs[(int)Inputs.crouch],
                            inputPayload.inputs[(int)Inputs.sprint]
                    );
    }

    float[] GetInputs()
    {
        var amountOfInputs = Enum.GetNames(typeof(Inputs)).Length;
        float[] newInputs = new float[amountOfInputs];
        newInputs[(int)Inputs.up] = Input.GetActionStrength("up");
        newInputs[(int)Inputs.down] = Input.GetActionStrength("down");
        newInputs[(int)Inputs.left] = Input.GetActionStrength("left");
        newInputs[(int)Inputs.right] = Input.GetActionStrength("right");
        newInputs[(int)Inputs.jump] = Input.GetActionStrength("jump");
        newInputs[(int)Inputs.crouch] = Input.GetActionStrength("crouch");
        newInputs[(int)Inputs.sprint] = Input.GetActionStrength("sprint");

        return newInputs;
    }

    PlayerStatePayload ProcessMovement(InputPayload inputPayload)
    {
        // Should always be in sync with same function on Server
        var player = PlayerManager.localPlayerInstance;

        if (player is null) return new PlayerStatePayload();

        player.HandleMovement(minTimeBetweenTicks, inputPayload.rotation, inputPayload.inputs);

        return new PlayerStatePayload()
        {
            tick = inputPayload.tick,
            playerId = player.id,
            health = player.Health,
            syncVelocity = player.syncVelocity,
            syncPosition = player.syncPosition,
            syncRotation = player.syncRotation,
            syncHeadRotationX = player.syncHeadRotationX,
            syncHeadRotationY = player.syncHeadRotationY,
            syncMovementStateId = player.syncMovementStateId,
            syncHeldItemId = player.syncHeldItemId
        };
    }

    void HandleServerReconciliation()
    {
        lastProcessedState = latestServerState;

        int serverStateBufferIndex = latestServerState.tick % BUFFER_SIZE;

        float XZPositionError = new Vector2(latestServerState.syncPosition.X, latestServerState.syncPosition.Z).DistanceTo(new Vector2(stateBuffer[serverStateBufferIndex].syncPosition.X, stateBuffer[serverStateBufferIndex].syncPosition.Z));

        if (XZPositionError > 0.1f)
        {
            var player = PlayerManager.localPlayerInstance;
            GD.Print($"We have to reconcile from {player.GlobalPosition} to {latestServerState.syncPosition}");
            // Rewind & Replay
            player.GlobalPosition = latestServerState.syncPosition;

            // Update buffer at index of latest server state
            stateBuffer[serverStateBufferIndex] = latestServerState;

            // Now re-simulate the rest of the ticks up to the current tick on the client
            int tickToProcess = latestServerState.tick + 1;

            while (tickToProcess < currentTick)
            {
                int bufferIndex = tickToProcess % BUFFER_SIZE;

                // Process new movement with reconciled state
                PlayerStatePayload playerStatePayload = ProcessMovement(inputBuffer[bufferIndex]);

                // Update buffer with recalculated state
                stateBuffer[bufferIndex] = playerStatePayload;

                tickToProcess++;
            }
        }
    }
}
