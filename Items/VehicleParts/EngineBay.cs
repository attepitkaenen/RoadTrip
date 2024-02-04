using System;
using Godot;
using Godot.Collections;

public partial class EngineBay : Node3D
{
    [Export] MultiplayerSpawner multiplayerSpawner;
    GameManager gameManager;

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
        GetNode<PartHandler>("Engine").PartInstalled += PartInstalled;
        Callable spawnCallable = new Callable(this, MethodName.SpawnPart);
        multiplayerSpawner.SpawnFunction = spawnCallable;
    }

    void PartInstalled(int itemId, float condition, string markerPath)
    {
        GD.Print("Part installed");
        RpcId(1, nameof(SpawnInstalledPart), itemId, condition, markerPath);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SpawnInstalledPart(int itemId, float condition, string markerPath)
	{
        var item = multiplayerSpawner.Spawn(itemId) as CarPart;
        item.SetCondition(condition);
        item.Position = GetNode<Marker3D>(markerPath).Position;
	}

    Node SpawnPart(int itemId)
    {
        var item = gameManager.GetItemResource(itemId).ItemInHand.Instantiate();
        return item;
    }

    public void GetHorsePower()
    {

    }
}
