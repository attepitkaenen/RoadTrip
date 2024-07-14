using Godot;
using Riptide;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

public partial class MainMenu : Menu
{
    GameManager gameManager;
    SaveManager saveManager;
    RiptideServer riptideServer;
    RiptideClient riptideClient;

    [Export] private LineEdit ipAddress;
    [Export] private LineEdit userName;
    [Export] private LineEdit saveName;
    [Export] private ItemList playerList;
    [Export] private MenuButton _saveList;
    [Export] private VBoxContainer buttons;
    [Export] private PanelContainer singleplayerContainer;
    [Export] private PanelContainer multiplayerContainer;
    [Export] private PanelContainer lobbyContainer;
    [Export] private VBoxContainer multiplayerButtons;
    [Export] private MenuButton menuButton;
    private string _pickedSave;

    public override void _Ready()
    {
        menuType = MenuHandler.MenuType.mainmenu;
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
        saveManager = GetTree().Root.GetNode<SaveManager>("SaveManager");
        riptideServer = GetTree().Root.GetNode<RiptideServer>("RiptideServer");
        riptideClient = GetTree().Root.GetNode<RiptideClient>("RiptideClient");

        menuButton.GetPopup().IdPressed += SaveChosen;

        var saves = saveManager.GetSaves();
        foreach (string saveName in saves)
        {
            GD.Print(saveName);
            _saveList.GetPopup().AddCheckItem(saveName);
        }


    }

    public override void _PhysicsProcess(double delta)
    {
        // if (multiplayerController.GetPlayerStates().Count > 0)
        // {
        //     lobbyContainer.Visible = true;
        // }
        // else
        // {
        //     lobbyContainer.Visible = false;
        // }

    }

    private void ConnectionFailed()
    {
        ToggleHostAndJoinDisabled(false);
        playerList.Clear();
    }

    public void UpdateLobbyNames(long peerId, PlayerState peerName)
    {
        playerList.Clear();
        // while (playerList.ItemCount < multiplayerController.GetPlayerStates().Count)
        // {
        //     playerList.AddItem("");
        // }
        // var index = 0;
        // foreach (var (id, playerState) in multiplayerController.GetPlayerStates())
        // {
        //     playerList.SetItemText(index, playerState.Name);
        //     index++;
        // }
    }

    private void SaveChosen(long id)
    {
        _pickedSave = menuButton.GetPopup().GetItemText((int)id);
        GD.Print(_pickedSave);
    }

    public void _on_singleplayer_pressed()
    {
        singleplayerContainer.Visible = !singleplayerContainer.Visible;
        multiplayerContainer.Visible = false;
    }

    public void _on_multiplayer_pressed()
    {
        multiplayerContainer.Visible = !multiplayerContainer.Visible;
        singleplayerContainer.Visible = false;
    }

    public void _on_settings_pressed()
    {
        GD.Print("Settings pressed");
        MenuHandler.OpenMenu(MenuHandler.MenuType.settings);
    }

    public void _on_leave_pressed()
    {
        riptideClient.Disconnect();

        if (riptideClient.IsHost())
        {
            riptideServer.StopServer();
        }
    }

    public void _on_host_pressed()
    {
        ToggleHostAndJoinDisabled(false);

        riptideServer.Host(10);

        if (userName.Text == "")
        {
            riptideClient.Connect(ipAddress.Text, 25565, "Jorma", true);
        }
        else
        {
            riptideClient.Connect(ipAddress.Text, 25565, userName.Text, true);
        }
    }

    public void _on_join_pressed()
    {
        ToggleJoined(false);

        if (userName.Text == "")
        {
            riptideClient.Connect(ipAddress.Text, 25565, "Jorma");
        }
        else
        {
            riptideClient.Connect(ipAddress.Text, 25565, userName.Text);
        }
    }


    public void _on_new_game_pressed()
    {
        saveManager.LoadGame(saveName.Text);
    }

    public void _on_load_game_pressed()
    {
        saveManager.LoadGame(saveName.Text);
    }

    public void _on_menu_button_pressed()
    {

    }

    // Toggle if player has hosted a game successfully
    public void ToggleHostAndJoinDisabled(bool state)
    {
        buttons.GetNode<Button>("Singleplayer").Visible = state;
        buttons.GetNode<Button>("Multiplayer").Visible = state;
        buttons.GetNode<Button>("Leave").Visible = !state;
        multiplayerButtons.GetNode<HBoxContainer>("Hosted").Visible = !state;
        multiplayerButtons.GetNode<Button>("Host").Visible = state;
        multiplayerButtons.GetNode<Button>("Join").Visible = state;
    }

    // Toggle if player has joined a game successfully
    public void ToggleJoined(bool state)
    {
        buttons.GetNode<Button>("Singleplayer").Visible = state;
        buttons.GetNode<Button>("Multiplayer").Visible = state;
        buttons.GetNode<Button>("Leave").Visible = !state;
        multiplayerContainer.Visible = state;
        multiplayerButtons.GetNode<Button>("Host").Visible = state;
        multiplayerButtons.GetNode<Button>("Join").Visible = state;
    }

    // Toggle this if connection lost or left game
    public void ResetMenu()
    {
        GD.Print("Resetting menu");
        multiplayerContainer.Visible = false;
        singleplayerContainer.Visible = false;
        buttons.GetNode<Button>("Singleplayer").Visible = true;
        buttons.GetNode<Button>("Multiplayer").Visible = true;
        buttons.GetNode<Button>("Leave").Visible = false;
        multiplayerButtons.GetNode<HBoxContainer>("Hosted").Visible = false;
        multiplayerButtons.GetNode<Button>("Host").Visible = true;
        multiplayerButtons.GetNode<Button>("Join").Visible = true;
    }

    public void _on_quit_pressed()
    {
        GetTree().Quit();
    }
}
