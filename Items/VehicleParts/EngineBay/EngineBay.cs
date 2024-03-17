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

        _batteryMount = GetNode<PartMount>("BatteryMount");

        _starterMount = GetNode<PartMount>("StarterMount");

        _radiatorMount = GetNode<PartMount>("RadiatorMount");

        _alternatorMount = GetNode<PartMount>("AlternatorMount");

        _fuelInjectorMount = GetNode<PartMount>("FuelInjectorMount");

        _intakeMount = GetNode<PartMount>("IntakeMount");

        _waterTankMount = GetNode<PartMount>("WaterTankMount");
    }

    public override void _PhysicsProcess(double delta)
    {
        // if any required parts missing, stop running
        if (_engine is null || _battery is null || _starter is null)
        {
            _running = false;
        }

        _engine = _engineMount.GetPart();
        _battery = _batteryMount.GetPart();
        _starter = _starterMount.GetPart();
        _radiator = _radiatorMount.GetPart();
        _alternator = _alternatorMount.GetPart();
        _waterTank = _waterTankMount.GetPart();
        _intake = _intakeMount.GetPart();
        _fuelInjector = _fuelInjectorMount.GetPart();
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
}
