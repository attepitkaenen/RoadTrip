using Godot;


[GlobalClass]
public partial class WeaponResource : Resource
{
    [Export] public string WeaponName { get; set; } = "";
    [Export] public string ActiveAnim { get; set; } = "";
    [Export] public string ShootAnim { get; set; } = "";
    [Export] public string ReloadAnim { get; set; } = "";
    [Export] public string DeactivateAnim { get; set; } = "";
    [Export] public string OutOfAmmoAnim { get; set; } = "";

    [Export] public int CurrentAmmo { get; set; } = 0;
    [Export] public int ReserveAmmo { get; set; } = 0;
    [Export] public int Magazine { get; set; } = 0;
    [Export] public int MaxAmmo { get; set; } = 0;

    [Export] public bool AutoFire { get; set; } = false;

    // Make sure you provide a parameterless constructor.
    // In C#, a parameterless constructor is different from a
    // constructor with all default values.
    // Without a parameterless constructor, Godot will have problems
    // creating and editing your resource via the inspector.
    public WeaponResource() {}
}