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
    PartMount _engineMount;
    Engine _engine;
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

        _engineMount = GetNode<PartMount>("EngineMount");
        _engineMount.PartInstalled += EngineInstalled;
        _engineMount.PartUninstalled += EngineUninstalled;
        multiplayerSynchronizer.ReplicationConfig.AddProperty(_engineMount.GetPartIdPath());
        multiplayerSynchronizer.ReplicationConfig.AddProperty(_engineMount.GetPartConditionPath());
    }

    private void EngineInstalled(int itemId, float condition)
    {
        GD.Print("We got an engine! " + itemId);
        _engineId = itemId;
        _engineCondition = condition;
    }

    private void EngineUninstalled()
    {
        GD.Print("Engine uninstalled.");
        _engineId = 0;
        _engine = null;
        _engineCondition = 0;
    }

    public override void _PhysicsProcess(double delta)
    {
        // if any required parts missing, stop running
        if (_engine is null || _battery is null || _starter is null)
        {
            _running = false;
        }

        HandleEngine();
    }

    public void HandleEngine()
    {
        if (_engineId != 0 && _engine is null)
        {
            _engine = _engineMount.GetPart() as Engine;
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
}
