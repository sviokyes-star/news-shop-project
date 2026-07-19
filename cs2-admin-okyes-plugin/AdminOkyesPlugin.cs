using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using CS2MenuManager.API.Menu;

namespace AdminOkyesPlugin;

public class AdminOkyesPlugin : BasePlugin
{
    public override string ModuleName => "Admin [Okyes]";
    public override string ModuleVersion => "1.2.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Админ-панель с меню управления игроками и сервером";

    private static readonly string[] Maps =
    {
        "de_dust2",
        "de_mirage",
        "de_inferno",
        "de_nuke",
        "de_ancient",
        "de_anubis",
        "de_vertigo",
        "de_train"
    };

    public override void Load(bool hotReload)
    {
        AddCommand("css_admin", "Открыть Admin [Okyes]", OnAdminMenuCommand);
        AddCommandListener("say", OnPlayerSay);
        AddCommandListener("say_team", OnPlayerSay);

        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
    }

    private HookResult OnPlayerSay(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        string message = info.GetArg(1).Trim();

        if (message.Equals("!admin", StringComparison.OrdinalIgnoreCase))
        {
            if (!HasAdminPermission(player))
            {
                player.PrintToChat($" {ChatColors.Red}[Admin Okyes] У вас нет прав для использования админ-меню");
                return HookResult.Handled;
            }

            ShowMainMenu(player);
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }

    private bool HasAdminPermission(CCSPlayerController player)
    {
        return AdminManager.PlayerHasPermissions(player, "@css/root") ||
               AdminManager.PlayerHasPermissions(player, "@css/kick") ||
               AdminManager.PlayerHasPermissions(player, "@css/ban") ||
               AdminManager.PlayerHasPermissions(player, "@css/generic");
    }

    public void OnAdminMenuCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null || !caller.IsValid)
            return;

        if (!HasAdminPermission(caller))
        {
            caller.PrintToChat($" {ChatColors.Red}[Admin Okyes] У вас нет прав для использования админ-меню");
            return;
        }

