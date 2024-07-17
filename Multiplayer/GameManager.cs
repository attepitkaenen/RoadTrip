using Godot;
using Godot.Collections;
using Riptide;
using System.Formats.Asn1;
using System.Linq;
using System.Runtime.Serialization;

public partial class GameManager : Node
{
    // Signals
    [Signal] public delegate void GameStartedEventHandler(ushort id);

    // 
    [Export] private PackedScene playerScene;
    [Export] public Array<ItemResource> itemList = new Array<ItemResource>();
    [Export] public Array<VehicleResource> vehicleList = new Array<VehicleResource>();
    public static Array<MapResource> mapList = new Array<MapResource>();

    // Save
    public int saveId;

    public MenuHandler menuHandler;
    RiptideClient riptideClient;
    RiptideServer riptideServer;

    public Map world;
    public float Sensitivity = 0.001f;
    public static bool isGameStarted = false;
    public static int activeMapId = -1;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        // Load all items, vehicles and maps
        foreach (string fileNameRemap in DirAccess.GetFilesAt("res://ItemData"))
        {
            var fileName = fileNameRemap.Replace(".remap", "");
            var item = GD.Load<ItemResource>("res://ItemData/" + fileName);
            itemList.Add(item);
        }

        foreach (string fileNameRemap in DirAccess.GetFilesAt("res://VehicleData"))
        {
            var fileName = fileNameRemap.Replace(".remap", "");
            var vehicle = GD.Load<VehicleResource>("res://VehicleData/" + fileName);
            vehicleList.Add(vehicle);
        }

        foreach (string fileNameRemap in DirAccess.GetFilesAt("res://MapData"))
        {
            var fileName = fileNameRemap.Replace(".remap", "");
            var map = GD.Load<MapResource>("res://MapData/" + fileName);
            mapList.Add(map);
        }


