using Godot;
using Riptide;
using System;

public partial class InputManager : Node
{
	Player player;
	RiptideClient riptideClient;
	private bool[] inputs = new bool[7];
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		player = GetParent<Player>();
		riptideClient = GetTree().Root.GetNode<RiptideClient>("RiptideClient");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (player.isLocal)
		{
			if (Input.IsActionPressed("up"))
				inputs[0] = true;
			if (Input.IsActionPressed("down"))
				inputs[1] = true;
			if (Input.IsActionPressed("left"))
				inputs[2] = true;
			if (Input.IsActionPressed("right"))
				inputs[3] = true;
			if (Input.IsActionPressed("jump"))
				inputs[4] = true;
			if (Input.IsActionPressed("sprint"))
				inputs[5] = true;
			if (Input.IsActionPressed("crouch"))
				inputs[6] = true;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (player.isLocal)
		{
			SendInput();

			for (int i = 0; i < inputs.Length; i++)
			{
				inputs[i] = false;
			}
		}
	}

	private void SendInput()
	{
		Message message = Message.Create(MessageSendMode.Unreliable, ClientToServerId.input);
		message.AddBools(inputs, false);
		message.AddVector3(player.GlobalRotation);
		riptideClient.SendMessage(message);
	}
}