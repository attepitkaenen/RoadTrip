using Godot;
using System;

public partial class SceneManager : Node3D
{
	[Export]
	private PackedScene playerScene;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if(Multiplayer.IsServer())
		{
			int index = 0;
			foreach (var item in GameManager.Players)
			{
				
				Player currentPlayer = playerScene.Instantiate<Player>();
				currentPlayer.Name = item.Id.ToString();
				// currentPlayer.SetUpPlayer(item.Name);
				AddChild(currentPlayer);
				foreach (Node3D spawnPoint in GetTree().GetNodesInGroup("SpawnPoints"))
				{
					if(int.Parse(spawnPoint.Name) == index){
						currentPlayer.GlobalPosition = spawnPoint.GlobalPosition;
					}
				}
				index ++;
			}
		}
	}
}