        menuHandler = GetNode<MenuHandler>("/root/MenuHandler");
        riptideServer = GetTree().Root.GetNode<RiptideServer>("RiptideServer");
        riptideClient = GetTree().Root.GetNode<RiptideClient>("RiptideClient");
    }

    public override void _PhysicsProcess(double delta)
    {
        StartGame();
    }

    public void StartGame()
    {
        if (!isGameStarted & RiptideClient.IsHost() && !riptideClient.IsLoading())
        {
            GD.Print("Starting game: host");
            isGameStarted = true;
            MenuHandler.OpenMenu(MenuHandler.MenuType.none);
        }
        else if (!isGameStarted && !RiptideClient.IsHost() && !riptideClient.IsLoading() && !riptideClient.IsServerLoading())
        {
            GD.Print("Starting game: client");
            isGameStarted = true;
            MenuHandler.OpenMenu(MenuHandler.MenuType.none);
        }

        if (riptideClient.GetPlayersToSpawn().Count != 0 && isGameStarted)
        {
            GD.Print("Spawning players");
            SpawnPlayers(riptideClient.GetPlayersToSpawn());
        }
    }

    public void SpawnPlayers(Dictionary<ushort, PlayerState> playerStates)
    {
        var spawnPoints = GetTree().GetNodesInGroup("SpawnPoints");

        foreach (var playerState in playerStates)
        {
            GD.Print(playerStates.Count());
            GD.Print($"Spawning player {playerState.Value.Name} with id: {playerState.Key} on client: {RiptideClient.GetId()}");

            if (riptideClient.GetPlayerInstances().Keys.Contains(playerState.Key))
            {
                GD.Print("Player: " + playerState.Key + " has already been spawned: " + RiptideClient.IsHost());
                return;
            }

            var newPlayer = playerScene.Instantiate<Player>();
            newPlayer.Name = playerState.Key.ToString();

            var isLocal = RiptideClient.GetId() == playerState.Key ? true : false;
            newPlayer.SetPlayerProperties(playerState.Key, playerState.Value.Name, isLocal);

            AddChild(newPlayer, true);
            var random = new RandomNumberGenerator();
            var randomSpawnPoint = spawnPoints[random.RandiRange(0, spawnPoints.Count() - 1)] as Node3D;
            newPlayer.GlobalPosition = randomSpawnPoint.GlobalPosition;
            riptideClient.AddPlayerInstance(playerState.Key, newPlayer);

            if (RiptideClient.IsHost())
            {
                riptideServer.AddPlayerInstance(playerState.Key, newPlayer);
            }
        }
    }

    public Dictionary<ushort, PlayerState> GetPlayerStates()
    {
        return riptideClient.GetPlayerStates();
    }

    public int GetPlayerIndex(ushort id)
    {
        return GetPlayerStates().Keys.ToList().IndexOf(id);
    }

    public Array<Player> GetPlayers()
    {
        return GetTree().GetNodesInGroup("Player").Select(node => node as Player) as Array<Player>;
    }

    public void Respawn()
    {
        GD.Print($"Respawning {Multiplayer.GetUniqueId()}");
        var player = GetTree().GetNodesInGroup("Player").ToList().Find(player => player.Name == $"{Multiplayer.GetUniqueId()}") as Player;
        // int playerIndex = GetPlayerIndex(player.Id);
        var spawnPoints = GetTree().GetNodesInGroup("SpawnPoints");
        // foreach (Node3D spawnPoint in spawnPoints)
        // {
        // 	if (int.Parse(spawnPoint.Name) == playerIndex)
        // 	{
        // 		player.Rpc(nameof(player.MovePlayer), spawnPoint.GlobalPosition, Vector3.Zero);
        // 	}
        // }
    }

    public ItemResource GetItemResource(int id)
    {
        if (itemList is not null)
        {
            return itemList.FirstOrDefault(item => item.Id == id);
        }
        return null;
    }

    public VehicleResource GetVehicleResource(int id)
    {
        if (vehicleList is not null)
        {
            return vehicleList.FirstOrDefault(vehicle => vehicle.Id == id);
        }
        return null;
    }

    public void RemovePlayer(ushort id)
    {
        // var players = GetTree().GetNodesInGroup("Player");
        // if (players.Count > 0)
        // {
        // 	var player = players.FirstOrDefault(i => (i as Player).Id == id);
        // 	if (player is not null)
        // 	{
        // 		player.QueueFree();
        // 	}
        // }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SpawnItem(int playerId, int id, float condition, Vector3 position, Vector3 rotation)
    {
        var scene = GetItemResource(id).ItemOnFloor;
        var item = scene.Instantiate() as Item;
        AddChild(item);

        item.GlobalPosition = position;
        item.GlobalRotation = rotation;
        item.condition = condition;

        if (playerId == 0)
        {
            return;
        }
        var player = GetNode<Player>($"{playerId}");
        player.playerInteraction.RpcId(playerId, nameof(player.playerInteraction.SetPickedItem), item.GetPath());
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SpawnVehicle(int id, Vector3 position, Vector3 rotation, Array<VehiclePartSaveResource> vehicleParts)
    {
        var scene = GetVehicleResource(id).VehicleBody;
        var vehicle = scene.Instantiate() as Vehicle;
        AddChild(vehicle);

        vehicle.GlobalPosition = position;
        vehicle.GlobalRotation = rotation;

        if (vehicleParts.Count < 1) return;

        var partMounts = vehicle.GetPartMounts();
        foreach (PartMount partMount in partMounts)
        {
            foreach (VehiclePartSaveResource partSave in vehicleParts)
            {
                if (partSave.PartMountName == partMount.Name)
                {
                    partMount.partId = partSave.Id;
                    partMount.partCondition = partSave.Condition;
                }
            }
        }
    }


    public void InstantiateMap(PackedScene scene)
    {
        var map = scene.Instantiate() as Map;
        activeMapId = map.id;
        world = map;
        AddChild(map);
    }

    Node SpawnNode(string nodePath)
    {
        return (ResourceLoader.Load(nodePath) as PackedScene).Instantiate();
    }

    public void ResetGameState()
    {
        GD.Print("Resetting game");
        isGameStarted = false;
        world = null;
        var destroyList = GetChildren().ToList();
        if (destroyList.Count > 0)
        {
            destroyList.ForEach(node => node.QueueFree());
        }
    }
}
