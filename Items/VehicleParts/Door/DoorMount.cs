using Godot;
using System;

public partial class DoorMount : Node3D, IMount
{
    MultiplayerSynchronizer multiplayerSynchronizer;
    GameManager gameManager;

    private Door _door;
    private Area3D _doorArea;
    [Export] private int _doorId;
    [Export] private float _doorCondition;
    [Export] private bool isHorizontal = false;

    public override void _Ready()
    {
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
        multiplayerSynchronizer = GetParent().GetParent().GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer");

        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_doorId");
        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_doorCondition");

        _doorArea = GetNode<Area3D>("Area3D");
        _doorArea.BodyEntered += PartEntered;
        _doorArea.BodyExited += PartExited;
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleDoor();
    }

    public Door SpawnInstalledPart(int itemId, float condition, Vector3 partPosition)
    {
        var part = gameManager.GetItemResource(itemId).ItemInHand.Instantiate() as Door;
        AddChild(part);
        part.SetMount(this);
        part.itemId = itemId;
        part.isHorizontal = isHorizontal;
        part.SetCondition(condition);
        part.Position = partPosition;
        return part;
    }

    // Handles part removing
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RemoveInstalledPart(int itemId, float condition, Vector3 position)
    {
        _doorId = 0;
        gameManager.RpcId(1, nameof(gameManager.SpawnVehiclePart), itemId, condition, position);
    }

    private void PartEntered(Node3D body)
    {
        GD.Print("Tire entered");
        if (body is DoorDropped door)
        {
            door.InstallPart += InstallDoor;
            door.isInstallable = true;
        }
    }

    private void PartExited(Node3D body)
    {
        if (body is DoorDropped door)
        {
            door.InstallPart -= InstallDoor;
            door.isInstallable = false;
        }
    }

    private void InstallDoor(int itemId, float condition)
    {
        if (_doorId == 0)
        {
            Rpc(nameof(SetDoorIdAndCondition), itemId, condition);
        }
    }

    public void HandleDoor()
    {
        if (_doorId != 0 && _door is null)
        {
            _door = SpawnInstalledPart(_doorId, _doorCondition, _doorArea.Position);
        }
        else if (_doorId == 0 && _door is not null)
        {
            _door = null;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetDoorIdAndCondition(int id, float condition)
    {
        _doorCondition = condition;
        _doorId = id;
    }

}
