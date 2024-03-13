using System.Linq;
using System.Reflection.Metadata;
using Godot;
using Godot.Collections;

public partial class LoadingScreen : Control
{
    GameManager gameManager;
    [Export] Label statusLabel;
    [Export] TextureRect icon;
    ResourceLoader.ThreadLoadStatus sceneLoadStatus = 0;
    string sceneName = "";
    Array progress = new Array();
    public override void _Ready()
    {
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
        sceneName = "res://Scenes/World.tscn";
        ResourceLoader.LoadThreadedRequest(sceneName);
    }

    public override void _Process(double delta)
    {
        icon.RotationDegrees += 0.1f;
        sceneLoadStatus = ResourceLoader.LoadThreadedGetStatus(sceneName, progress);
        if (progress is not null)
        {
            statusLabel.Text = Mathf.Round((float)progress[0] * 100).ToString() + "%";
        }
        if (sceneLoadStatus == ResourceLoader.ThreadLoadStatus.Loaded)
        {
            GD.Print("Loading done");
            var newScene = (ResourceLoader.LoadThreadedGet(sceneName) as PackedScene).Instantiate();
            gameManager.AddChild(newScene, true);
            gameManager.world = newScene as SceneManager;
            QueueFree();
        }
    }
}
