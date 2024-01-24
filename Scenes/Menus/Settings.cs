using Godot;
using System;

public partial class Settings : Menu
{
    [Export] private LineEdit sensitivity;
    MultiplayerController multiplayerController;

    public override void _Ready()
    {
        menuType = MenuHandler.MenuType.settings;
        sensitivity.TextChanged += ChangeSensitivity;
        multiplayerController = GetNode<MultiplayerController>("/root/MultiplayerController");
        
    }

    private void ChangeSensitivity(string newText)
    {
        GameManager.Sensitivity = int.Parse(newText);
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
}
