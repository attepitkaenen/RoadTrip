using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MenuHandler : Control
{
    GameManager gameManager;

    public static MenuType currentMenu = MenuType.mainmenu;
    private static List<Menu> menus;

    public enum MenuType
    {
        none,
        mainmenu,
        settings,
        ingamemenu,
        loading
    }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");

        menus = GetChildren().Select(menu => menu as Menu).ToList();
    }

    public override void _Input(InputEvent @event)
    {
        if (gameManager.isGameStarted)
        {
            if (currentMenu == MenuType.none && Input.IsActionJustPressed("menu"))
            {
                OpenMenu(MenuType.ingamemenu);
            }
            else if (Input.IsActionJustPressed("menu"))
            {
                OpenMenu(MenuType.none);
            }
        }
    }

    private static void SwitchMenus(List<Menu> menus, MenuType menuType)
    {
        foreach (Menu menu in menus)
        {
            if (menu.menuType == menuType)
            {
                menu.Visible = true;
                if (menuType == MenuType.mainmenu)
                {
                    ((MainMenu)menu).ResetMenu();
                }
            }
            else
            {
                menu.Visible = false;
            }
        }
    }

    public static void OpenMenu(MenuType menuType)
    {
        GD.Print($"Switching to menu: {menuType.ToString()}");
        currentMenu = menuType;


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
            case MenuType.loading:
                SwitchMenus(menus, MenuType.loading);
                Input.MouseMode = Input.MouseModeEnum.Captured;
                break;
        }
    }
}
