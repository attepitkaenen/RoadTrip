using Godot;
using System;

public partial class InGameMenu : Menu
{
    public override void _Ready()
    {
        menuType = MenuHandler.MenuType.ingamemenu;
    }

    public void _on_resume_pressed()
    {
        menuHandler.OpenMenu(MenuHandler.MenuType.none);
    }

    public void _on_mainmenu_pressed()
    {
        menuHandler.OpenMenu(MenuHandler.MenuType.mainmenu);
        GetTree().Root.GetNode("World").QueueFree();
    }

    public void _on_settings_pressed()
    {

    }

    public void _on_quit_pressed()
    {
        GetTree().Quit();
    }
}
