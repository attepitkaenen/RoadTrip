using System;
using System.Reflection.Metadata;
using Godot;
using Godot.Collections;

public partial class EngineBay : Node3D
{
    [Export] MultiplayerSpawner multiplayerSpawner;
    GameManager gameManager;

    private Engine _engine;
    private Area3D _engineArea;
    private int _engineId;
    private float _engineCondition;

    private Radiator _radiator;
    private Area3D _radiatorArea;
    private int _radiatorId;
    private float _radiatorCondition;

    private Carburetor _injector;
    private Area3D _injectorArea;
    private int _injectorId;
    private float _injectorCondition;

    private Battery _battery;
    private Area3D _batteryArea;
    private int _batteryId;
    private float _batteryCondition;

    private Alternator _alternator;
    private Area3D _alternatorArea;
    private int _alternatorId;
    private float _alternatorCondition;

    private Intake _intake;
    private Area3D _intakeArea;
    private int _intakeId;
    private float _intakeCondition;



    public override void _Ready()
    {
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");

        _engineArea = GetNode<Area3D>("Engine/Area3D");
        _engineArea.BodyEntered += PartEntered;
        _engineArea.BodyExited += PartExited;

        _radiatorArea = GetNode<Area3D>("Radiator/Area3D");
        _radiatorArea.BodyEntered += PartEntered;
        _radiatorArea.BodyExited += PartExited;
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleEngine();

        HandleRadiator();
    }

    // General part handling
    private void PartExited(Node3D body)
    {
        if (body is EngineDropped engine)
        {
            engine.InstallPart -= InstallEngine;
            engine.isInstallable = false;
        }
        else if (body is RadiatorDropped radiator)
        {
            radiator.InstallPart -= InstallRadiator;
            radiator.isInstallable = false;
        }
    }

    private void PartEntered(Node3D body)
    {
        if (body is EngineDropped engine)
        {
            engine.InstallPart += InstallEngine;
            engine.isInstallable = true;
        }
        else if (body is RadiatorDropped radiator)
        {
            radiator.InstallPart += InstallRadiator;
            radiator.isInstallable = true;
        }
    }

    public CarPart SpawnInstalledPart(int itemId, float condition, Vector3 partPosition)
    {
        var part = gameManager.GetItemResource(itemId).ItemInHand.Instantiate() as CarPart;
        AddChild(part);
        part.SetCondition(condition);
        part.itemId = itemId;
        part.Position = partPosition;
        return part;
    }

    public void RemoveInstalledPart(int itemId, float condition, Vector3 position)
    {
        if (_engineId == itemId)
        {
            _engineId = 0;
        }
        else if (_injectorId == itemId)
        {
            _injectorId = 0;
        }
        else if (_alternatorId == itemId)
        {
            _alternatorId = 0;
        }
        else if (_batteryId == itemId)
        {
            _batteryId = 0;
        }
        else if (_radiatorId == itemId)
        {
            _radiatorId = 0;
        }
        else if (_intakeId == itemId)
        {
            _intakeId = 0;
        }

        gameManager.RpcId(1, nameof(gameManager.SpawnVehiclePart), itemId, condition, position);
    }

    public float GetHorsePower()
    {
        if (_engine is null)
        {
            return 0;
        }
        return _engine.GetEnginePower();
    }

    // Engine
    private void InstallEngine(int itemId, float condition)
    {
        if (_engine is null)
        {
            Rpc(nameof(SetEngineIdAndCondition), itemId, condition);
        }
    }

    public void HandleEngine()
    {
        if (_engineId != 0 && _engine is null)
        {
            _engine = SpawnInstalledPart(_engineId, _engineCondition, _engineArea.Position) as Engine;
        }
        else if (_engineId == 0 && _engine is not null)
        {
            _engine = null;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetEngineIdAndCondition(int id, float condition)
    {
        _engineCondition = condition;
        _engineId = id;
    }


    // Radiator
    private void InstallRadiator(int itemId, float condition)
    {
        if (_radiator is null)
        {
            Rpc(nameof(SetRadiatorIdAndCondition), itemId, condition);
        }
    }

    private void HandleRadiator()
    {
        if (_radiatorId != 0 && _radiator is null)
        {
            _radiator = SpawnInstalledPart(_radiatorId, _radiatorCondition, _radiatorArea.Position) as Radiator;
        }
        else if (_radiatorId == 0 && _radiator is not null)
        {
            _radiator = null;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetRadiatorIdAndCondition(int id, float condition)
    {
        _radiatorCondition = condition;
        _radiatorId = id;
    }
}
