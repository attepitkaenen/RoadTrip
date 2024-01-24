using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MenuHandler : Control
{
    MultiplayerController multiplayerController;
    public MenuType currentMenu = MenuType.mainmenu;

    public enum MenuType
    {
        none,
        mainmenu,
        settings,
        ingamemenu
    }

    public override void _Ready()
    {
        multiplayerController = GetNode<MultiplayerController>("/root/MultiplayerController");
    }

    public override void _Input(InputEvent @event)
    {
        if (currentMenu == MenuType.none && Input.IsActionJustPressed("menu"))
        {
            OpenMenu(MenuType.ingamemenu);
        }
        else if (currentMenu == MenuType.ingamemenu && Input.IsActionJustPressed("menu"))
        {
            OpenMenu(MenuType.none);
        }
    }

    private void SwitchMenus(List<Menu> menus, MenuType menuType)
    {
        foreach (Menu menu in menus)
        {
            if (menu.menuType == menuType)
            {
                menu.Visible = true;
            }
            else
            {
                menu.Visible = false;
            }
        }
    }

    public void OpenMenu(MenuType menuType)
    {
        currentMenu = menuType;

        List<Menu> menus = GetChildren().Select(menu => menu as Menu).ToList();

        switch (currentMenu)
        {
            case MenuType.none:
                SwitchMenus(menus, MenuType.none);
                Input.MouseMode = Input.MouseModeEnum.Captured;
                break;
            case MenuType.mainmenu:
                SwitchMenus(menus, MenuType.mainmenu);
                Input.MouseMode = Input.MouseModeEnum.Visible;
                break;
            case MenuType.settings:
                SwitchMenus(menus, MenuType.settings);
                Input.MouseMode = Input.MouseModeEnum.Visible;
                break;
            case MenuType.ingamemenu:
                SwitchMenus(menus, MenuType.ingamemenu);
                Input.MouseMode = Input.MouseModeEnum.Visible;
                break;

        }
    }
}
