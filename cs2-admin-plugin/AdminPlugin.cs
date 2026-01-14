using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;

namespace AdminPlugin;

public class AdminPlugin : BasePlugin
{
    public override string ModuleName => "Admin Tools";
    public override string ModuleVersion => "1.0.8";
    public override string ModuleAuthor => "poehali.dev";
    public override string ModuleDescription => "Полнофункциональная админка для CS2";

    private readonly Dictionary<ulong, string> _playerMenuContext = new();

    public override void Load(bool hotReload)
    {
        AddCommand("css_admin", "Открыть админ-меню", OnAdminMenuCommand);
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
            var steamId = player.SteamID;
            Console.WriteLine($"[AdminPlugin] Player {player.PlayerName} (SteamID: {steamId}) попытался открыть меню");
            
            bool hasPermission = AdminManager.PlayerHasPermissions(player, "@css/kick") || 
                                 AdminManager.PlayerHasPermissions(player, "@css/root") ||
                                 AdminManager.PlayerHasPermissions(player, "@css/ban") ||
                                 AdminManager.PlayerHasPermissions(player, "@css/slay") ||
                                 AdminManager.PlayerHasPermissions(player, "@css/cheats");
            
            Console.WriteLine($"[AdminPlugin] Has permission: {hasPermission}");

            if (!hasPermission)
            {
                player.PrintToChat($" {ChatColors.Red}[ADMIN] У вас нет прав для использования админ-меню");
                player.PrintToChat($" {ChatColors.Yellow}[DEBUG] Ваш SteamID: {steamId}");
                return HookResult.Handled;
            }

            ShowMainMenu(player);
            return HookResult.Handled;
        }

        if (message.StartsWith("!a") && message.Length == 3 && char.IsDigit(message[2]))
        {
            if (!AdminManager.PlayerHasPermissions(player, "@css/kick"))
                return HookResult.Continue;

            string context = _playerMenuContext.ContainsKey(player.SteamID) ? _playerMenuContext[player.SteamID] : "main";
            int num = int.Parse(message[2].ToString());

            HandleQuickCommand(player, context, num);
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }

    private void HandleQuickCommand(CCSPlayerController player, string context, int num)
    {
        switch (context)
        {
            case "main":
                HandleMainMenuCommand(player, num);
                break;
            case "players":
                HandlePlayersMenuCommand(player, num);
                break;
            case "cheats":
                HandleCheatsMenuCommand(player, num);
                break;
            case "zones":
                HandleZonesMenuCommand(player, num);
                break;
            case "gifts":
                HandleGiftsMenuCommand(player, num);
                break;
            case "spawns":
                HandleSpawnsMenuCommand(player, num);
                break;
        }
    }

    private void HandleMainMenuCommand(CCSPlayerController player, int num)
    {
        switch (num)
        {
            case 1:
                ShowPlayerManagementMenu(player);
                break;
            case 2:
                ShowCheatsMenu(player);
                break;
            case 3:
                if (AdminManager.PlayerHasPermissions(player, "@css/root"))
                    ShowZonesMenu(player);
                break;
            case 4:
                if (AdminManager.PlayerHasPermissions(player, "@css/root"))
                    ShowGiftsMenu(player);
                break;
            case 5:
                if (AdminManager.PlayerHasPermissions(player, "@css/root"))
                    ShowSpawnsMenu(player);
                break;
        }
    }

    private void HandlePlayersMenuCommand(CCSPlayerController player, int num)
    {
        switch (num)
        {
            case 1:
                player.PrintToChat($" {ChatColors.Yellow}[ADMIN] Выберите игрока для убийства через меню");
                ShowPlayerManagementMenu(player);
                break;
            case 2:
                player.PrintToChat($" {ChatColors.Yellow}[ADMIN] Выберите игрока для кика через меню");
                ShowPlayerManagementMenu(player);
                break;
            case 3:
                player.PrintToChat($" {ChatColors.Yellow}[ADMIN] Выберите игрока для бана через меню");
                ShowPlayerManagementMenu(player);
                break;
            case 9:
                ShowMainMenu(player);
                break;
        }
    }

