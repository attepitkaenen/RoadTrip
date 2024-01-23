using Godot;
using System;

public partial class InGameMenu : Menu
{
    public override void _Ready()
    {
        menuType = MenuHandler.MenuType.ingamemenu;
    }
}
