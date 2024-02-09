using System;
using System.Reflection.Metadata;
using Godot;
using Godot.Collections;

public partial class EngineBay : Node3D
{
    [Export] MultiplayerSpawner multiplayerSpawner;
    MultiplayerSynchronizer multiplayerSynchronizer;
    GameManager gameManager;

    private Engine _engine;
    private Area3D _engineArea;
    private int _engineId;
    private float _engineCondition;

    private Radiator _radiator;
    private Area3D _radiatorArea;
    private int _radiatorId;
    private float _radiatorCondition;

    private FuelInjector _fuelInjector;
    private Area3D _fuelInjectorArea;
    private int _fuelInjectorId;
    private float _fuelInjectorCondition;

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
        multiplayerSynchronizer = GetParent().GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer");

        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_engineId");
        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_engineCondition");
        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_radiatorId");
        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_radiatorCondition");
        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_batteryId");
        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_batteryCondition");
        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_fuelInjectorId");
        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_fuelInjectorCondition");
        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_alternatorId");
        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_alternatorCondition");

        _engineArea = GetNode<Area3D>("Engine/Area3D");
        _engineArea.BodyEntered += PartEntered;
        _engineArea.BodyExited += PartExited;

        _radiatorArea = GetNode<Area3D>("Radiator/Area3D");
        _radiatorArea.BodyEntered += PartEntered;
        _radiatorArea.BodyExited += PartExited;

        _batteryArea = GetNode<Area3D>("Battery/Area3D");
        _batteryArea.BodyEntered += PartEntered;
        _batteryArea.BodyExited += PartExited;

        _fuelInjectorArea = GetNode<Area3D>("FuelInjector/Area3D");
        _fuelInjectorArea.BodyEntered += PartEntered;
        _fuelInjectorArea.BodyExited += PartExited;

        _alternatorArea = GetNode<Area3D>("Alternator/Area3D");
        _alternatorArea.BodyEntered += PartEntered;
        _alternatorArea.BodyExited += PartExited;
    }

    public override void _PhysicsProcess(double delta)
    {
        // The fucking ids of the parts change to 0 for 3 frames and then back to the correct one forever, I have no idea why

        HandleEngine();

        HandleRadiator();

        HandleBattery();

        HandleFuelInjector();

        HandleAlternator();
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
        else if (body is BatteryDropped battery)
        {
            battery.InstallPart -= InstallBattery;
            battery.isInstallable = false;
        }
        else if (body is FuelInjectorDropped fuelInjector)
        {
            fuelInjector.InstallPart -= InstallFuelInjector;
            fuelInjector.isInstallable = false;
        }
        else if (body is AlternatorDropped alternator)
        {
            alternator.InstallPart -= InstallAlternator;
            alternator.isInstallable = false;
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
        else if (body is BatteryDropped battery)
        {
            battery.InstallPart += InstallBattery;
            battery.isInstallable = true;
        }
        else if (body is FuelInjectorDropped fuelInjector)
        {
            fuelInjector.InstallPart += InstallFuelInjector;
            fuelInjector.isInstallable = true;
        }
        else if (body is AlternatorDropped alternator)
        {
            alternator.InstallPart += InstallAlternator;
            alternator.isInstallable = true;
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

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RemoveInstalledPart(int itemId, float condition, Vector3 position)
    {
        if (_engineId == itemId)
        {
            _engineId = 0;
        }
        else if (_fuelInjectorId == itemId)
        {
            _fuelInjectorId = 0;
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
        if (_engineId == 0)
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

    // Battery
    private void InstallBattery(int itemId, float condition)
    {
        if (_battery is null)
        {
            Rpc(nameof(SetBatteryIdAndCondition), itemId, condition);
        }
    }

    private void HandleBattery()
    {
        if (_batteryId != 0 && _battery is null)
        {
            _battery = SpawnInstalledPart(_batteryId, _batteryCondition, _batteryArea.Position) as Battery;
        }
        else if (_batteryId == 0 && _battery is not null)
        {
            _battery = null;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetBatteryIdAndCondition(int id, float condition)
    {
        _batteryCondition = condition;
        _batteryId = id;
    }

    // FuelInjector
    private void InstallFuelInjector(int itemId, float condition)
    {
        if (_fuelInjector is null)
        {
            Rpc(nameof(SetFuelInjectorIdAndCondition), itemId, condition);
        }
    }

    private void HandleFuelInjector()
    {
        if (_fuelInjectorId != 0 && _fuelInjector is null)
        {
            _fuelInjector = SpawnInstalledPart(_fuelInjectorId, _fuelInjectorCondition, _fuelInjectorArea.Position) as FuelInjector;
        }
        else if (_fuelInjectorId == 0 && _fuelInjector is not null)
        {
            _fuelInjector = null;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetFuelInjectorIdAndCondition(int id, float condition)
    {
        _fuelInjectorCondition = condition;
        _fuelInjectorId = id;
    }

    // Alternator
    private void InstallAlternator(int itemId, float condition)
    {
        if (_alternator is null)
        {
            Rpc(nameof(SetAlternatorIdAndCondition), itemId, condition);
        }
    }

    private void HandleAlternator()
    {
        if (_alternatorId != 0 && _alternator is null)
        {
            _alternator = SpawnInstalledPart(_alternatorId, _alternatorCondition, _alternatorArea.Position) as Alternator;
        }
        else if (_alternatorId == 0 && _alternator is not null)
        {
            _alternator = null;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetAlternatorIdAndCondition(int id, float condition)
    {
        _alternatorCondition = condition;
        _alternatorId = id;
    }
}
