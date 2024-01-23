using Godot;
using System;

public partial class Settings : Menu
{
    public override void _Ready()
    {
        menuType = MenuHandler.MenuType.settings;
        
    }

        public void _on_back_button_pressed()
    {
        menuHandler.OpenMenu(MenuHandler.MenuType.mainmenu);
    }
}
