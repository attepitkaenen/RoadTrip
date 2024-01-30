using Godot;
using System;
using System.Linq;

public partial class SceneManager : Node3D
{
	GameManager gameManager;

	public override void _EnterTree()
	{
		gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
	}
}
