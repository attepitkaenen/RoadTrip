using Godot;
using Godot.Collections;

[GlobalClass]
public partial class EngineResource : Resource
{
    [Export] public float Horsepower { get; set; } = 200;
    // Make sure you provide a parameterless constructor.
    // In C#, a parameterless constructor is different from a
    // constructor with all default values.
    // Without a parameterless constructor, Godot will have problems
    // creating and editing your resource via the inspector.
    public EngineResource() { }
}