    private void HandleCheatsMenuCommand(CCSPlayerController player, int num)
    {
        switch (num)
        {
            case 1:
                if (player.PlayerPawn.Value != null)
                {
                    var pawn = player.PlayerPawn.Value;
                    var currentMode = pawn.MoveType;
                    if (currentMode == MoveType_t.MOVETYPE_NOCLIP)
                    {
                        pawn.MoveType = MoveType_t.MOVETYPE_WALK;
                        player.PrintToChat($" {ChatColors.Red}[ADMIN] Noclip выключен");
                    }
                    else
                    {
                        pawn.MoveType = MoveType_t.MOVETYPE_NOCLIP;
                        player.PrintToChat($" {ChatColors.Green}[ADMIN] Noclip включен");
                    }
                }
                break;
            case 2:
                if (player.PlayerPawn.Value != null)
                {
                    var pawn = player.PlayerPawn.Value;
                    bool isGod = pawn.TakesDamage;
                    pawn.TakesDamage = !isGod;
                    if (!isGod)
                        player.PrintToChat($" {ChatColors.Red}[ADMIN] Режим бога выключен");
                    else
                        player.PrintToChat($" {ChatColors.Green}[ADMIN] Режим бога включен");
                }
                break;
            case 9:
                ShowMainMenu(player);
                break;
        }
    }

    private void HandleZonesMenuCommand(CCSPlayerController player, int num)
    {
        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
            return;

        switch (num)
        {
            case 1:
                Server.ExecuteCommand("css_setstart");
                player.PrintToChat($" {ChatColors.Green}[ADMIN] Зона старта установлена");
                break;
            case 2:
                Server.ExecuteCommand("css_setend");
                player.PrintToChat($" {ChatColors.Green}[ADMIN] Зона финиша установлена");
                break;
            case 3:
                Server.ExecuteCommand("css_showzones");
                break;
            case 4:
                Server.ExecuteCommand("css_removezones");
                player.PrintToChat($" {ChatColors.Red}[ADMIN] Зоны удалены");
                break;
            case 9:
                ShowMainMenu(player);
                break;
        }
    }

    private void HandleGiftsMenuCommand(CCSPlayerController player, int num)
    {
        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
            return;

        switch (num)
        {
            case 1:
                Server.ExecuteCommand("css_addgift 1000");
                break;
            case 2:
                Server.ExecuteCommand("css_addgift 5000");
                break;
            case 3:
                Server.ExecuteCommand("css_addgift 10000");
                break;
            case 4:
                Server.ExecuteCommand("css_listgifts");
                break;
            case 5:
                Server.ExecuteCommand("css_removegifts");
                break;
            case 9:
                ShowMainMenu(player);
                break;
        }
    }

    private void HandleSpawnsMenuCommand(CCSPlayerController player, int num)
    {
        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
            return;

        switch (num)
        {
            case 1:
                Server.ExecuteCommand("css_addspawn CT");
                break;
            case 2:
                Server.ExecuteCommand("css_addspawn T");
                break;
            case 3:
                Server.ExecuteCommand("css_removespawn");
                break;
            case 4:
                Server.ExecuteCommand("css_listspawns");
                break;
            case 5:
                Server.ExecuteCommand("css_showspawns CT");
                break;
            case 6:
                Server.ExecuteCommand("css_showspawns T");
                break;
            case 7:
                Server.ExecuteCommand("css_hidespawns");
                break;
            case 8:
                Server.ExecuteCommand("css_removespawns");
                break;
            case 9:
                ShowMainMenu(player);
                break;
        }
    }

