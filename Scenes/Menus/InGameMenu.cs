using Godot;
using System;

public partial class InGameMenu : Menu
{
    MultiplayerController multiplayerController;
    GameManager gameManager;

    public override void _Ready()
    {
        menuType = MenuHandler.MenuType.ingamemenu;
        multiplayerController = GetNode<MultiplayerController>("/root/MultiplayerController");
        gameManager = GetNode<GameManager>("/root/GameManager");
    }

    public void _on_resume_pressed()
    {
        menuHandler.OpenMenu(MenuHandler.MenuType.none);
    }

    public void _on_mainmenu_pressed()
    {
        menuHandler.OpenMenu(MenuHandler.MenuType.mainmenu);
        multiplayerController.Disconnect();
    }

    public void _on_settings_pressed()
    {
        menuHandler.OpenMenu(MenuHandler.MenuType.settings);
    }

    public void _on_quit_pressed()
    {
        GetTree().Quit();
    }

    public void _on_respawn_pressed()
    {
        gameManager.Respawn();
    }
}
