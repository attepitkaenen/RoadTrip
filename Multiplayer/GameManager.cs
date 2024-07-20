using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class GameManager : Node
{
    [Signal] public delegate void GameStartedEventHandler(long id);
    [Export] public Array<ItemResource> itemList = new Array<ItemResource>();
    [Export] public Array<VehicleResource> vehicleList = new Array<VehicleResource>();
    [Export] public Array<MapResource> mapList = new Array<MapResource>();
    [Export] public MultiplayerSpawner multiplayerSpawner;

    public static bool isGameStarted = false;
    public int saveId;
    public MenuHandler menuHandler;
    public Map world;
    MultiplayerController multiplayerController;
    PlayerManager playerManager;
    public float Sensitivity = 0.001f;

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


        multiplayerController = GetTree().Root.GetNode<MultiplayerController>("MultiplayerController");
        playerManager = GetTree().Root.GetNode<PlayerManager>("PlayerManager");
        menuHandler = GetNode<MenuHandler>("/root/MenuHandler");

        Callable spawnCallable = new Callable(this, MethodName.SpawnNode);
        multiplayerSpawner.SpawnFunction = spawnCallable;
    }

    Node SpawnNode(string nodePath)
    {
        return (ResourceLoader.Load(nodePath) as PackedScene).Instantiate();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!isGameStarted && !PlayerManager.localPlayerState.IsLoading && !PlayerManager.playerStates[1].IsLoading)
        {
            isGameStarted = true;
        }
    }

    public Array<Player> GetPlayers()
    {
        return GetTree().GetNodesInGroup("Player").Select(node => node as Player) as Array<Player>;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Respawn(int id)
    {
        GD.Print($"Respawning {Multiplayer.GetUniqueId()}");
        // if (players.TryGetValue(id, out Player player))
        // {
        // 	int playerIndex = GetPlayerIndex(player.id);
        // 	var spawnPoints = GetTree().GetNodesInGroup("SpawnPoints");
        // 	foreach (Node3D spawnPoint in spawnPoints)
        // 	{
        // 		if (int.Parse(spawnPoint.Name) == playerIndex)
        // 		{
        // 			player.Rpc(nameof(player.MovePlayer), spawnPoint.GlobalPosition, Vector3.Zero);
        // 		}
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

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SpawnItem(int playerId, int id, float condition, Vector3 position, Vector3 rotation)
    {
        var scene = GetItemResource(id).ItemOnFloor.ResourcePath;
        var item = multiplayerSpawner.Spawn(scene) as Item;

        item.GlobalPosition = position;
        item.GlobalRotation = rotation;
        item.condition = condition;

        if (playerId == 0)
        {
            return;
        }

        if (PlayerManager.playerInstances.TryGetValue(playerId, out var player))
        {
            item.GlobalPosition = player.playerInteraction.syncHandPosition;
            item.PickItem(playerId);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SpawnVehicle(int id, Vector3 position, Vector3 rotation, Array<VehiclePartSaveResource> vehicleParts)
    {
        var scene = GetVehicleResource(id).VehicleBody.ResourcePath;
        var vehicle = multiplayerSpawner.Spawn(scene) as Vehicle;

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

    public void InstantiateMap(string mapPath)
    {
        var map = multiplayerSpawner.Spawn(mapPath) as Map;
        world = map;
        GD.Print("Setting world to " + world);
        isGameStarted = true;
        playerManager.SpawnPlayers();
    }

    public void ResetWorld()
    {
        GD.Print("Resetting world");
        isGameStarted = false;
        world = null;
        var destroyList = GetChildren().Where(node => node is not MultiplayerSpawner).ToList();
        if (destroyList.Count > 0)
        {
            destroyList.ForEach(node => node.QueueFree());
        }
    }
}
