using Godot;
using System;

public partial class Settings : Menu
{
    [Export] private LineEdit sensitivity;

    public override void _Ready()
    {
        menuType = MenuHandler.MenuType.settings;
        sensitivity.TextChanged += ChangeSensitivity;
        
    }

    private void ChangeSensitivity(string newText)
    {
        GameManager.Sensitivity = int.Parse(newText);
    }

    public void _on_back_button_pressed()
    {
        menuHandler.OpenMenu(MenuHandler.MenuType.mainmenu);
    }
}
