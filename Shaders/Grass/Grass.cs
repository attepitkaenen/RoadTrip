using Godot;
using System;
using System.Linq;

public partial class Grass : Node3D
{
	GameManager gameManager;
	Player player;
	Node scatter;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		gameManager = GetTree().Root.GetNode<GameManager>("GameManager");
		scatter = GetNode("ProtonScatter");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
				// GetNode("ProtonScatter").Set("enabled", true);

		// if (player is not null)
		// {
		// 	string status = scatter.Get("enabled").ToString();
		// 	// GD.Print(status);
		// 	if (GlobalPosition.DistanceTo(player.GlobalPosition) < 10 && status == "false")
		// 	{
		// 		// GD.Print(GetNode("ProtonScatter").Get("enabled"));
		// 		GD.Print("Closer than 10");
		// 		GetNode("ProtonScatter").Set("enabled", true);
		// 	}
		// 	else if (GlobalPosition.DistanceTo(player.GlobalPosition) > 10 && status == "true")
		// 	{
		// 		// GD.Print(GetNode("ProtonScatter").Get("enabled"));
		// 		GD.Print("Further than 10");
		// 		GetNode("ProtonScatter").Set("enabled", false);
		// 	}
		// }
		// else
		// {
		// 	player = GetTree().GetNodesInGroup("Player").ToList().Find(player => player.Name == $"{Multiplayer.GetUniqueId()}") as Player;
		// }
	}
}
