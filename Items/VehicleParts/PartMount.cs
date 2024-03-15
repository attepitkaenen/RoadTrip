using System;
using Godot;


public partial class PartMount : Node3D
{
    GameManager gameManager;
    [Export] MultiplayerSynchronizer multiplayerSynchronizer;

    [Signal] public delegate void PartInstalledEventHandler(int id, float condition, string partType);
    [Signal] public delegate void PartUninstalledEventHandler();
    [Signal] public delegate void PartChangedEventHandler(int id, float condition, string partType);

    [ExportGroup("Installable part properties")]
    IMounted _part;
    private Area3D _partArea;
    [Export] private int _partId;
    [Export] private float _partCondition;
    [Export] public ItemTypeEnum partType = ItemTypeEnum.Engine;


    public override void _Ready()
    {
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
        if (multiplayerSynchronizer is null)
        {
            multiplayerSynchronizer = GetParent().GetParent().GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer");
        }
        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_partId");
        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_partCondition");

        _partArea = GetNode<Area3D>("Area3D");
        _partArea.BodyEntered += PartEntered;
        _partArea.BodyExited += PartExited;
        EmitSignal(SignalName.PartChanged, _partId, _partCondition, partType.ToString());
    }

    public override void _PhysicsProcess(double delta)
    {
        HandlePart();
    }

    public int GetPartId()
    {
        return _partId;
    }

    public float GetPartCondition()
    {
        return _partCondition;
    }

    public dynamic GetPart()
    {
        return _part;
    }

    // Make part eligible to be installed
    private void PartEntered(Node3D body)
    {
        if (body is Item item)
        {
            if (item.type == partType)
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
    }

    private void PartExited(Node3D body)
    {
        if (body is Item item)
        {
            if (item.type == partType)
            {
                var part = body as Installable;

                part.InstallPart -= InstallPart;
                part.canBeInstalled = false;
            }
        }
    }

    // Spawns installed part and sets its condition and id
    public IMounted SpawnInstalledPart(int id, float condition, Vector3 partPosition, Vector3 partRotation)
    {
        var part = gameManager.GetItemResource(id).ItemInHand.Instantiate() as IMounted;
        AddChild((dynamic)part, true);
        part.SetMount(this);
        part.SetCondition(condition);
        part.SetId(id);
        return part;
    }

    // Handles part removing
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RemoveInstalledPart(int id, float condition, Vector3 position, Vector3 rotation)
    {
        if (_partId == 0) return;
        _partId = 0;
        _partCondition = 0;
        EmitSignal(SignalName.PartChanged, 0, 0, partType.ToString());

        if (IsMultiplayerAuthority())
        {
            gameManager.RpcId(1, nameof(gameManager.SpawnItem), 0, id, condition, position, rotation);
        }
        _part.QueueFree();
    }

    private void InstallPart(int id, float condition)
    {
        if (_partId == 0)
        {
            GD.Print("Installing part with id: " + id);
            Rpc(nameof(SetPartIdAndCondition), id, condition);
        }
    }

    public void HandlePart()
    {
        if (_partId != 0 && _part is null)
        {
            _part = SpawnInstalledPart(_partId, _partCondition, Position, GlobalRotation);
        }
        else if (_partId == 0 && _part is not null)
        {
            _part = null;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetPartIdAndCondition(int id, float condition)
    {
        EmitSignal(SignalName.PartChanged, id, condition, partType.ToString());
        _partCondition = condition;
        _partId = id;
    }
}
