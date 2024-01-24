using Godot;
using System;
using System.Linq;

public partial class SceneManager : Node3D
{
	[Export] private PackedScene playerScene;
	GameManager gameManager;
    // Called when the node enters the scene tree for the first time.

    public override void _EnterTree()
    {
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
    }

    public override void _Ready()
	{
		if (Multiplayer.IsServer())
		{
			foreach (var playerState in gameManager.GetPlayerStates())
			{
				Player currentPlayer = playerScene.Instantiate<Player>();
				currentPlayer.Name = playerState.Id.ToString();
				AddChild(currentPlayer);
			}
		}
	}
}
