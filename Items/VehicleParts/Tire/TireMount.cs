using Godot;
using System;

public partial class TireMount : VehicleWheel3D
{
    MultiplayerSynchronizer multiplayerSynchronizer;
    GameManager gameManager;

    private Tire _tire;
    private Area3D _tireArea;
    [Export] private int _tireId;
    [Export] private float _tireCondition;

    public override void _Ready()
    {
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
        multiplayerSynchronizer = GetParent().GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer");

        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_tireId");
        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_tireCondition");

        _tireArea = GetNode<Area3D>("Area3D");
        _tireArea.BodyEntered += PartEntered;
        _tireArea.BodyExited += PartExited;
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleTire();

        if (_tire is not null)
        {
            SuspensionTravel = 0.15f;
            WheelRadius = _tire.GetRadius();
        }
        else
        {
            SuspensionTravel = 0f;
            WheelRadius = 0.1f;
        }
    }

    public CarPart SpawnInstalledPart(int itemId, float condition, Vector3 partPosition)
    {
        var part = gameManager.GetItemResource(itemId).ItemInHand.Instantiate() as Tire;
        AddChild(part);
        part.SetTireMount(this);
        part.SetCondition(condition);
        part.itemId = itemId;
        part.Position = partPosition;
        return part;
    }

    // Handles part removing
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RemoveInstalledPart(int itemId, float condition, Vector3 position)
    {
        if (_tireId == itemId)
        {
            _tireId = 0;
        }
        gameManager.RpcId(1, nameof(gameManager.SpawnVehiclePart), itemId, condition, position);
    }

    private void PartEntered(Node3D body)
    {
        GD.Print("Tire entered");
        if (body is TireDropped tire)
        {
            tire.InstallPart += InstallTire;
            tire.isInstallable = true;
        }
    }

    private void PartExited(Node3D body)
    {
        if (body is TireDropped tire)
        {
            tire.InstallPart -= InstallTire;
            tire.isInstallable = false;
        }
    }

    private void InstallTire(int itemId, float condition)
    {
        if (_tireId == 0)
        {
            Rpc(nameof(SetTireIdAndCondition), itemId, condition);
        }
    }

    public void HandleTire()
    {
        if (_tireId != 0 && _tire is null)
        {
            _tire = SpawnInstalledPart(_tireId, _tireCondition, _tireArea.Position) as Tire;
        }
        else if (_tireId == 0 && _tire is not null)
        {
            _tire = null;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetTireIdAndCondition(int id, float condition)
    {
        _tireCondition = condition;
        _tireId = id;
    }

}
