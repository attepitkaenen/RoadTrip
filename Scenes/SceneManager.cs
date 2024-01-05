using Godot;
using System;

public partial class SceneManager : Node3D
{
	[Export]
	private PackedScene playerScene;
	// Called when the node enters the scene tree for the first time.

	public override void _Ready()
	{
		if (Multiplayer.IsServer())
		{
			int index = 0;
			foreach (var item in GameManager.Players)
			{
				// foreach (Node3D spawnPoint in GetTree().GetNodesInGroup("SpawnPoints"))
				// {
				// 	if (int.Parse(spawnPoint.Name) == index)
				// 	{
				// 		item.SpawnLocation = spawnPoint.GlobalPosition;
				// 		// currentPlayer.spawnLocation = spawnPoint.GlobalPosition;
				// 	}
				// }
				// currentPlayer.SetUpPlayer(item.Name);
				Player currentPlayer = playerScene.Instantiate<Player>();
				currentPlayer.Name = item.Id.ToString();
				AddChild(currentPlayer);
				// currentPlayer.GlobalPosition = new Vector3(0, 3, index);
				index++;
			}
		}
	}
}
