using System;
using System.Reflection.Metadata;
using Godot;
using Godot.Collections;

public partial class EngineBay : Node3D
{
    [Export] MultiplayerSpawner multiplayerSpawner;
    GameManager gameManager;

    private RayCast3D _engineCast;
    private Marker3D _engineMarker;
    private Engine _engine;

    private Carburetor _carburetor;
    private Battery _battery;
    private Alternator _alternator;
    private Radiator _radiator;
    private Intake _intake;

    Array<PartHandler> partHandlers = new Array<PartHandler>();

    public override void _Ready()
    {
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
        // Callable spawnCallable = new Callable(this, MethodName.SpawnPart);
        // multiplayerSpawner.SpawnFunction = spawnCallable;
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleEngine();
    }

    public void HandleEngine()
    {
        if (_engineCast is null || _engineMarker is null)
        {
            var engineNode = GetNode<Node3D>("Engine");
            _engineCast = engineNode.GetNode<RayCast3D>("RayCast3D");
            _engineMarker = engineNode.GetNode<Marker3D>("Marker3D");
        }

        if (_engineCast.GetCollider() is EngineDropped engine)
        {
            engine.InstallPart += InstallEngine;
            engine.isInstallable = true;
        }
    }

    private void InstallEngine(int itemId, float condition)
    {
        if (_engine is null)
        {
            Rpc(nameof(SpawnInstalledPart), itemId, condition, _engineMarker.Position);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SetPart(string name)
    {
        var part = GetNode(name);
        
        if (part is Engine engine)
        {
            _engine = engine;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SpawnInstalledPart(int itemId, float condition, Vector3 partPosition)
    {
        var part = gameManager.GetItemResource(itemId).ItemInHand.Instantiate() as CarPart;
        AddChild(part);
        part.SetCondition(condition);
        part.itemId = itemId;
        part.Position = partPosition;
        Rpc(nameof(SetPart), part.Name);
    }

    public void RemoveInstalledPart(int itemId, float condition, Vector3 position)
    {
        if (_engine.itemId == itemId)
        {
            _engine = null;
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

    public void GetHorsePower()
    {

    }
}
