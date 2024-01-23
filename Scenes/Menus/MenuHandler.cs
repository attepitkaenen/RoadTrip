using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MenuHandler : Control
{
    public MenuType currentMenu = MenuType.mainmenu;

    public enum MenuType
    {
        none,
        mainmenu,
        settings,
        ingamemenu
    }

    public override void _Process(double delta)
    {
        List<Menu> menus = GetChildren().Select(menu => menu as Menu).ToList();

        switch (currentMenu)
        {
            case MenuType.mainmenu:
                SwitchMenus(menus, MenuType.mainmenu);
                break;
            case MenuType.settings:
                SwitchMenus(menus, MenuType.settings);
                break;
            case MenuType.ingamemenu:
                SwitchMenus(menus, MenuType.ingamemenu);
                break;
        }
    }

    public void SwitchMenus(List<Menu> menus, MenuType menuType)
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
    }
}
