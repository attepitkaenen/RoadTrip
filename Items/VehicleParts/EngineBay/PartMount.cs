using System;
using Godot;


public partial class PartMount : Node3D, IMount
{
    GameManager gameManager;

    [Signal] public delegate void PartInstalledEventHandler(int itemId, float condition);
    [Signal] public delegate void PartUninstalledEventHandler();

    [ExportGroup("Installable part properties")]
    CarPart _carPart;
    private Area3D _engineArea;
    [Export] private int _partId;
    [Export] private float _partCondition;
    [Export] public TypeEnum partType = TypeEnum.EngineDropped;

    public enum TypeEnum
    {
        EngineDropped,
        AlternatorDropped,
        BatteryDropped,
        RadiatorDropped,
        FuelInjectorDropped,
        IntakeDropped,
        StarterDropped,
        WaterTankDropped,
        WindshieldDropped,
        DoorDropped,
        TireDropped
    }

    public override void _Ready()
    {
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");

        _engineArea = GetNode<Area3D>("Area3D");
        _engineArea.BodyEntered += PartEntered;
        _engineArea.BodyExited += PartExited;
    }

    public override void _PhysicsProcess(double delta)
    {
        HandlePart();
    }

    public string GetPartIdPath()
    {
        return GetPath() + ":_partId";
    }

    public string GetPartConditionPath()
    {
        return GetPath() + ":_partCondition";
    }

    public CarPart GetPart()
    {
        return _carPart;
    }

    // Make part eligible to be installed
    private void PartEntered(Node3D body)
    {
        var type = body.GetType().ToString();
        GD.Print(type);
        if (type == partType.ToString())
        {
            GD.Print("Correct type!");
            var part = body as Installable;
            if (part.canBeInstalled == false)
            {
                part.InstallPart += InstallPart;
                part.canBeInstalled = true;
            }
        }
    }

    private void PartExited(Node3D body)
    {
        var type = body.GetType().ToString();
        if (type == partType.ToString())
        {
            var part = body as Installable;
            if (part.canBeInstalled == false)
            {
                part.InstallPart += InstallPart;
                part.canBeInstalled = true;
            }
        }
    }

    // Spawns installed part and sets its condition and itemId
    public CarPart SpawnInstalledPart(int itemId, float condition, Vector3 partPosition)
    {
        var part = gameManager.GetItemResource(itemId).ItemInHand.Instantiate() as CarPart;
        AddChild(part);
        part.SetMount(this);
        part.SetCondition(condition);
        part.SetId(itemId);
        part.Position = partPosition;
        return part;
    }

    // Handles part removing
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RemoveInstalledPart(int itemId, float condition, Vector3 position)
    {
        _partId = 0;
        EmitSignal(SignalName.PartUninstalled);

        if (IsMultiplayerAuthority())
        {
            gameManager.RpcId(1, nameof(gameManager.SpawnPart), itemId, condition, position, GlobalRotation);
        }
    }

    private void InstallPart(int itemId, float condition)
    {
        if (_partId == 0)
        {
            GD.Print("Installing part with id: " + itemId);
            Rpc(nameof(SetPartIdAndCondition), itemId, condition);
        }
    }

    public void HandlePart()
    {
        if (_partId != 0 && _carPart is null)
        {
            _carPart = SpawnInstalledPart(_partId, _partCondition, _engineArea.Position);
        }
        else if (_partId == 0 && _carPart is not null)
        {
            _carPart = null;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetPartIdAndCondition(int id, float condition)
    {
        EmitSignal(SignalName.PartInstalled, id, condition);
        _partCondition = condition;
        _partId = id;
    }
}
