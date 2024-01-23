using Godot;
using System;
using System.Linq;

public partial class MainMenu : Control
{
    MultiplayerController multiplayerController;

    [Export] private LineEdit address;
    [Export] private LineEdit userName;
    [Export] private ItemList playerList;

    public override void _Ready()
    {
        multiplayerController = GetNode<MultiplayerController>("/root/MultiplayerController");
        userName.TextChanged += SaveUserName;
        playerList.AddItem(userName.Text);
    }

    public override void _Process(double delta)
    {
        if (multiplayerController.GetGameStartedStatus())
        {
            Hide();
        }
        else 
        {
            Visible = true;
        }

        UpdateLobbyNames();

    }

    public void UpdateLobbyNames()
    {
        while (playerList.ItemCount < GameManager.Players.Count)
        {
            playerList.AddItem("");
        }
        var index = 0;
        GameManager.Players.ForEach(player => {
            playerList.SetItemText(index, player.Name);
            index++;
        });
    }

    private void SaveUserName(string newText)
    {
        multiplayerController.SetUserName(newText);
    }

    public void _on_host_pressed()
    {
        multiplayerController.OnHostPressed();
    }

    public void _on_join_pressed()
    {
        multiplayerController.OnJoinPressed(address.Text);
    }
    public void _on_start_pressed()
    {
        multiplayerController.OnStartPressed();
    }
}
