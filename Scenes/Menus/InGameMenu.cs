using Godot;
using System;

public partial class InGameMenu : Menu
{
    RiptideClient riptideClient;
    RiptideServer riptideServer;
    GameManager gameManager;
    SaveManager saveManager;

    public override void _Ready()
    {
        menuType = MenuHandler.MenuType.ingamemenu;
        riptideClient = GetTree().Root.GetNode<RiptideClient>("RiptideClient");
        riptideServer = GetTree().Root.GetNode<RiptideServer>("RiptideServer");
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
        saveManager = GetTree().Root.GetNode<SaveManager>("SaveManager");
    }

    public void _on_resume_pressed()
    {
        MenuHandler.OpenMenu(MenuHandler.MenuType.none);
    }

    public void _on_mainmenu_pressed()
    {
        MenuHandler.OpenMenu(MenuHandler.MenuType.mainmenu);
        riptideClient.Disconnect();

        if (RiptideClient.IsHost())
        {
            riptideServer.StopServer();
        }
    }

    public void _on_settings_pressed()
    {
        MenuHandler.OpenMenu(MenuHandler.MenuType.settings);
    }

    public void _on_quit_pressed()
    {
        GetTree().Quit();
    }

    public void _on_respawn_pressed()
    {
        gameManager.Respawn();
    }

    public void _on_save_pressed()
    {
        saveManager.SaveGame();
    }
}
