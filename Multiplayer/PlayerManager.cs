using Godot;
using Godot.Collections;
using System;

public partial class PlayerManager : Node
{
    // Resources
    [Export] private PackedScene playerScene;

    // Signals
    [Signal] public delegate void PlayerStatesChangedEventHandler(long peerId, PlayerState state);


    // Players
    public static Dictionary<int, PlayerState> playerStates { get; private set; } = new Dictionary<int, PlayerState>();

    public static Dictionary<int, Player> playerInstances { get; private set; } = new Dictionary<int, Player>();
    public static PlayerState localPlayerState { get; private set; } = new PlayerState() { Id = -1, Name = "Jorma", IsLoading = true };
    public static int playerCount = 0;

    // References	
    MultiplayerController multiplayerController;
    MultiplayerSpawner multiplayerSpawner;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        multiplayerController = GetTree().Root.GetNode<MultiplayerController>("MultiplayerController");
        multiplayerSpawner = GetNode<MultiplayerSpawner>("MultiplayerSpawner");

        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;

        Callable spawnCallable = new Callable(this, MethodName.SpawnNode);
        multiplayerSpawner.SpawnFunction = spawnCallable;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (playerCount != playerStates.Count)
        {
            GD.Print($"PlayerState count changed from {playerCount} to {playerStates.Count} on {localPlayerState.Id}");
            playerCount = playerStates.Count;
            EmitSignal(SignalName.PlayerStatesChanged);
        }
    }

    Node SpawnNode(string nodePath)
    {
        return (ResourceLoader.Load(nodePath) as PackedScene).Instantiate();
    }

    private void OnPeerDisconnected(long peerId)
    {
        if (playerInstances.TryGetValue((int)peerId, out Player player))
        {
            player.QueueFree();
            playerInstances.Remove((int)peerId);
        }

        if (playerStates.TryGetValue((int)peerId, out PlayerState state))
        {
            playerStates.Remove((int)peerId);
        }
    }

    public void OnConnectedToServer()
    {
        GD.Print($"Player connected, setting local Id to {Multiplayer.GetUniqueId()}");
        localPlayerState.Id = Multiplayer.GetUniqueId();
    }


    public void AddPlayerInstace(Player player)
    {
        playerInstances.Add(player.id, player);
    }

    public static void AddPlayerState(PlayerState playerState)
    {
        if (playerStates.TryGetValue(playerState.Id, out _))
        {
            if (playerState.Id == localPlayerState.Id)
            {
                localPlayerState = playerState;
            }
            playerStates[playerState.Id] = playerState;
        }
        else
        {
            playerStates.Add(playerState.Id, playerState);
        }
    }

    public static void SetLoadingStatus(int id, bool status)
    {
        if (playerStates.TryGetValue(id, out PlayerState playerState))
        {
            if (id == localPlayerState.Id)
            {
                localPlayerState.IsLoading = status;
            }
            playerState.IsLoading = status;
        }
    }

    public static PlayerState GetPlayerState(int id)
    {
        if (playerStates.TryGetValue(id, out PlayerState state))
        {
            return state;
        }
        throw new Exception("A playerState of this Id doesn't exist.");
    }

    public void SpawnPlayers()
    {
        foreach (var playerState in playerStates)
        {
            SpawnPlayer(playerState.Value);
        }
    }

    public void SpawnPlayer(PlayerState playerState)
    {
        foreach (var player in playerInstances)
        {
            if (player.Key == playerState.Id)
            {
                GD.Print("Player: " + player.Value.Name + " has already been spawned");
                return;
            }
        }

        GD.Print($"Spawning player {playerState.Name} with id: {playerState.Id}");
        var currentPlayer = multiplayerSpawner.Spawn(playerScene.ResourcePath) as Player;
        playerInstances.Add(playerState.Id, currentPlayer);
        currentPlayer.Rpc(nameof(currentPlayer.SetPlayerState), playerState.Id, playerState.Name);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void LocalPlayerReady(int id)
    {
        GD.Print($"LocalPlayerReady {id}");
        var random = new Random();
        int spawnPointId = random.Next(4);
        var spawnPoints = GetTree().GetNodesInGroup("SpawnPoints");
        foreach (Node3D spawnPoint in spawnPoints)
        {
            if (int.Parse(spawnPoint.Name) == spawnPointId)
            {
                if (playerInstances.TryGetValue(id, out var player))
                {
                    player.Rpc(nameof(player.MovePlayerReliable), spawnPoint.GlobalPosition, Vector3.Zero);
                }
                else
                {
                    GD.Print($"No player of id: {id}, was found");
                }
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void UpdatePlayerData(int id, short health)
    {
        GD.Print("Receiving update");
    }

    public void SetPlayerLoadingStatus(int id, bool status)
    {
        if (id == localPlayerState.Id)
        {
            localPlayerState.IsLoading = status;
        }

        if (playerStates.TryGetValue(id, out PlayerState state))
        {
            state.IsLoading = status;
        }
    }

    public static void Reset()
    {
        foreach (var player in playerInstances.Values)
        {
            player.QueueFree();
        }
        
        playerStates = new Dictionary<int, PlayerState>();
        playerInstances = new Dictionary<int, Player>();
        localPlayerState = new PlayerState { Id = -1, Name = "Jorma", IsLoading = true };
    }
}