        ShowMainMenu(caller);
    }

    private void ShowMainMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Admin [Okyes]", this);

        menu.AddItem("Управление игроками", (controller, option) =>
        {
            ShowPlayersMenu(controller);
        });

        menu.AddItem("Управление сервером", (controller, option) =>
        {
            ShowServerMenu(controller);
        });

        menu.AddItem("Управление Таймером", (controller, option) =>
        {
            ShowTimerMenu(controller);
        });

        menu.AddItem("Управление спавнами", (controller, option) =>
        {
            ShowSpawnsMenu(controller);
        });

        menu.AddItem("Управление магазином", (controller, option) =>
        {
            ShowShopMenu(controller);
        });

        menu.AddItem("Управление подарками", (controller, option) =>
        {
            ShowGiftsMenu(controller);
        });

        menu.Display(player, 0);
    }

    private void ShowPlayersMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Управление игроками", this);

        menu.AddItem("Забанить", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/ban") &&
                !AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для бана");
                return;
            }

            ShowPlayerSelectMenu(controller, "Кого забанить?", target =>
            {
                Server.ExecuteCommand($"banid 60 {target.UserId}");
                Server.PrintToChatAll($" {ChatColors.Red}[Admin Okyes] {controller.PlayerName} забанил {target.PlayerName} на 60 минут");
            });
        });

        menu.AddItem("Кикнуть", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/kick") &&
                !AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для кика");
                return;
            }

            ShowPlayerSelectMenu(controller, "Кого кикнуть?", target =>
            {
                Server.ExecuteCommand($"kickid {target.UserId}");
                Server.PrintToChatAll($" {ChatColors.Red}[Admin Okyes] {controller.PlayerName} кикнул {target.PlayerName}");
            });
        });

        menu.PrevMenu = GetMainMenu();

        menu.Display(player, 0);
    }

    private void ShowServerMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Управление сервером", this);

        menu.AddItem("Сменить карту", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root") &&
                !AdminManager.PlayerHasPermissions(controller, "@css/changemap"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для смены карты");
                return;
            }

            ShowMapSelectMenu(controller);
        });

        menu.PrevMenu = GetMainMenu();

        menu.Display(player, 0);
    }

    private void ShowMapSelectMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Выберите карту", this);

        foreach (var map in Maps)
        {
            menu.AddItem(map, (controller, option) =>
            {
                Server.PrintToChatAll($" {ChatColors.Green}[Admin Okyes] {controller.PlayerName} меняет карту на {map}...");
                Server.ExecuteCommand($"changelevel {map}");
            });
        }

        menu.Display(player, 0);
    }

    private void ShowTimerMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Управление Таймером", this);

        menu.AddItem("Добавить старт", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для настройки таймера");
                return;
            }

            if (controller.PlayerPawn.Value == null)
                return;

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_setstart"));
        });

        menu.AddItem("Добавить финиш", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для настройки таймера");
                return;
            }

            if (controller.PlayerPawn.Value == null)
                return;

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_setend"));
        });

        menu.PrevMenu = GetMainMenu();

        menu.Display(player, 0);
    }

    private void ShowSpawnsMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Управление спавнами", this);

        menu.AddItem("Добавить точку спавна CT", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления спавнами");
                return;
            }

            if (controller.PlayerPawn.Value == null)
                return;

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_setctspawn"));
        });

        menu.AddItem("Добавить точку спавна T", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления спавнами");
                return;
            }

            if (controller.PlayerPawn.Value == null)
                return;

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_settspawn"));
        });

        menu.AddItem("Удалить все CT спавны", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления спавнами");
                return;
            }

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_clearctspawns"));
        });

        menu.AddItem("Удалить все T спавны", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления спавнами");
                return;
            }

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_cleartspawns"));
        });

        menu.PrevMenu = GetMainMenu();

        menu.Display(player, 0);
    }

    private void ShowPlayerSelectMenu(CCSPlayerController caller, string title, Action<CCSPlayerController> onSelect)
    {
        var menu = new WasdMenu(title, this);

        var players = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot).ToList();

        if (players.Count == 0)
        {
            caller.PrintToChat($" {ChatColors.Yellow}[Admin Okyes] На сервере нет игроков");
            return;
        }

        foreach (var target in players)
        {
            menu.AddItem(target.PlayerName, (controller, option) =>
            {
                onSelect(target);
            });
        }

        menu.Display(caller, 0);
    }

    private WasdMenu GetMainMenu()
    {
        var menu = new WasdMenu("Admin [Okyes]", this);

        menu.AddItem("Управление игроками", (controller, option) =>
        {
            ShowPlayersMenu(controller);
        });

        menu.AddItem("Управление сервером", (controller, option) =>
        {
            ShowServerMenu(controller);
        });

        menu.AddItem("Управление Таймером", (controller, option) =>
        {
            ShowTimerMenu(controller);
        });

        menu.AddItem("Управление спавнами", (controller, option) =>
        {
            ShowSpawnsMenu(controller);
        });

        menu.AddItem("Управление магазином", (controller, option) =>
        {
            ShowShopMenu(controller);
        });

        menu.AddItem("Управление подарками", (controller, option) =>
        {
            ShowGiftsMenu(controller);
        });

        return menu;
    }

    private static readonly int[] ShopAmounts = { 10, 50, 100, 500, 1000 };

    private void ShowShopMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Управление магазином", this);

        menu.AddItem("Выдать золото", (controller, option) =>
            ShowShopActionMenu(controller, "css_givegold", "золото", "Выдать"));

        menu.AddItem("Выдать серебро", (controller, option) =>
            ShowShopActionMenu(controller, "css_givesilver", "серебро", "Выдать"));

        menu.AddItem("Забрать золото", (controller, option) =>
            ShowShopActionMenu(controller, "css_takegold", "золото", "Забрать"));

        menu.AddItem("Забрать серебро", (controller, option) =>
            ShowShopActionMenu(controller, "css_takesilver", "серебро", "Забрать"));

        menu.AddItem("Посмотреть баланс игрока", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления магазином");
                return;
            }

            ShowPlayerSelectMenu(controller, "Чей баланс посмотреть?", target =>
            {
                int targetUserId = target.UserId ?? 0;
                Server.NextFrame(() => controller.ExecuteClientCommandFromServer($"css_balance {targetUserId}"));
            });
        });

        menu.AddItem("Перезагрузить товары", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления магазином");
                return;
            }

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_shop_reload"));
        });

        menu.PrevMenu = GetMainMenu();
        menu.Display(player, 0);
    }

    private void ShowShopActionMenu(CCSPlayerController player, string command, string currencyName, string actionName)
    {
        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            player.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления магазином");
            return;
        }

        ShowPlayerSelectMenu(player, $"{actionName} {currencyName} кому?", target =>
        {
            var amountMenu = new WasdMenu($"Сколько {currencyName}?", this);

            foreach (int amount in ShopAmounts)
            {
                int value = amount;
                amountMenu.AddItem(value.ToString(), (controller, option) =>
                {
                    Server.ExecuteCommand($"{command} {target.UserId} {value}");
                    controller.PrintToChat($" {ChatColors.Green}[Admin Okyes] {actionName}: {value} {currencyName} — {target.PlayerName}");
                });
            }

            amountMenu.Display(player, 0);
        });
    }

    private static readonly int[] GiftAmounts = { 5, 10, 25, 50, 100 };

    private void ShowGiftsMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Управление подарками", this);

        menu.AddItem("Поставить подарок (золото)", (controller, option) =>
            ShowGiftAmountMenu(controller, "gold", "золота"));

        menu.AddItem("Поставить подарок (серебро)", (controller, option) =>
            ShowGiftAmountMenu(controller, "silver", "серебра"));

        menu.AddItem("Убрать ближайший подарок", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления подарками");
                return;
            }

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_gift_remove"));
        });

        menu.AddItem("Убрать все подарки", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления подарками");
                return;
            }

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_gift_clear"));
        });

        menu.PrevMenu = GetMainMenu();
        menu.Display(player, 0);
    }

    private void ShowGiftAmountMenu(CCSPlayerController player, string currencyArg, string currencyName)
    {
        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            player.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления подарками");
            return;
        }

        var menu = new WasdMenu($"Сколько {currencyName}?", this);

        foreach (int amount in GiftAmounts)
        {
            int value = amount;
            menu.AddItem(value.ToString(), (controller, option) =>
            {
                Server.NextFrame(() => controller.ExecuteClientCommandFromServer($"css_gift {currencyArg} {value}"));
                controller.PrintToChat($" {ChatColors.Green}[Admin Okyes] Подарок установлен: +{value} {currencyName}");
            });
        }

        menu.Display(player, 0);
    }

    [ConsoleCommand("css_okban", "Забанить игрока")]
    [RequiresPermissions("@css/ban")]
    [CommandHelper(minArgs: 1, usage: "<имя/userid> [время] [причина]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnBanCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var target = FindTarget(command.GetArg(1));
        if (target == null)
        {
            command.ReplyToCommand($" {ChatColors.Red}[Admin Okyes] Игрок не найден");
            return;
        }

        string minutes = command.ArgCount > 2 ? command.GetArg(2) : "60";
        string reason = command.ArgCount > 3 ? command.GetArg(3) : "Нарушение правил";
        string adminName = caller?.PlayerName ?? "Console";

        Server.ExecuteCommand($"banid {minutes} {target.UserId}");
        Server.PrintToChatAll($" {ChatColors.Red}[Admin Okyes] {adminName} забанил {target.PlayerName} на {minutes} мин. Причина: {reason}");
    }

    [ConsoleCommand("css_okkick", "Кикнуть игрока")]
    [RequiresPermissions("@css/kick")]
    [CommandHelper(minArgs: 1, usage: "<имя/userid> [причина]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnKickCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var target = FindTarget(command.GetArg(1));
        if (target == null)
        {
            command.ReplyToCommand($" {ChatColors.Red}[Admin Okyes] Игрок не найден");
            return;
        }

        string reason = command.ArgCount > 2 ? command.GetArg(2) : "Нарушение правил";
        string adminName = caller?.PlayerName ?? "Console";

        Server.ExecuteCommand($"kickid {target.UserId} {reason}");
        Server.PrintToChatAll($" {ChatColors.Red}[Admin Okyes] {adminName} кикнул {target.PlayerName}. Причина: {reason}");
    }

    [ConsoleCommand("css_okmap", "Сменить карту")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 1, usage: "<название карты>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnMapCommand(CCSPlayerController? caller, CommandInfo command)
    {
        string map = command.GetArg(1);
        string adminName = caller?.PlayerName ?? "Console";

        Server.PrintToChatAll($" {ChatColors.Green}[Admin Okyes] {adminName} меняет карту на {map}...");
        Server.ExecuteCommand($"changelevel {map}");
    }

    private CCSPlayerController? FindTarget(string search)
    {
        if (int.TryParse(search, out int userId))
        {
            return Utilities.GetPlayerFromUserid(userId);
        }

        var players = Utilities.GetPlayers();
        return players.FirstOrDefault(p =>
            p.PlayerName.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    public override void Unload(bool hotReload)
    {
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}