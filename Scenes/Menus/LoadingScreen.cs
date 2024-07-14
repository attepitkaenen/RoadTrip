using Godot;

public partial class LoadingScreen : Menu
{
    GameManager gameManager;
    RiptideClient riptideClient;
    SaveManager saveManager;
    [Export] Label statusLabel;
    [Export] TextureRect icon;

    public override void _Ready()
    {
        menuType = MenuHandler.MenuType.loading;
        ProcessMode = ProcessModeEnum.Always;
        saveManager = GetTree().Root.GetNode<SaveManager>("SaveManager");
        riptideClient = GetTree().Root.GetNode<RiptideClient>("RiptideClient");
    }

    public override void _Process(double delta)
    {
        var progress = saveManager.progress;
        icon.RotationDegrees += 0.5f;
        if (riptideClient.IsLoading() && progress.Count > 0)
        {
            statusLabel.Text = Mathf.Round((float)progress[0] * 100).ToString() + "%";
        }
    }
}

