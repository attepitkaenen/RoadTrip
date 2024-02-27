using Godot;
using System;

public partial class Dashboard : Node3D
{
    Vehicle _vehicle;
    Interactable _ignition;
    Label3D _speedometer;

    public override void _Ready()
    {
        _vehicle = GetParent<Vehicle>();

        _ignition = GetNode<Interactable>("Ignition");
        _speedometer = GetNode<Label3D>("Speedometer");
        _ignition.Pressed += ToggleEngine;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_vehicle.engineBay.GetHorsePower() != 0)
        {
            _speedometer.Text = Math.Round(_vehicle.LinearVelocity.Length() * 3.6f, 0).ToString();
        }
    }

    private void ToggleEngine()
    {
        _vehicle.engineBay.ToggleEngine();
    }

}
