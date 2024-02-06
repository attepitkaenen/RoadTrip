using System;
using System.Reflection.Metadata;
using Godot;
using Godot.Collections;

public partial class EngineBay : Node3D
{
    [Export] MultiplayerSpawner multiplayerSpawner;
    GameManager gameManager;

    // private RayCast3D _engineCast;
    // private Marker3D _engineMarker;
    private Engine _engine;
    private Area3D _engineArea;
    private int _engineId;
    private float _engineCondition;

    private Carburetor _carburetor;
    private Battery _battery;
    private Alternator _alternator;
    private Radiator _radiator;
    private Intake _intake;

    Array<PartHandler> partHandlers = new Array<PartHandler>();

    public override void _Ready()
    {
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
        var engineNode = GetNode<Node3D>("Engine");
        // _engineCast = engineNode.GetNode<RayCast3D>("RayCast3D");
        // _engineMarker = engineNode.GetNode<Marker3D>("Marker3D");
        _engineArea = engineNode.GetNode<Area3D>("Area3D");
        _engineArea.BodyEntered += PartEntered;
        _engineArea.BodyExited += PartExited;
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleEngine();
    }

    private void PartExited(Node3D body)
    {
        if (body is EngineDropped engine)
        {
            engine.InstallPart -= InstallEngine;
            engine.isInstallable = false;
        }
    }

    private void PartEntered(Node3D body)
    {
        if (body is EngineDropped engine)
        {
            engine.InstallPart += InstallEngine;
            engine.isInstallable = true;
        }
    }

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
        if (_engine.itemId == itemId)
        {
            _engineId = 0;
        }
        else if (_carburetor.itemId == itemId)
        {
            _carburetor = null;
        }
        else if (_alternator.itemId == itemId)
        {
            _alternator = null;
        }
        else if (_battery.itemId == itemId)
        {
            _battery = null;
        }
        else if (_radiator.itemId == itemId)
        {
            _radiator = null;
        }
        else if (_intake.itemId == itemId)
        {
            _intake = null;
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
}
