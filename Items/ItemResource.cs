using Godot;

[GlobalClass]
public partial class ItemResource : Resource
{
    [Export]
    public bool Equippable { get; set; }
    [Export]
    public string ItemName { get; set; }
    [Export]
    public int ItemId { get; set; }
    [Export]
    public string Description { get; set; }
    [Export]
    public PackedScene ItemOnFloor { get; set; }
    [Export]
    public PackedScene ItemInHand { get; set; }

    // Make sure you provide a parameterless constructor.
    // In C#, a parameterless constructor is different from a
    // constructor with all default values.
    // Without a parameterless constructor, Godot will have problems
    // creating and editing your resource via the inspector.
    public ItemResource() { }
}
