using Godot;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

public partial class GameManager : Node
{
	public static List<PlayerState> Players = new List<PlayerState>();

	public static float Sensitivity = 0.001f;
}
