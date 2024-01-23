using Godot;
using System;

public partial class Menu : Control
{
    public MenuHandler.MenuType menuType;
    public MenuHandler menuHandler;

    public override void _EnterTree()
    {
        menuHandler = GetParent<MenuHandler>();
    }
}
