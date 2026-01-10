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
    public override string ModuleVersion => "1.0.3";
    public override string ModuleAuthor => "poehali.dev";
    public override string ModuleDescription => "Полнофункциональная админка для CS2";

    private CCSPlayerController? _selectedTarget;

    public override void Load(bool hotReload)
    {
        AddCommand("css_admin", "Открыть админ-меню", OnAdminMenuCommand);
        
        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
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
        var menu = new ChatMenu("Админ-Меню");

        menu.AddMenuOption("Управление игроками", (controller, option) =>
        {
            ShowPlayerManagementMenu(controller);
        });

        menu.AddMenuOption("Модерация", (controller, option) =>
        {
            ShowModerationMenu(controller);
        });

        menu.AddMenuOption("Читы и настройки", (controller, option) =>
        {
            ShowCheatsMenu(controller);
        });

        MenuManager.OpenChatMenu(player, menu);
    }

    private void ShowPlayerManagementMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu("Управление игроками");

        menu.AddMenuOption("Телепортация к игроку", (controller, option) =>
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

        menu.AddMenuOption("Притянуть игрока", (controller, option) =>
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

        menu.AddMenuOption("Возродить игрока", (controller, option) =>
        {
            ShowPlayerSelectMenu(controller, "Выберите игрока", (target) =>
            {
                target.Respawn();
                controller.PrintToChat($" {ChatColors.Green}[ADMIN] {target.PlayerName} возрождён");
            });
        });

        menu.AddMenuOption("← Назад", (controller, option) =>
        {
            ShowMainMenu(controller);
        });

        MenuManager.OpenChatMenu(player, menu);
    }

    private void ShowModerationMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu("Модерация");

        menu.AddMenuOption("Кикнуть игрока", (controller, option) =>
        {
            ShowPlayerSelectMenu(controller, "Выберите игрока для кика", (target) =>
            {
                Server.ExecuteCommand($"kickid {target.UserId}");
                Server.PrintToChatAll($" {ChatColors.Red}[ADMIN] {controller.PlayerName} кикнул {target.PlayerName}");
            });
        });

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

        menu.AddMenuOption("Ударить (10 урона)", (controller, option) =>
        {
            ShowPlayerSelectMenu(controller, "Выберите игрока", (target) =>
            {
                if (target.PlayerPawn.Value != null)
                {
                    var pawn = target.PlayerPawn.Value;
                    pawn.Health -= 10;
                    
                    if (pawn.Health <= 0)
                        pawn.CommitSuicide(false, true);

                    var velocity = new Vector(
                        Random.Shared.Next(-100, 100),
                        Random.Shared.Next(-100, 100),
                        Random.Shared.Next(50, 150)
                    );
                    pawn.Teleport(pawn.AbsOrigin, pawn.AbsRotation, velocity);

                    Server.PrintToChatAll($" {ChatColors.Yellow}[ADMIN] {controller.PlayerName} ударил {target.PlayerName}");
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

                    string status = isFrozen ? "разморозил" : "заморозил";
                    Server.PrintToChatAll($" {ChatColors.Yellow}[ADMIN] {controller.PlayerName} {status} {target.PlayerName}");
                }
            });
        });

        menu.AddMenuOption("← Назад", (controller, option) =>
        {
            ShowMainMenu(controller);
        });

        MenuManager.OpenChatMenu(player, menu);
    }

    private void ShowCheatsMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu("Читы и настройки");

        menu.AddMenuOption("Noclip (себе)", (controller, option) =>
        {
            if (controller.PlayerPawn.Value != null)
            {
                var pawn = controller.PlayerPawn.Value;
                var currentMode = pawn.MoveType;

                if (currentMode == MoveType_t.MOVETYPE_NOCLIP)
                {
                    pawn.MoveType = MoveType_t.MOVETYPE_WALK;
                    controller.PrintToChat($" {ChatColors.Red}[ADMIN] Noclip выключен");
                }
                else
                {
                    pawn.MoveType = MoveType_t.MOVETYPE_NOCLIP;
                    controller.PrintToChat($" {ChatColors.Green}[ADMIN] Noclip включен");
                }
            }
            ShowCheatsMenu(controller);
        });

        menu.AddMenuOption("God Mode (себе)", (controller, option) =>
        {
            if (controller.PlayerPawn.Value != null)
            {
                var pawn = controller.PlayerPawn.Value;
                bool isGod = pawn.TakesDamage;

                pawn.TakesDamage = !isGod;

                if (!isGod)
                    controller.PrintToChat($" {ChatColors.Red}[ADMIN] Режим бога выключен");
                else
                    controller.PrintToChat($" {ChatColors.Green}[ADMIN] Режим бога включен");
            }
            ShowCheatsMenu(controller);
        });

        menu.AddMenuOption("Здоровье 200 HP", (controller, option) =>
        {
            ShowPlayerSelectMenu(controller, "Выберите игрока", (target) =>
            {
                if (target.PlayerPawn.Value != null)
                {
                    target.PlayerPawn.Value.Health = 200;
                    target.PlayerPawn.Value.MaxHealth = 200;
                    controller.PrintToChat($" {ChatColors.Green}[ADMIN] {target.PlayerName} установлено 200 HP");
                }
            });
        });

        menu.AddMenuOption("Скорость x2", (controller, option) =>
        {
            ShowPlayerSelectMenu(controller, "Выберите игрока", (target) =>
            {
                if (target.PlayerPawn.Value != null)
                {
                    target.PlayerPawn.Value.VelocityModifier = 2.0f;
                    controller.PrintToChat($" {ChatColors.Yellow}[ADMIN] {target.PlayerName} скорость x2");
                }
            });
        });

        menu.AddMenuOption("Гравитация 0.5x", (controller, option) =>
        {
            ShowPlayerSelectMenu(controller, "Выберите игрока", (target) =>
            {
                if (target.PlayerPawn.Value != null)
                {
                    target.PlayerPawn.Value.GravityScale = 0.5f;
                    controller.PrintToChat($" {ChatColors.Yellow}[ADMIN] {target.PlayerName} гравитация 0.5x");
                }
            });
        });

        menu.AddMenuOption("← Назад", (controller, option) =>
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