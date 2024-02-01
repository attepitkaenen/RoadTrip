using Godot;
using System;
using System.Linq;

public partial class SceneManager : Node3D
{
	MultiplayerController multiplayerController;

	public override void _EnterTree()
	{
		multiplayerController = GetTree().Root.GetNode<MultiplayerController>("MultiplayerController");
	}

    public override void _Ready()
    {
		multiplayerController.isGameStarted = true;
        multiplayerController.RpcId(1, nameof(multiplayerController.PlayerLoaded)); 
    }

    public override void _ExitTree()
    {
        multiplayerController.CloseConnection();
    }
}
