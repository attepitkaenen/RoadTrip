using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TireResource : Resource
{
    [Export] public float TireRadius { get; set; } = 0.35f;
    [Export] public float TireWidth { get; set; } = 0.2f;
    [Export] public float FrictionSlip { get; set; } = 1.5f;
    // Make sure you provide a parameterless constructor.
    // In C#, a parameterless constructor is different from a
    // constructor with all default values.
    // Without a parameterless constructor, Godot will have problems
    // creating and editing your resource via the inspector.
    public TireResource() { }
}
