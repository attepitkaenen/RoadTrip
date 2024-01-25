using Godot;
using System;

public partial class InGameMenu : Menu
{
    MultiplayerController multiplayerController;

    public override void _Ready()
    {
        menuType = MenuHandler.MenuType.ingamemenu;
        multiplayerController = GetNode<MultiplayerController>("/root/MultiplayerController");
    }

    public void _on_resume_pressed()
    {
        menuHandler.OpenMenu(MenuHandler.MenuType.none);
    }

    public void _on_mainmenu_pressed()
    {
        menuHandler.OpenMenu(MenuHandler.MenuType.mainmenu);
        multiplayerController.ResetGameState();
    }

    public void _on_settings_pressed()
    {
        menuHandler.OpenMenu(MenuHandler.MenuType.settings);
    }

    public void _on_quit_pressed()
    {
        GetTree().Quit();
    }
}
