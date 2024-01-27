using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MainMenu : Menu
{
    MultiplayerController multiplayerController;
    GameManager gameManager;

    [Export] private LineEdit address;
    [Export] private LineEdit userName;
    [Export] private ItemList playerList;
    private List<Button> buttons;

    public override void _Ready()
    {
        menuType = MenuHandler.MenuType.mainmenu;
        multiplayerController = GetNode<MultiplayerController>("/root/MultiplayerController");
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
        userName.TextChanged += SaveUserName;
        gameManager.PlayerJoined += UpdateLobbyNames;
        Multiplayer.ConnectionFailed += ConnectionFailed;
        Multiplayer.ServerDisconnected += ConnectionFailed;
        buttons = GetNode("Buttons").GetChildren().Select(node => node as Button).ToList();
    }

    private void ConnectionFailed()
    {
        ToggleHostAndJoinDisabled(false);
    }

    public void UpdateLobbyNames(long id)
    {
        playerList.Clear();
        while (playerList.ItemCount < gameManager.GetPlayerStates().Count)
        {
            playerList.AddItem("");
        }
        var index = 0;
        gameManager.GetPlayerStates().ForEach(player =>
        {
            playerList.SetItemText(index, player.Name);
            index++;
        });
    }

    private void SaveUserName(string newText)
    {
        multiplayerController.SetUserName(newText);
    }

    public void _on_settings_pressed()
    {
        GD.Print("Settings pressed");
        MenuHandler.MenuType menuType = MenuHandler.MenuType.settings;
        menuHandler.OpenMenu(menuType);
    }

    public void _on_host_pressed()
    {
        GD.Print(buttons.Count);
        ToggleHostAndJoinDisabled(true);
        multiplayerController.OnHostPressed();
    }

    public void _on_join_pressed()
    {
        multiplayerController.OnJoinPressed(address.Text);
        // ToggleHostAndJoinDisabled(true);
    }
    public void _on_start_pressed()
    {
        ToggleHostAndJoinDisabled(false);
        multiplayerController.OnStartPressed();
    }

    public void ToggleHostAndJoinDisabled(bool state)
    {
        var joinButton = buttons.Find(button => button.Name == "Join");
        var hostButton = buttons.Find(button => button.Name == "Host");
        joinButton.Disabled = state;
        hostButton.Disabled = state;
    }

    public void _on_quit_pressed()
    {
        GetTree().Quit();
    }
}
