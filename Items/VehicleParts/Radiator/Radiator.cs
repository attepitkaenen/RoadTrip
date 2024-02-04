using Godot;
using System;

public partial class Radiator : CarPart
{
	public float coolant = 100f;

    public Timer timer;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        timer = GetNode<Timer>("Timer");
        timer.Start(1f);
        timer.Timeout += HandleLeak;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (GetCondition() < 1)
		{
			coolant = 0;
		}
	}

	public void HandleLeak()
	{
        if (GetCondition() < 75)
        {
            if (GetCondition() < 1)
            {
                LeakCoolant(1);
            }
            else
            {
                LeakCoolant(1 / GetCondition());
            }
        }
    }

    public void LeakCoolant(float amount)
    {
        coolant -= amount;
    }

	public void Fill()
	{
		coolant++;
	}

    public float GetCoolant()
	{
		return coolant;
    }
}
