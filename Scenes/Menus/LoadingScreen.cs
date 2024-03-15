using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using Godot;
using Godot.Collections;

public partial class LoadingScreen : Control
{
    GameManager gameManager;
    MultiplayerController multiplayerController;
    [Export] Label statusLabel;
    [Export] TextureRect icon;
    ResourceLoader.ThreadLoadStatus sceneLoadStatus = 0;
    string sceneName = "";
    Array progress = new Array();
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        GetTree().Paused = true;
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
        multiplayerController = GetTree().Root.GetNode<MultiplayerController>("MultiplayerController");
        sceneName = "res://Scenes/World.tscn";
        ResourceLoader.LoadThreadedRequest(sceneName);
    }

    public override void _Process(double delta)
    {
        icon.RotationDegrees += 0.1f;
        sceneLoadStatus = ResourceLoader.LoadThreadedGetStatus(sceneName, progress);
        if (progress is not null && sceneLoadStatus != ResourceLoader.ThreadLoadStatus.Loaded)
        {
            statusLabel.Text = Mathf.Round((float)progress[0] * 100).ToString() + "%";
        }

        if (sceneLoadStatus == ResourceLoader.ThreadLoadStatus.Loaded && multiplayerController.GetPlayerStates()[Multiplayer.GetUniqueId()].IsLoading && !Multiplayer.IsServer())
        {
            GD.Print("Loading done client");
            multiplayerController.Rpc(nameof(multiplayerController.SetLoadingStatus), false);
            statusLabel.Text = "Waiting for server";
        }

        if (sceneLoadStatus == ResourceLoader.ThreadLoadStatus.Loaded && Multiplayer.IsServer())
        {
            GD.Print("Loading done server");
            var newScene = (ResourceLoader.LoadThreadedGet(sceneName) as PackedScene).Instantiate();
            gameManager.AddChild(newScene, true);
            gameManager.world = newScene as SceneManager;
            multiplayerController.Rpc(nameof(multiplayerController.SetLoadingStatus), false);
            GetTree().Paused = false;
            Rpc(nameof(ServerLoaded));
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void ServerLoaded()
    {
        multiplayerController.isGameStarted = true;
        multiplayerController.RpcId(1, nameof(multiplayerController.PlayerLoaded)); 
        GetTree().Paused = false;
        QueueFree();
    }
}

