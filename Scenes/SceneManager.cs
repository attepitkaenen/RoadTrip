using Godot;
using System;
using System.Linq;

public partial class SceneManager : Node3D
{
	[Export]
	private PackedScene playerScene;
	// Called when the node enters the scene tree for the first time.

	public override void _Ready()
	{
		if (Multiplayer.IsServer())
		{
			foreach (var item in GameManager.Players)
			{
				Player currentPlayer = playerScene.Instantiate<Player>();
				currentPlayer.Name = item.Id.ToString();
				AddChild(currentPlayer);
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressed("menu"))
		{
			GetTree().Quit();
		}
	}
}
