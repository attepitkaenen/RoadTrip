using Godot;
using System;

public partial class InGameMenu : Menu
{
    MultiplayerController multiplayerController;
    GameManager gameManager;
    SaveManager saveManager;

    public override void _Ready()
    {
        menuType = MenuHandler.MenuType.ingamemenu;
        multiplayerController = GetTree().Root.GetNode<MultiplayerController>("MultiplayerController");
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
        saveManager = GetTree().Root.GetNode<SaveManager>("SaveManager");
    }

    public void _on_resume_pressed()
    {
        menuHandler.OpenMenu(MenuHandler.MenuType.none);
    }

    public void _on_mainmenu_pressed()
    {
        menuHandler.OpenMenu(MenuHandler.MenuType.mainmenu);
        multiplayerController.CloseConnection();
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
        Rpc(nameof(gameManager.Respawn), Multiplayer.GetUniqueId());
    }

    public void _on_save_pressed()
    {
        saveManager.SaveGame();
    }
}
