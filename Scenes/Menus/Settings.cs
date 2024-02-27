using Godot;
using System;

public partial class Settings : Menu
{
    [Export] private LineEdit sensitivity;
    MultiplayerController multiplayerController;
    GameManager gameManager;

    public override void _Ready()
    {
        menuType = MenuHandler.MenuType.settings;
        sensitivity.TextChanged += ChangeSensitivity;
        multiplayerController = GetNode<MultiplayerController>("/root/MultiplayerController");
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");

    }

    private void ChangeSensitivity(string newText)
    {
        gameManager.Sensitivity = int.Parse(newText);
    }

    public void _on_back_button_pressed()
    {
        if (multiplayerController.GetGameStartedStatus())
        {
            menuHandler.OpenMenu(MenuHandler.MenuType.ingamemenu);
        }
        else
        {
            menuHandler.OpenMenu(MenuHandler.MenuType.mainmenu);
        }
    }

    public void _on_check_button_toggled(bool status)
    {
        GD.Print("switch toggled to:" + status);
        if (status)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
        }
        else
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        }
    }
}
