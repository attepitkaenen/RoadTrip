using Godot;
using System;
using System.Linq;

public partial class SceneManager : Node3D
{
	[Export] private PackedScene playerScene;
	GameManager gameManager;
	MultiplayerController multiplayerController;
	// Called when the node enters the scene tree for the first time.

	public override void _EnterTree()
	{
		gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
		multiplayerController = GetTree().Root.GetNode<MultiplayerController>("MultiplayerController");
	}
}
