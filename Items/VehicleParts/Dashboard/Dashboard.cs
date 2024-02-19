using Godot;
using System;

public partial class Dashboard : Node3D
{
    Vehicle _vehicle;
    Interactable _ignition;

    public override void _Ready()
    {
        _vehicle = GetParent<Vehicle>();

        _ignition = GetNode<Interactable>("Ignition");
        _ignition.Pressed += ToggleEngine;
    }

    private void ToggleEngine()
    {
        _vehicle.engineBay.ToggleEngine();
    }

}
