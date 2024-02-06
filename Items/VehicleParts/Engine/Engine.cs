using Godot;
using System;

public partial class Engine : CarPart
{
	[Export] public EngineResource stats;
	[Export] float oil = 100;
	bool running = false;
	public Timer timer;



	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (GetCondition() == 0)
		{
			running = false;
		}
	}

	public void ToggleEngine()
	{
		running = !running;
	}
	
	public void EngineWear()
	{
		if (running && GetCondition() > 1)
		{
			if (oil < 1)
			{
				Damage(1);
			}
			else if (oil < 75)
			{
				Damage(1 / oil);
			}
		}

        if (GetCondition() < 75)
        {
            if (GetCondition() < 1)
            {
                LeakOil(1);
            }
            else
            {
                LeakOil(1 / GetCondition());
            }
        }
    }

	public void LeakOil(float amount)
	{
		oil -= amount;
	}

	public float GetEnginePower()
	{
		// if (!running)
		// {
		// 	return 0f;
		// }
		return stats.Horsepower - (stats.Horsepower * 0.4f * (100f - GetCondition()));
	}

	public bool IsRunning()
	{
		return running;
	}


}
