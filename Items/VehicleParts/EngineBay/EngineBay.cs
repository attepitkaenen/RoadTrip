using System;
using System.Reflection.Metadata;
using Godot;
using Godot.Collections;

public partial class EngineBay : Node3D
{
    [Export] MultiplayerSpawner multiplayerSpawner;
    MultiplayerSynchronizer multiplayerSynchronizer;
    GameManager gameManager;

    private bool _running;

    [ExportGroup("Engine properties")]
    private Engine _engine;
    private Area3D _engineArea;
    [Export] private int _engineId;
    [Export] private float _engineCondition;

    [ExportGroup("Radiator properties")]
    private Radiator _radiator;
    private Area3D _radiatorArea;
    [Export] private int _radiatorId;
    [Export] private float _radiatorCondition;

    [ExportGroup("Fuel injector properties")]
    private FuelInjector _fuelInjector;
    private Area3D _fuelInjectorArea;
    [Export] private int _fuelInjectorId;
    [Export] private float _fuelInjectorCondition;

    [ExportGroup("Battery properties")]
    private Battery _battery;
    private Area3D _batteryArea;
    [Export] private int _batteryId;
    [Export] private float _batteryCondition;

    [ExportGroup("Alternator properties")]
    private Alternator _alternator;
    private Area3D _alternatorArea;
    [Export] private int _alternatorId;
    [Export] private float _alternatorCondition;

    [ExportGroup("Intake properties")]
    private Intake _intake;
    private Area3D _intakeArea;
    [Export] private int _intakeId;
    [Export] private float _intakeCondition;

    [ExportGroup("Water tank properties")]
    private WaterTank _waterTank;
    private Area3D _waterTankArea;
    [Export] private int _waterTankId;
    [Export] private float _waterTankCondition;

    [ExportGroup("Starter properties")]
    private Starter _starter;
    private Area3D _starterArea;
    [Export] private int _starterId;
    [Export] private float _starterCondition;




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
        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_waterTankId");
        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_waterTankCondition");
        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_intakeId");
        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_intakeCondition");
        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_starterId");
        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_starterCondition");

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

        _waterTankArea = GetNode<Area3D>("WaterTank/Area3D");
        _waterTankArea.BodyEntered += PartEntered;
        _waterTankArea.BodyExited += PartExited;

        _intakeArea = GetNode<Area3D>("Intake/Area3D");
        _intakeArea.BodyEntered += PartEntered;
        _intakeArea.BodyExited += PartExited;

        _starterArea = GetNode<Area3D>("Starter/Area3D");
        _starterArea.BodyEntered += PartEntered;
        _starterArea.BodyExited += PartExited;
    }

    public override void _PhysicsProcess(double delta)
    {
        // The fucking ids of the parts change to 0 for 3 frames and then back to the correct one forever, I have no idea why

        // if any required parts missing, stop running
        if (_engine is null || _battery is null || _starter is null)
        {
            _running = false;
        }

        HandleEngine();

        HandleRadiator();

        HandleBattery();

        HandleFuelInjector();

        HandleAlternator();

        HandleWaterTank();

        HandleIntake();
        
        HandleStarter();
    }

    public void HandleEngineUpdate()
    {
        
    }


    // Gameplay logic
    public void ToggleEngine()
    {
        if (_engine is null || _battery is null || _starter is null) return;
        _running = !_running;
    }

    public float GetHorsePower()
    {
        if (!_running)
        {
            return 0;
        }
        return _engine.GetEnginePower();
    }

    // General part handling

    // Make part not eligible to be installed
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
        else if (body is WaterTankDropped waterTank)
        {
            waterTank.InstallPart -= InstallWaterTank;
            waterTank.isInstallable = false;
        }
        else if (body is IntakeDropped intake)
        {
            intake.InstallPart -= InstallIntake;
            intake.isInstallable = false;
        }
        else if (body is StarterDropped starter)
        {
            starter.InstallPart -= InstallStarter;
            starter.isInstallable = false;
        }
    }

    // Make part eligible to be installed
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
        else if (body is WaterTankDropped waterTank)
        {
            waterTank.InstallPart += InstallWaterTank;
            waterTank.isInstallable = true;
        }
        else if (body is IntakeDropped intake)
        {
            intake.InstallPart += InstallIntake;
            intake.isInstallable = true;
        }
        else if (body is StarterDropped starter)
        {
            starter.InstallPart += InstallStarter;
            starter.isInstallable = true;
        }
    }

    // Spawns installed part and sets its condition and itemId
    public CarPart SpawnInstalledPart(int itemId, float condition, Vector3 partPosition)
    {
        var part = gameManager.GetItemResource(itemId).ItemInHand.Instantiate() as CarPart;
        AddChild(part);
        part.SetEngineBay(this);
        part.SetCondition(condition);
        part.itemId = itemId;
        part.Position = partPosition;
        return part;
    }

    // Handles part removing
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
        else if (_waterTankId == itemId)
        {
            _waterTankId = 0;
        }
        else if (_starterId == itemId)
        {
            _starterId = 0;
        }

        gameManager.RpcId(1, nameof(gameManager.SpawnVehiclePart), itemId, condition, position);
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
            GD.Print("make alternator null");
            _alternator = null;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetAlternatorIdAndCondition(int id, float condition)
    {
        _alternatorCondition = condition;
        _alternatorId = id;
    }

    // WaterTank
    private void InstallWaterTank(int itemId, float condition)
    {
        if (_waterTank is null)
        {
            Rpc(nameof(SetWaterTankIdAndCondition), itemId, condition);
        }
    }

    private void HandleWaterTank()
    {
        if (_waterTankId != 0 && _waterTank is null)
        {
            _waterTank = SpawnInstalledPart(_waterTankId, _waterTankCondition, _waterTankArea.Position) as WaterTank;
        }
        else if (_waterTankId == 0 && _waterTank is not null)
        {
            _waterTank = null;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetWaterTankIdAndCondition(int id, float condition)
    {
        _waterTankCondition = condition;
        _waterTankId = id;
    }

    // Intake
    private void InstallIntake(int itemId, float condition)
    {
        if (_intake is null)
        {
            Rpc(nameof(SetIntakeIdAndCondition), itemId, condition);
        }
    }

    private void HandleIntake()
    {
        if (_intakeId != 0 && _intake is null)
        {
            _intake = SpawnInstalledPart(_intakeId, _intakeCondition, _intakeArea.Position) as Intake;
        }
        else if (_intakeId == 0 && _intake is not null)
        {
            _intake = null;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetIntakeIdAndCondition(int id, float condition)
    {
        _intakeCondition = condition;
        _intakeId = id;
    }

    // Starter
    private void InstallStarter(int itemId, float condition)
    {
        if (_starter is null)
        {
            Rpc(nameof(SetStarterIdAndCondition), itemId, condition);
        }
    }

    private void HandleStarter()
    {
        if (_starterId != 0 && _starter is null)
        {
            _starter = SpawnInstalledPart(_starterId, _starterCondition, _starterArea.Position) as Starter;
        }
        else if (_starterId == 0 && _starter is not null)
        {
            _starter = null;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetStarterIdAndCondition(int id, float condition)
    {
        _starterCondition = condition;
        _starterId = id;
    }
}
