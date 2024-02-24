using System;
using System.Reflection.Metadata;
using Godot;
using Godot.Collections;

public partial class WindshieldMount : Node3D, IMount
{
    MultiplayerSynchronizer multiplayerSynchronizer;
    GameManager gameManager;

    private bool _running;

    [ExportGroup("Windshield properties")]
    private Windshield _windshield;
    private Area3D _windshieldArea;
    [Export] private int _windshieldId;
    [Export] private float _windshieldCondition;



    public override void _Ready()
    {
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
        multiplayerSynchronizer = GetParent().GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer");

        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_windshieldId");
        multiplayerSynchronizer.ReplicationConfig.AddProperty(GetPath() + ":_windshieldCondition");

        _windshieldArea = GetNode<Area3D>("Area3D");
        _windshieldArea.BodyEntered += PartEntered;
        _windshieldArea.BodyExited += PartExited;
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleWindshield();
    }

    // General part handling

    // Make part not eligible to be installed
    private void PartExited(Node3D body)
    {
        if (body is WindshieldDropped windshield)
        {
            windshield.InstallPart -= InstallWindshield;
            windshield.canBeInstalled = false;
        }
    }

    // Make part eligible to be installed
    private void PartEntered(Node3D body)
    {
        if (body is WindshieldDropped windshield)
        {
            GD.Print("Windshield entered!");
            windshield.InstallPart += InstallWindshield;
            windshield.canBeInstalled = true;
        }
    }

    // Spawns installed part and sets its condition and itemId
    public Windshield SpawnInstalledPart(int itemId, float condition, Vector3 partPosition)
    {
        var part = gameManager.GetItemResource(itemId).ItemInHand.Instantiate() as Windshield;
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
        if (_windshieldId == itemId)
        {
            _windshieldId = 0;
        }
        gameManager.RpcId(1, nameof(gameManager.SpawnPart), itemId, condition, position, GlobalRotation);
    }

    // Windshield
    private void InstallWindshield(int itemId, float condition)
    {
        if (_windshieldId == 0)
        {
            Rpc(nameof(SetWindshieldIdAndCondition), itemId, condition);
        }
    }

    public void HandleWindshield()
    {
        if (_windshieldId != 0 && _windshield is null)
        {
            _windshield = SpawnInstalledPart(_windshieldId, _windshieldCondition, _windshieldArea.Position);
        }
        else if (_windshieldId == 0 && _windshield is not null)
        {
            _windshield = null;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetWindshieldIdAndCondition(int id, float condition)
    {
        _windshieldCondition = condition;
        _windshieldId = id;
    }
}
