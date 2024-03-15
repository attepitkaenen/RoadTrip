using System;
using System.Reflection.Metadata;
using Godot;
using Godot.Collections;

public partial class EngineBay : Node3D
{
    private bool _running;

    [ExportGroup("Engine properties")]
    Engine _engine;
    PartMount _engineMount;
    [Export] private int _engineId;
    [Export] private float _engineCondition;

    [ExportGroup("Radiator properties")]
    private Radiator _radiator;
    private PartMount _radiatorMount;
    [Export] private int _radiatorId;
    [Export] private float _radiatorCondition;

    [ExportGroup("Fuel injector properties")]
    private FuelInjector _fuelInjector;
    private PartMount _fuelInjectorMount;
    [Export] private int _fuelInjectorId;
    [Export] private float _fuelInjectorCondition;

    [ExportGroup("Battery properties")]
    private Battery _battery;
    private PartMount _batteryMount;
    [Export] private int _batteryId;
    [Export] private float _batteryCondition;

    [ExportGroup("Alternator properties")]
    private Alternator _alternator;
    private PartMount _alternatorMount;
    [Export] private int _alternatorId;
    [Export] private float _alternatorCondition;

    [ExportGroup("Intake properties")]
    private Intake _intake;
    private PartMount _intakeMount;
    [Export] private int _intakeId;
    [Export] private float _intakeCondition;

    [ExportGroup("Water tank properties")]
    private WaterTank _waterTank;
    private PartMount _waterTankMount;
    [Export] private int _waterTankId;
    [Export] private float _waterTankCondition;

    [ExportGroup("Starter properties")]
    private Starter _starter;
    private PartMount _starterMount;
    [Export] private int _starterId;
    [Export] private float _starterCondition;

    public override void _EnterTree()
    {
        _engineMount = GetNode<PartMount>("EngineMount");
        _engineMount.PartChanged += PartChanged;

        _batteryMount = GetNode<PartMount>("BatteryMount");
        _batteryMount.PartChanged += PartChanged;

        _starterMount = GetNode<PartMount>("StarterMount");
        _starterMount.PartChanged += PartChanged;

        _radiatorMount = GetNode<PartMount>("RadiatorMount");
        _radiatorMount.PartChanged += PartChanged;

        _alternatorMount = GetNode<PartMount>("AlternatorMount");
        _alternatorMount.PartChanged += PartChanged;

        _fuelInjectorMount = GetNode<PartMount>("FuelInjectorMount");
        _fuelInjectorMount.PartChanged += PartChanged;

        _intakeMount = GetNode<PartMount>("IntakeMount");
        _intakeMount.PartChanged += PartChanged;

        _waterTankMount = GetNode<PartMount>("WaterTankMount");
        _waterTankMount.PartChanged += PartChanged;
    }

    public override void _PhysicsProcess(double delta)
    {
        // if any required parts missing, stop running
        if (_engine is null || _battery is null || _starter is null)
        {
            _running = false;
        }

        HandleEngine();

        HandleAlternator();

        HandleBattery();

        HandleFuelInjector();

        HandleFuelInjector();

        HandleIntake();

        HandleStarter();

        HandleWaterTank();
    }

    public void PartChanged(int id, float condition, string partType)
    {
        switch (partType)
        {
            case "Engine":
                _engineId = id;
                _engineCondition = condition;
                break;
            case "Battery":
                _batteryId = id;
                _batteryCondition = condition;
                break;
            case "Starter":
                _starterId = id;
                _starterCondition = condition;
                break;
            case "Alternator":
                _alternatorId = id;
                _alternatorCondition = condition;
                break;
            case "Radiator":
                _radiatorId = id;
                _radiatorCondition = condition;
                break;
            case "Intake":
                _intakeId = id;
                _intakeCondition = condition;
                break;
            case "WaterTank":
                _waterTankId = id;
                _waterTankCondition = condition;
                break;
            case "FuelInjector":
                _fuelInjectorId = id;
                _fuelInjectorCondition = condition;
                break;
        }
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

    // Engine
    public void HandleEngine()
    {
        if (_engineId != 0 && _engine is null)
        {
            _engine = _engineMount.GetPart() as Engine;
        }
        else if (_engineId == 0)
        {
            _engine = null;
        }
    }

    // Battery
    public void HandleBattery()
    {
        if (_batteryId != 0 && _battery is null)
        {
            _battery = _batteryMount.GetPart() as Battery;
        }
        else if (_batteryId == 0)
        {
            _battery = null;
        }
    }

    // Starter
    public void HandleStarter()
    {
        if (_starterId != 0 && _starter is null)
        {
            _starter = _starterMount.GetPart() as Starter;
        }
        else if (_starterId == 0)
        {
            _starter = null;
        }
    }

    // Alternator
    public void HandleAlternator()
    {
        if (_alternatorId != 0 && _alternator is null)
        {
            _alternator = _alternatorMount.GetPart() as Alternator;
        }
        else if (_alternatorId == 0)
        {
            _alternator = null;
        }
    }

    // Radiator
    public void HandleRadiator()
    {
        if (_radiatorId != 0 && _radiator is null)
        {
            _radiator = _radiatorMount.GetPart() as Radiator;
        }
        else if (_radiatorId == 0)
        {
            _radiator = null;
        }
    }

    // FuelInjector
    public void HandleFuelInjector()
    {
        if (_fuelInjectorId != 0 && _fuelInjector is null)
        {
            _fuelInjector = _fuelInjectorMount.GetPart() as FuelInjector;
        }
        else if (_fuelInjectorId == 0)
        {
            _fuelInjector = null;
        }
    }

    // Intake
    public void HandleIntake()
    {
        if (_intakeId != 0 && _intake is null)
        {
            _intake = _intakeMount.GetPart() as Intake;
        }
        else if (_intakeId == 0)
        {
            _intake = null;
        }
    }

    // WaterTank
    public void HandleWaterTank()
    {
        if (_waterTankId != 0 && _waterTank is null)
        {
            _waterTank = _waterTankMount.GetPart() as WaterTank;
        }
        else if (_waterTankId == 0)
        {
            _waterTank = null;
        }
    }
}
