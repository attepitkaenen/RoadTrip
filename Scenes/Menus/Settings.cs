using Godot;
using System;

public partial class Settings : Menu
{
    [Export] private LineEdit sensitivity;

    GameManager gameManager;
    RiptideClient riptideClient;

    public override void _Ready()
    {
        menuType = MenuHandler.MenuType.settings;
        sensitivity.TextChanged += ChangeSensitivity;

        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
        riptideClient = GetTree().Root.GetNode<RiptideClient>("RiptideClient");
    }

    private void ChangeSensitivity(string newText)
    {
        gameManager.Sensitivity = int.Parse(newText);
    }

    public void _on_back_button_pressed()
    {
        GD.Print("Back button pressed");
        if (GameManager.isGameStarted)
        {
            MenuHandler.OpenMenu(MenuHandler.MenuType.ingamemenu);
        }
        else
        {
            GD.Print("Should go to main menu");

            MenuHandler.OpenMenu(MenuHandler.MenuType.mainmenu);
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
