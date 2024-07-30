using Godot;
using System.Collections.Generic;

public partial class Server : Node
{
    Client _client;

    // Tick
    private float timer;
    private int currentTick;
    private float minTimeBetweenTicks;
    private const float SERVER_TICK_RATE = 30f;
    private const int BUFFER_SIZE = 1024;

    private static Dictionary<int, PlayerStatePayload[]> stateBuffers = new Dictionary<int, PlayerStatePayload[]>();
    private static Dictionary<int, Queue<InputPayload>> inputQueues = new Dictionary<int, Queue<InputPayload>>();


    public override void _Ready()
    {
        // Tick
        minTimeBetweenTicks = 1f / SERVER_TICK_RATE;

        _client = GetTree().Root.GetNode<Client>("Client");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (currentTick % 150 == 0)
        {
            _client.Rpc(nameof(_client.SyncTick), currentTick);
        }
    }


    public override void _Process(double delta)
    {
        timer += (float)delta;

        while (timer >= minTimeBetweenTicks)
        {
            timer -= minTimeBetweenTicks;
            HandleTick();
            currentTick++;
        }
    }

    public static void AddPlayerBufferAndQueue(int id)
    {
        stateBuffers[id] = new PlayerStatePayload[BUFFER_SIZE];
        inputQueues[id] = new Queue<InputPayload>();
    }

    void SendSync()
    {
        _client.Rpc(nameof(_client.SyncTick), currentTick);
    }

    void HandleTick()
    {
        foreach (var inputQueue in inputQueues)
        {
            // Process the input queue
            int bufferIndex = -1;
            while (inputQueue.Value.Count > 0)
            {
                InputPayload inputPayload = inputQueue.Value.Dequeue();

                bufferIndex = inputPayload.tick % BUFFER_SIZE;

                PlayerStatePayload playerStatePayload = ProcessMovement(inputPayload);
                stateBuffers[inputQueue.Key][bufferIndex] = playerStatePayload;
            }

            if (bufferIndex != -1)
            {
                var buffer = stateBuffers[inputQueue.Key][bufferIndex];
                _client.Rpc(nameof(_client.OnServerMovementState), 
                                    buffer.tick, 
                                    buffer.playerId, 
                                    buffer.health, 
                                    buffer.syncVelocity, 
                                    buffer.syncPosition, 
                                    buffer.syncRotation, 
                                    buffer.syncHeadRotationX, 
                                    buffer.syncHeadRotationY, 
                                    buffer.syncMovementStateId, 
                                    buffer.syncHeldItemId
                            );
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void OnClientInput(int tick, int playerId, float rotation, float up, float down, float left, float right, float jump, float crouch, float sprint)
    {
        if (inputQueues.TryGetValue(playerId, out var inputQueue))
        {
            inputQueue.Enqueue(new InputPayload() {tick = tick, playerId = playerId, rotation = rotation, inputs = new float[]{up, down, left, right, jump, crouch, sprint}});
        }
    }

    PlayerStatePayload ProcessMovement(InputPayload inputPayload)
    {
        // Should always be in sync with same function on Client
        if (PlayerManager.playerInstances.TryGetValue(inputPayload.playerId, out Player player))
        {
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

        return new PlayerStatePayload();
    }
}
