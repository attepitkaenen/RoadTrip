using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

public partial class GameManager : Node
{
	[Signal] public delegate void PlayersChangedEventHandler();
	private List<PlayerState> Players = new List<PlayerState>();
	public float Sensitivity = 0.001f;

	public void AddPlayerState(PlayerState state)
	{
		Players.Add(state);
		EmitSignal(SignalName.PlayersChanged);
	}

	public void RemovePlayerState(long id)
	{
		Players.Remove(Players.Where(i => i.Id == id).First<PlayerState>());
	}

	public void ResetPlayerStates()
	{
		Players = new List<PlayerState>();
	}

	public List<PlayerState> GetPlayerStates()
	{
		return Players;
	}
}
