
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class SaveResource : Resource
{
    [Export] public int SaveId { get; set; }
    [Export] public Array<ItemSaveResource> Items { get; set; }

    // Make sure you provide a parameterless constructor.
    // In C#, a parameterless constructor is different from a
    // constructor with all default values.
    // Without a parameterless constructor, Godot will have problems
    // creating and editing your resource via the inspector.
    public SaveResource() { }
    public SaveResource(int id) 
    {
        SaveId = id;
    }
}
