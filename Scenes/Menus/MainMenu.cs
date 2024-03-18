using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MainMenu : Menu
{
    MultiplayerController multiplayerController;
    GameManager gameManager;
    SaveManager saveManager;

    [Export] private LineEdit address;
    [Export] private LineEdit userName;
    [Export] private LineEdit saveName;
    [Export] private ItemList playerList;
    [Export] private MenuButton _saveList;
    [Export] private VBoxContainer buttons;
    [Export] private PanelContainer singleplayerContainer;
    [Export] private PanelContainer multiplayerContainer;
    [Export] private PanelContainer lobbyContainer;
    [Export] private VBoxContainer multiplayerButtons;

    public override void _Ready()
    {
        menuType = MenuHandler.MenuType.mainmenu;
        multiplayerController = GetTree().Root.GetNode<MultiplayerController>("MultiplayerController");
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
        saveManager = GetTree().Root.GetNode<SaveManager>("SaveManager");
        userName.TextChanged += SaveUserName;
        multiplayerController.PlayerConnected += UpdateLobbyNames;
        Multiplayer.ConnectionFailed += ConnectionFailed;
        Multiplayer.ServerDisconnected += ConnectionFailed;

        var saves = saveManager.GetSaves();
        foreach (string saveName in saves)
        {
            GD.Print(saveName);
            _saveList.GetPopup().AddCheckItem(saveName);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (multiplayerController.GetPlayerStates().Count > 0)
        {
            lobbyContainer.Visible = true;
        }
        else
        {
            lobbyContainer.Visible = false;
        }

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

    public void _on_singleplayer_pressed()
    {
        multiplayerController.maxConnections = 1;
        singleplayerContainer.Visible = !singleplayerContainer.Visible;
        multiplayerContainer.Visible = false;
    }

    public void _on_multiplayer_pressed()
    {
        multiplayerController.maxConnections = 20;
        multiplayerContainer.Visible = !multiplayerContainer.Visible;
        singleplayerContainer.Visible = false;
    }

    public void _on_settings_pressed()
    {
        GD.Print("Settings pressed");
        menuHandler.OpenMenu(MenuHandler.MenuType.settings);
    }

    public void _on_host_pressed()
    {
        ToggleHostAndJoinDisabled(false);
        multiplayerController.CreateGame();
    }

    public void _on_join_pressed()
    {
        multiplayerController.JoinGame(address.Text);
        ToggleJoined(false);
    }
    public void _on_new_game_pressed()
    {
        menuHandler.OpenMenu(MenuHandler.MenuType.loading);
        saveManager.LoadGame(saveName.Text);
    }

    public void _on_load_game_pressed()
    {
        menuHandler.OpenMenu(MenuHandler.MenuType.loading);
        saveManager.LoadGame(saveName.Text);
    }

    public void ToggleHostAndJoinDisabled(bool state)
    {
        buttons.GetNode<Button>("Singleplayer").Visible = state;
        buttons.GetNode<Button>("Multiplayer").Visible = state;
        buttons.GetNode<Button>("Leave").Visible = !state;
        multiplayerButtons.GetNode<HBoxContainer>("Hosted").Visible = !state;
        multiplayerButtons.GetNode<Button>("Host").Visible = state;
        multiplayerButtons.GetNode<Button>("Join").Visible = state;
    }

    public void ToggleJoined(bool state)
    {
        buttons.GetNode<Button>("Singleplayer").Visible = state;
        buttons.GetNode<Button>("Multiplayer").Visible = state;
        buttons.GetNode<Button>("Leave").Visible = !state;
        multiplayerContainer.Visible = state;
        multiplayerButtons.GetNode<Button>("Host").Visible = state;
        multiplayerButtons.GetNode<Button>("Join").Visible = state;
    }

    public void _on_quit_pressed()
    {
        GetTree().Quit();
    }
}