    public void OnAdminMenuCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null || !caller.IsValid)
            return;

        if (!AdminManager.PlayerHasPermissions(caller, "@css/kick"))
        {
            caller.PrintToChat($" {ChatColors.Red}[ADMIN] У вас нет прав для использования админ-меню");
            return;
        }

        ShowMainMenu(caller);
    }

    private void ShowMainMenu(CCSPlayerController player)
    {
        _playerMenuContext[player.SteamID] = "main";
        
        var menu = new ChatMenu("Админ-Меню [!a1-!a5]");

        menu.AddMenuOption("!a1 → Управление игроками", (controller, option) =>
        {
            ShowPlayerManagementMenu(controller);
        });

        menu.AddMenuOption("!a2 → Читы и настройки", (controller, option) =>
        {
            ShowCheatsMenu(controller);
        });

        if (AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            menu.AddMenuOption("!a3 → ⚙️ Настройка зон карты", (controller, option) =>
            {
                ShowZonesMenu(controller);
            });

            menu.AddMenuOption("!a4 → 🎁 Управление подарками", (controller, option) =>
            {
                ShowGiftsMenu(controller);
            });

            menu.AddMenuOption("!a5 → 📍 Управление спавнами", (controller, option) =>
            {
                ShowSpawnsMenu(controller);
            });
        }

        MenuManager.OpenChatMenu(player, menu);
    }

    private void ShowPlayerManagementMenu(CCSPlayerController player)
    {
        _playerMenuContext[player.SteamID] = "players";
        
        var menu = new ChatMenu("Управление игроками [!a9=Назад]");

        menu.AddMenuOption("Убить игрока", (controller, option) =>
        {
            ShowPlayerSelectMenu(controller, "Выберите игрока", (target) =>
            {
                if (target.PlayerPawn.Value != null)
                {
                    target.PlayerPawn.Value.CommitSuicide(false, true);
                    Server.PrintToChatAll($" {ChatColors.Red}[ADMIN] {controller.PlayerName} убил {target.PlayerName}");
                }
            });
        });

        menu.AddMenuOption("Кикнуть игрока", (controller, option) =>
        {
            ShowPlayerSelectMenu(controller, "Выберите игрока для кика", (target) =>
            {
                Server.ExecuteCommand($"kickid {target.UserId}");
                Server.PrintToChatAll($" {ChatColors.Red}[ADMIN] {controller.PlayerName} кикнул {target.PlayerName}");
            });
        });

        menu.AddMenuOption("Забанить игрока", (controller, option) =>
        {
            ShowPlayerSelectMenu(controller, "Выберите игрока для бана", (target) =>
            {
                Server.ExecuteCommand($"banid 60 {target.UserId}");
                Server.PrintToChatAll($" {ChatColors.Red}[ADMIN] {controller.PlayerName} забанил {target.PlayerName} на 60 минут");
            });
        });

        menu.AddMenuOption("Шлепнуть игрока", (controller, option) =>
        {
            ShowPlayerSelectMenu(controller, "Выберите игрока", (target) =>
            {
                if (target.PlayerPawn.Value != null)
                {
                    var pawn = target.PlayerPawn.Value;
                    pawn.Health -= 5;
                    
                    var velocity = new Vector(
                        Random.Shared.Next(-100, 100),
                        Random.Shared.Next(-100, 100),
                        Random.Shared.Next(50, 150)
                    );
                    pawn.Teleport(pawn.AbsOrigin, pawn.AbsRotation, velocity);

                    Server.PrintToChatAll($" {ChatColors.Yellow}[ADMIN] {controller.PlayerName} шлепнул {target.PlayerName}");
                }
            });
        });

        menu.AddMenuOption("Заморозить/Разморозить", (controller, option) =>
        {
            ShowPlayerSelectMenu(controller, "Выберите игрока", (target) =>
            {
                if (target.PlayerPawn.Value != null)
                {
                    var pawn = target.PlayerPawn.Value;
                    bool isFrozen = pawn.MoveType == MoveType_t.MOVETYPE_OBSOLETE;

                    if (isFrozen)
                    {
                        pawn.MoveType = MoveType_t.MOVETYPE_WALK;
                        target.PrintToChat($" {ChatColors.Green}[ADMIN] Вы разморожены");
                    }
                    else
                    {
                        pawn.MoveType = MoveType_t.MOVETYPE_OBSOLETE;
                        target.PrintToChat($" {ChatColors.Red}[ADMIN] Вы заморожены");
                    }

                    controller.PrintToChat($" {ChatColors.Green}[ADMIN] {target.PlayerName} {(isFrozen ? "разморожен" : "заморожен")}");
                }
            });
        });

        menu.AddMenuOption("Возродить игрока", (controller, option) =>
        {
            ShowPlayerSelectMenu(controller, "Выберите игрока", (target) =>
            {
                target.Respawn();
                controller.PrintToChat($" {ChatColors.Green}[ADMIN] {target.PlayerName} возрождён");
            });
        });

        menu.AddMenuOption("Телепортировать к себе", (controller, option) =>
        {
            ShowPlayerSelectMenu(controller, "Выберите игрока", (target) =>
            {
                if (controller.PlayerPawn.Value != null && target.PlayerPawn.Value != null)
                {
                    var callerPos = controller.PlayerPawn.Value.AbsOrigin;
                    if (callerPos != null)
                    {
                        target.PlayerPawn.Value.Teleport(callerPos, controller.PlayerPawn.Value.AbsRotation, new Vector(0, 0, 0));
                        controller.PrintToChat($" {ChatColors.Green}[ADMIN] {target.PlayerName} телепортирован к вам");
                    }
                }
            });
        });

        menu.AddMenuOption("Телепортироваться к игроку", (controller, option) =>
        {
            ShowPlayerSelectMenu(controller, "Выберите игрока", (target) =>
            {
                if (controller.PlayerPawn.Value != null && target.PlayerPawn.Value != null)
                {
                    var targetPos = target.PlayerPawn.Value.AbsOrigin;
                    if (targetPos != null)
                    {
                        controller.PlayerPawn.Value.Teleport(targetPos, target.PlayerPawn.Value.AbsRotation, new Vector(0, 0, 0));
                        controller.PrintToChat($" {ChatColors.Green}[ADMIN] Телепортация к {target.PlayerName}");
                    }
                }
            });
        });

        menu.AddMenuOption("!a9 → Назад", (controller, option) =>
        {
            ShowMainMenu(controller);
        });

        MenuManager.OpenChatMenu(player, menu);
    }



    private void ShowCheatsMenu(CCSPlayerController player)
    {
        _playerMenuContext[player.SteamID] = "cheats";
        
        var menu = new ChatMenu("Читы и настройки [!a1-!a2, !a9=Назад]");

        menu.AddMenuOption("!a1 → Режим полёта (Noclip)", (controller, option) =>
        {
            if (controller.PlayerPawn.Value != null)
            {
                var pawn = controller.PlayerPawn.Value;
                var currentMode = pawn.MoveType;

                if (currentMode == MoveType_t.MOVETYPE_NOCLIP)
                {
                    pawn.MoveType = MoveType_t.MOVETYPE_WALK;
                    controller.PrintToChat($" {ChatColors.Red}[ADMIN] Режим полёта выключен");
                }
                else
                {
                    pawn.MoveType = MoveType_t.MOVETYPE_NOCLIP;
                    controller.PrintToChat($" {ChatColors.Green}[ADMIN] Режим полёта включен");
                }
            }
            ShowCheatsMenu(controller);
        });

        menu.AddMenuOption("!a2 → Режим неуязвимости (God Mode)", (controller, option) =>
        {
            if (controller.PlayerPawn.Value != null)
            {
                var pawn = controller.PlayerPawn.Value;
                bool isGod = pawn.TakesDamage;

                pawn.TakesDamage = !isGod;

                if (!isGod)
                    controller.PrintToChat($" {ChatColors.Red}[ADMIN] Режим неуязвимости выключен");
                else
                    controller.PrintToChat($" {ChatColors.Green}[ADMIN] Режим неуязвимости включен");
            }
            ShowCheatsMenu(controller);
        });



        menu.AddMenuOption("!a9 → Назад", (controller, option) =>
        {
            ShowMainMenu(controller);
        });

        MenuManager.OpenChatMenu(player, menu);
    }

    private void ShowZonesMenu(CCSPlayerController player)
    {
        _playerMenuContext[player.SteamID] = "zones";
        
        var menu = new ChatMenu("Настройка зон карты [!a1-!a4, !a9=Назад]");

        menu.AddMenuOption("!a1 → 🟩 Установить зону СТАРТА", (controller, option) =>
        {
            if (controller.PlayerPawn.Value != null)
            {
                controller.ExecuteClientCommand($"css_setstart");
            }
            ShowZonesMenu(controller);
        });

        menu.AddMenuOption("!a2 → 🟥 Установить зону ФИНИША", (controller, option) =>
        {
            if (controller.PlayerPawn.Value != null)
            {
                controller.ExecuteClientCommand($"css_setend");
            }
            ShowZonesMenu(controller);
        });

        menu.AddMenuOption("!a3 → 📋 Показать текущие зоны", (controller, option) =>
        {
            controller.ExecuteClientCommand($"css_showzones");
            ShowZonesMenu(controller);
        });

        menu.AddMenuOption("!a9 → Назад", (controller, option) =>
        {
            ShowMainMenu(controller);
        });

        MenuManager.OpenChatMenu(player, menu);
    }

    private void ShowGiftsMenu(CCSPlayerController player)
    {
        _playerMenuContext[player.SteamID] = "gifts";
        
        var menu = new ChatMenu("Управление подарками [!a1-!a5, !a9=Назад]");

        menu.AddMenuOption("!a1 → ➕ Добавить подарок (1000 серебра)", (controller, option) =>
        {
            if (controller.PlayerPawn.Value != null)
            {
                Server.NextFrame(() => controller.ExecuteClientCommand("say !addgift 1000"));
            }
            ShowGiftsMenu(controller);
        });

        menu.AddMenuOption("!a2 → ➕ Добавить подарок (5000 серебра)", (controller, option) =>
        {
            if (controller.PlayerPawn.Value != null)
            {
                Server.NextFrame(() => controller.ExecuteClientCommand("say !addgift 5000"));
            }
            ShowGiftsMenu(controller);
        });

        menu.AddMenuOption("!a3 → ➕ Добавить подарок (10000 серебра)", (controller, option) =>
        {
            if (controller.PlayerPawn.Value != null)
            {
                Server.NextFrame(() => controller.ExecuteClientCommand("say !addgift 10000"));
            }
            ShowGiftsMenu(controller);
        });

        menu.AddMenuOption("!a4 → 📋 Список всех подарков", (controller, option) =>
        {
            Server.NextFrame(() => controller.ExecuteClientCommand("say !listgifts"));
            ShowGiftsMenu(controller);
        });

        menu.AddMenuOption("!a5 → 🗑️ Удалить все подарки", (controller, option) =>
        {
            Server.NextFrame(() => controller.ExecuteClientCommand("say !removegifts"));
            ShowGiftsMenu(controller);
        });

        menu.AddMenuOption("!a9 → Назад", (controller, option) =>
        {
            ShowMainMenu(controller);
        });

        MenuManager.OpenChatMenu(player, menu);
    }

    private void ShowSpawnsMenu(CCSPlayerController player)
    {
        _playerMenuContext[player.SteamID] = "spawns";
        
        var menu = new ChatMenu("Управление спавнами [!a1-!a8, !a9=Назад]");

        menu.AddMenuOption("!a1 → ➕ Добавить спавн CT", (controller, option) =>
        {
            if (controller.PlayerPawn.Value != null)
            {
                Server.NextFrame(() => controller.ExecuteClientCommand("say !addspawn CT"));
            }
            ShowSpawnsMenu(controller);
        });

        menu.AddMenuOption("!a2 → ➕ Добавить спавн T", (controller, option) =>
        {
            if (controller.PlayerPawn.Value != null)
            {
                Server.NextFrame(() => controller.ExecuteClientCommand("say !addspawn T"));
            }
            ShowSpawnsMenu(controller);
        });

        menu.AddMenuOption("!a3 → 🗑️ Удалить ближайший спавн", (controller, option) =>
        {
            if (controller.PlayerPawn.Value != null)
            {
                Server.NextFrame(() => controller.ExecuteClientCommand("say !removespawn"));
            }
            ShowSpawnsMenu(controller);
        });

        menu.AddMenuOption("!a4 → 📋 Список всех спавнов", (controller, option) =>
        {
            Server.NextFrame(() => controller.ExecuteClientCommand("say !listspawns"));
            ShowSpawnsMenu(controller);
        });

        menu.AddMenuOption("!a5 → 👁️ Показать CT спавны", (controller, option) =>
        {
            Server.NextFrame(() => controller.ExecuteClientCommand("say !showspawns CT"));
            ShowSpawnsMenu(controller);
        });

        menu.AddMenuOption("!a6 → 👁️ Показать T спавны", (controller, option) =>
        {
            Server.NextFrame(() => controller.ExecuteClientCommand("say !showspawns T"));
            ShowSpawnsMenu(controller);
        });

        menu.AddMenuOption("!a7 → 🚫 Скрыть маркеры спавнов", (controller, option) =>
        {
            Server.NextFrame(() => controller.ExecuteClientCommand("say !hidespawns"));
            ShowSpawnsMenu(controller);
        });

        menu.AddMenuOption("!a8 → 🗑️ Удалить все спавны", (controller, option) =>
        {
            Server.NextFrame(() => controller.ExecuteClientCommand("say !removespawns"));
            ShowSpawnsMenu(controller);
        });

        menu.AddMenuOption("!a9 → Назад", (controller, option) =>
        {
            ShowMainMenu(controller);
        });

        MenuManager.OpenChatMenu(player, menu);
    }

    private void ShowPlayerSelectMenu(CCSPlayerController caller, string title, Action<CCSPlayerController> onSelect)
    {
        var menu = new ChatMenu(title);

        var players = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot).ToList();

        foreach (var player in players)
        {
            menu.AddMenuOption(player.PlayerName, (controller, option) =>
            {
                onSelect(player);
                controller.PrintToChat($" {ChatColors.Green}[ADMIN] Действие выполнено");
            });
        }

        menu.AddMenuOption("← Назад", (controller, option) =>
        {
            ShowMainMenu(controller);
        });

        MenuManager.OpenChatMenu(caller, menu);
    }

    [ConsoleCommand("css_kick", "Кикнуть игрока")]
    [RequiresPermissions("@css/kick")]
    [CommandHelper(minArgs: 1, usage: "<имя/userid> [причина]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnKickCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var target = FindTarget(command.GetArg(1));
        if (target == null)
        {
            command.ReplyToCommand($" {ChatColors.Red}[ADMIN] Игрок не найден");
            return;
        }

        string reason = command.ArgCount > 2 ? command.GetArg(2) : "Нарушение правил";
        string adminName = caller?.PlayerName ?? "Console";

        Server.ExecuteCommand($"kickid {target.UserId} {reason}");
        Server.PrintToChatAll($" {ChatColors.Red}[ADMIN] {adminName} кикнул {target.PlayerName}. Причина: {reason}");
    }

    [ConsoleCommand("css_ban", "Забанить игрока")]
    [RequiresPermissions("@css/ban")]
    [CommandHelper(minArgs: 2, usage: "<имя/userid> <время> [причина]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnBanCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var target = FindTarget(command.GetArg(1));
        if (target == null)
        {
            command.ReplyToCommand($" {ChatColors.Red}[ADMIN] Игрок не найден");
            return;
        }

        string timeStr = command.GetArg(2);
        string reason = command.ArgCount > 3 ? command.GetArg(3) : "Нарушение правил";
        string adminName = caller?.PlayerName ?? "Console";

        Server.ExecuteCommand($"css_ban #{target.UserId} {timeStr} {reason}");
        Server.PrintToChatAll($" {ChatColors.Red}[ADMIN] {adminName} забанил {target.PlayerName} на {timeStr}. Причина: {reason}");
    }

    [ConsoleCommand("css_slay", "Убить игрока")]
    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, usage: "<имя/userid>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnSlayCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var target = FindTarget(command.GetArg(1));
        if (target == null || target.PlayerPawn.Value == null)
        {
            command.ReplyToCommand($" {ChatColors.Red}[ADMIN] Игрок не найден");
            return;
        }

        target.PlayerPawn.Value.CommitSuicide(false, true);
        
        string adminName = caller?.PlayerName ?? "Console";
        Server.PrintToChatAll($" {ChatColors.Red}[ADMIN] {adminName} убил {target.PlayerName}");
    }

    [ConsoleCommand("css_slap", "Ударить игрока")]
    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, usage: "<имя/userid> [урон]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnSlapCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var target = FindTarget(command.GetArg(1));
        if (target == null || target.PlayerPawn.Value == null)
        {
            command.ReplyToCommand($" {ChatColors.Red}[ADMIN] Игрок не найден");
            return;
        }

        int damage = command.ArgCount > 2 ? int.Parse(command.GetArg(2)) : 10;
        
        var pawn = target.PlayerPawn.Value;
        pawn.Health -= damage;
        
        if (pawn.Health <= 0)
        {
            pawn.CommitSuicide(false, true);
        }

        var velocity = new Vector(
            Random.Shared.Next(-100, 100),
            Random.Shared.Next(-100, 100),
            Random.Shared.Next(50, 150)
        );
        pawn.Teleport(pawn.AbsOrigin, pawn.AbsRotation, velocity);

        string adminName = caller?.PlayerName ?? "Console";
        Server.PrintToChatAll($" {ChatColors.Yellow}[ADMIN] {adminName} ударил {target.PlayerName} (-{damage} HP)");
    }

    [ConsoleCommand("css_tp", "Телепортировать к игроку")]
    [RequiresPermissions("@css/cheats")]
    [CommandHelper(minArgs: 1, usage: "<имя/userid>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnTpCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null || caller.PlayerPawn.Value == null)
            return;

        var target = FindTarget(command.GetArg(1));
        if (target == null || target.PlayerPawn.Value == null)
        {
            command.ReplyToCommand($" {ChatColors.Red}[ADMIN] Игрок не найден");
            return;
        }

        var targetPos = target.PlayerPawn.Value.AbsOrigin;
        if (targetPos != null)
        {
            caller.PlayerPawn.Value.Teleport(targetPos, target.PlayerPawn.Value.AbsRotation, new Vector(0, 0, 0));
            caller.PrintToChat($" {ChatColors.Green}[ADMIN] Телепортация к {target.PlayerName}");
        }
    }

    [ConsoleCommand("css_bring", "Телепортировать игрока к себе")]
    [RequiresPermissions("@css/cheats")]
    [CommandHelper(minArgs: 1, usage: "<имя/userid>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnBringCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null || caller.PlayerPawn.Value == null)
            return;

        var target = FindTarget(command.GetArg(1));
        if (target == null || target.PlayerPawn.Value == null)
        {
            command.ReplyToCommand($" {ChatColors.Red}[ADMIN] Игрок не найден");
            return;
        }

        var callerPos = caller.PlayerPawn.Value.AbsOrigin;
        if (callerPos != null)
        {
            target.PlayerPawn.Value.Teleport(callerPos, caller.PlayerPawn.Value.AbsRotation, new Vector(0, 0, 0));
            caller.PrintToChat($" {ChatColors.Green}[ADMIN] {target.PlayerName} телепортирован к вам");
        }
    }

    [ConsoleCommand("css_hp", "Установить здоровье игроку")]
    [RequiresPermissions("@css/cheats")]
    [CommandHelper(minArgs: 2, usage: "<имя/userid> <hp>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnHpCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var target = FindTarget(command.GetArg(1));
        if (target == null || target.PlayerPawn.Value == null)
        {
            command.ReplyToCommand($" {ChatColors.Red}[ADMIN] Игрок не найден");
            return;
        }

        int hp = int.Parse(command.GetArg(2));
        target.PlayerPawn.Value.Health = hp;
        target.PlayerPawn.Value.MaxHealth = hp > 100 ? hp : 100;

        string adminName = caller?.PlayerName ?? "Console";
        Server.PrintToChatAll($" {ChatColors.Green}[ADMIN] {adminName} установил {target.PlayerName} здоровье: {hp} HP");
    }

    [ConsoleCommand("css_speed", "Установить скорость игроку")]
    [RequiresPermissions("@css/cheats")]
    [CommandHelper(minArgs: 2, usage: "<имя/userid> <множитель>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnSpeedCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var target = FindTarget(command.GetArg(1));
        if (target == null || target.PlayerPawn.Value == null)
        {
            command.ReplyToCommand($" {ChatColors.Red}[ADMIN] Игрок не найден");
            return;
        }

        float speed = float.Parse(command.GetArg(2));
        target.PlayerPawn.Value.VelocityModifier = speed;

        string adminName = caller?.PlayerName ?? "Console";
        Server.PrintToChatAll($" {ChatColors.Yellow}[ADMIN] {adminName} установил {target.PlayerName} скорость: {speed}x");
    }

    [ConsoleCommand("css_gravity", "Установить гравитацию игроку")]
    [RequiresPermissions("@css/cheats")]
    [CommandHelper(minArgs: 2, usage: "<имя/userid> <множитель>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnGravityCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var target = FindTarget(command.GetArg(1));
        if (target == null || target.PlayerPawn.Value == null)
        {
            command.ReplyToCommand($" {ChatColors.Red}[ADMIN] Игрок не найден");
            return;
        }

        float gravity = float.Parse(command.GetArg(2));
        target.PlayerPawn.Value.GravityScale = gravity;

        string adminName = caller?.PlayerName ?? "Console";
        Server.PrintToChatAll($" {ChatColors.Yellow}[ADMIN] {adminName} установил {target.PlayerName} гравитацию: {gravity}x");
    }

    [ConsoleCommand("css_noclip", "Включить noclip")]
    [RequiresPermissions("@css/cheats")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnNoclipCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null || caller.PlayerPawn.Value == null)
            return;

        var pawn = caller.PlayerPawn.Value;
        var currentMode = pawn.MoveType;

        if (currentMode == MoveType_t.MOVETYPE_NOCLIP)
        {
            pawn.MoveType = MoveType_t.MOVETYPE_WALK;
            caller.PrintToChat($" {ChatColors.Red}[ADMIN] Noclip выключен");
        }
        else
        {
            pawn.MoveType = MoveType_t.MOVETYPE_NOCLIP;
            caller.PrintToChat($" {ChatColors.Green}[ADMIN] Noclip включен");
        }
    }

    [ConsoleCommand("css_god", "Включить режим бога")]
    [RequiresPermissions("@css/cheats")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnGodCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null || caller.PlayerPawn.Value == null)
            return;

        var pawn = caller.PlayerPawn.Value;
        bool isGod = pawn.TakesDamage;

        pawn.TakesDamage = !isGod;

        if (!isGod)
        {
            caller.PrintToChat($" {ChatColors.Red}[ADMIN] Режим бога выключен");
        }
        else
        {
            caller.PrintToChat($" {ChatColors.Green}[ADMIN] Режим бога включен");
        }
    }

    [ConsoleCommand("css_a1", "Админ: Показать спавны CT")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnA1Command(CCSPlayerController? caller, CommandInfo command)
    {
        Server.ExecuteCommand("css_showspawns CT");
    }

    [ConsoleCommand("css_a2", "Админ: Показать спавны T")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnA2Command(CCSPlayerController? caller, CommandInfo command)
    {
        Server.ExecuteCommand("css_showspawns T");
    }

    [ConsoleCommand("css_a3", "Админ: Скрыть маркеры")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnA3Command(CCSPlayerController? caller, CommandInfo command)
    {
        Server.ExecuteCommand("css_hidespawns");
    }

    [ConsoleCommand("css_a4", "Админ: Добавить спавн CT")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnA4Command(CCSPlayerController? caller, CommandInfo command)
    {
        Server.ExecuteCommand("css_addspawn CT");
    }

    [ConsoleCommand("css_a5", "Админ: Добавить спавн T")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnA5Command(CCSPlayerController? caller, CommandInfo command)
    {
        Server.ExecuteCommand("css_addspawn T");
    }

    [ConsoleCommand("css_a6", "Админ: Удалить ближайший спавн")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnA6Command(CCSPlayerController? caller, CommandInfo command)
    {
        Server.ExecuteCommand("css_removespawn");
    }

    [ConsoleCommand("css_a7", "Админ: Добавить подарок 1000")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnA7Command(CCSPlayerController? caller, CommandInfo command)
    {
        Server.ExecuteCommand("css_addgift 1000");
    }

    [ConsoleCommand("css_a8", "Админ: Список подарков")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnA8Command(CCSPlayerController? caller, CommandInfo command)
    {
        Server.ExecuteCommand("css_listgifts");
    }

    [ConsoleCommand("css_a9", "Админ: Удалить все подарки")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnA9Command(CCSPlayerController? caller, CommandInfo command)
    {
        Server.ExecuteCommand("css_removegifts");
    }

    [ConsoleCommand("css_respawn", "Возродить игрока")]
    [RequiresPermissions("@css/cheats")]
    [CommandHelper(minArgs: 1, usage: "<имя/userid>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnRespawnCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var target = FindTarget(command.GetArg(1));
        if (target == null)
        {
            command.ReplyToCommand($" {ChatColors.Red}[ADMIN] Игрок не найден");
            return;
        }

        target.Respawn();

        string adminName = caller?.PlayerName ?? "Console";
        Server.PrintToChatAll($" {ChatColors.Green}[ADMIN] {adminName} возродил {target.PlayerName}");
    }

    [ConsoleCommand("css_freeze", "Заморозить игрока")]
    [RequiresPermissions("@css/slay")]
    [CommandHelper(minArgs: 1, usage: "<имя/userid>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnFreezeCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var target = FindTarget(command.GetArg(1));
        if (target == null || target.PlayerPawn.Value == null)
        {
            command.ReplyToCommand($" {ChatColors.Red}[ADMIN] Игрок не найден");
            return;
        }

        var pawn = target.PlayerPawn.Value;
        bool isFrozen = pawn.MoveType == MoveType_t.MOVETYPE_OBSOLETE;

        if (isFrozen)
        {
            pawn.MoveType = MoveType_t.MOVETYPE_WALK;
            target.PrintToChat($" {ChatColors.Green}[ADMIN] Вы разморожены");
        }
        else
        {
            pawn.MoveType = MoveType_t.MOVETYPE_OBSOLETE;
            target.PrintToChat($" {ChatColors.Red}[ADMIN] Вы заморожены");
        }

        string adminName = caller?.PlayerName ?? "Console";
        string status = isFrozen ? "разморозил" : "заморозил";
        Server.PrintToChatAll($" {ChatColors.Yellow}[ADMIN] {adminName} {status} {target.PlayerName}");
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