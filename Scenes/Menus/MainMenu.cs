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
        multiplayerController.PlayerConnected += UpdateLobbyNames;
        Multiplayer.ConnectionFailed += ConnectionFailed;
        Multiplayer.ServerDisconnected += ConnectionFailed;
        buttons = GetNode("Buttons").GetChildren().Select(node => node as Button).ToList();
    }

    private void ConnectionFailed()
    {
        ToggleHostAndJoinDisabled(false);
        playerList.Clear();
    }

    public void UpdateLobbyNames(long peerId, PlayerState peerName)
    {
        playerList.Clear();
        while (playerList.ItemCount < multiplayerController.GetPlayerStates().Count)
        {
            playerList.AddItem("");
        }
        var index = 0;
        foreach (var (id, playerState) in multiplayerController.GetPlayerStates())
        {
            playerList.SetItemText(index, playerState.Name);
            index++;
        }
    }

    private void SaveUserName(string newText)
    {
        multiplayerController.UpdateUserName(newText);
    }

    public void _on_settings_pressed()
    {
        GD.Print("Settings pressed");
        MenuHandler.MenuType menuType = MenuHandler.MenuType.settings;
        menuHandler.OpenMenu(menuType);
    }

    public void _on_host_pressed()
    {
        ToggleHostAndJoinDisabled(true);
        multiplayerController.CreateGame();
    }

    public void _on_join_pressed()
    {
        multiplayerController.JoinGame(address.Text);
        // ToggleHostAndJoinDisabled(true);
    }
    public void _on_start_pressed()
    {
        ToggleHostAndJoinDisabled(false);
        multiplayerController.Rpc(nameof(multiplayerController.LoadGame));
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
