using Godot;

public partial class LoadingScreen : Menu
{
    GameManager gameManager;
    MultiplayerController multiplayerController;
    SaveManager saveManager;
    [Export] Label statusLabel;
    [Export] TextureRect icon;

    public override void _Ready()
    {
        menuType = MenuHandler.MenuType.loading;
        ProcessMode = ProcessModeEnum.Always;
        saveManager = GetTree().Root.GetNode<SaveManager>("SaveManager");
        multiplayerController = GetTree().Root.GetNode<MultiplayerController>("MultiplayerController");
    }

    public override void _Process(double delta)
    {
        var progress = saveManager.progress;
        icon.RotationDegrees += 0.5f;
        if (multiplayerController.GetPlayerState().IsLoading && progress.Count > 0)
        {
            statusLabel.Text = Mathf.Round((float)progress[0] * 100).ToString() + "%";
        }
    }
}

