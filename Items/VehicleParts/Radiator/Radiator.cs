using Godot;
using System;

public partial class Radiator : CarPart
{
	public float coolant = 100f;

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
