using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;

namespace AdminPlugin;

public class AdminPlugin : BasePlugin
{
    public override string ModuleName => "Admin Panel";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Админ-панель для управления сервером CS2";

    private readonly Dictionary<ulong, string> _playerMenuContext = new();
    private readonly Dictionary<ulong, bool> _playerFlyMode = new();
    private readonly Dictionary<ulong, bool> _playerGodMode = new();
    private readonly Dictionary<ulong, bool> _playerFrozen = new();
    private readonly Dictionary<ulong, string> _playerSelectionAction = new();

    public override void Load(bool hotReload)
    {
        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
    }

    [ConsoleCommand("css_admin", "Открыть админ-панель")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnAdminCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} {ChatColors.Red}У вас нет прав администратора!");
            return;
        }

        ulong steamId = player.SteamID;
        _playerMenuContext[steamId] = "admin_main";
        ShowAdminPanel(player);
    }

    [ConsoleCommand("css_1", "Пункт меню 1")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnMenu1Command(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ulong steamId = player.SteamID;
        string context = _playerMenuContext.ContainsKey(steamId) ? _playerMenuContext[steamId] : "";
        
        Console.WriteLine($"[Admin] OnMenu1Command: context={context}, steamId={steamId}");

        if (context == "admin_main")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                _playerMenuContext[steamId] = "admin_players";
                ShowPlayersManagement(player);
            }
            return;
        }
        else if (context == "admin_players")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                _playerSelectionAction[steamId] = "kill";
                _playerMenuContext[steamId] = "admin_player_select";
                ShowPlayerList(player, "Выберите игрока для убийства:");
            }
            return;
        }
        else if (context == "admin_player_select")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                ExecutePlayerAction(player, 1);
            }
            return;
        }
        else if (context == "admin_cheats")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                ToggleFlyMode(player);
            }
            return;
        }
        else if (context == "admin_zones")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                SetStartZone(player);
            }
            return;
        }
    }

    [ConsoleCommand("css_2", "Пункт меню 2")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnMenu2Command(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ulong steamId = player.SteamID;
        string context = _playerMenuContext.ContainsKey(steamId) ? _playerMenuContext[steamId] : "";

        if (context == "admin_main")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                player.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Раздел 2: Модерация");
            }
            return;
        }
        else if (context == "admin_players")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                _playerSelectionAction[steamId] = "kick";
                _playerMenuContext[steamId] = "admin_player_select";
                ShowPlayerList(player, "Выберите игрока для кика:");
            }
            return;
        }
        else if (context == "admin_player_select")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                ExecutePlayerAction(player, 2);
            }
            return;
        }
        else if (context == "admin_cheats")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                ToggleGodMode(player);
            }
            return;
        }
        else if (context == "admin_zones")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                SetFinishZone(player);
            }
            return;
        }
    }

    [ConsoleCommand("css_3", "Пункт меню 3")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnMenu3Command(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ulong steamId = player.SteamID;
        string context = _playerMenuContext.ContainsKey(steamId) ? _playerMenuContext[steamId] : "";

        if (context == "admin_main")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                _playerMenuContext[steamId] = "admin_cheats";
                ShowCheatsManagement(player);
            }
            return;
        }
        else if (context == "admin_players")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                _playerSelectionAction[steamId] = "ban";
                _playerMenuContext[steamId] = "admin_player_select";
                ShowPlayerList(player, "Выберите игрока для бана:");
            }
            return;
        }
        else if (context == "admin_player_select")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                ExecutePlayerAction(player, 3);
            }
            return;
        }
        else if (context == "admin_cheats")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                ToggleFlyMode(player);
            }
            return;
        }
    }

    [ConsoleCommand("css_4", "Пункт меню 4")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnMenu4Command(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ulong steamId = player.SteamID;
        string context = _playerMenuContext.ContainsKey(steamId) ? _playerMenuContext[steamId] : "";

        if (context == "admin_main")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                _playerMenuContext[steamId] = "admin_zones";
                ShowZonesManagement(player);
            }
            return;
        }
        else if (context == "admin_players")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                _playerSelectionAction[steamId] = "slap";
                _playerMenuContext[steamId] = "admin_player_select";
                ShowPlayerList(player, "Выберите игрока для шлепка:");
            }
            return;
        }
        else if (context == "admin_player_select")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                ExecutePlayerAction(player, 4);
            }
            return;
        }
    }

    [ConsoleCommand("css_5", "Команда 5")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnMenu5Command(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ulong steamId = player.SteamID;
        string context = _playerMenuContext.ContainsKey(steamId) ? _playerMenuContext[steamId] : "";

        if (context == "admin_players")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                _playerSelectionAction[steamId] = "freeze";
                _playerMenuContext[steamId] = "admin_player_select";
                ShowPlayerList(player, "Выберите игрока для заморозки:");
            }
        }
        else if (context == "admin_player_select")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                ExecutePlayerAction(player, 5);
            }
        }
    }

    [ConsoleCommand("css_6", "Команда 6")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnMenu6Command(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ulong steamId = player.SteamID;
        string context = _playerMenuContext.ContainsKey(steamId) ? _playerMenuContext[steamId] : "";

        if (context == "admin_players")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                _playerSelectionAction[steamId] = "respawn";
                _playerMenuContext[steamId] = "admin_player_select";
                ShowPlayerList(player, "Выберите игрока для возрождения:");
            }
        }
        else if (context == "admin_player_select")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                ExecutePlayerAction(player, 6);
            }
        }
    }

    [ConsoleCommand("css_7", "Команда 7")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnMenu7Command(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ulong steamId = player.SteamID;
        string context = _playerMenuContext.ContainsKey(steamId) ? _playerMenuContext[steamId] : "";

        if (context == "admin_players" && AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            _playerSelectionAction[steamId] = "teleport_to_me";
            _playerMenuContext[steamId] = "admin_player_select";
            ShowPlayerList(player, "Выберите игрока для телепортации к себе:");
        }
        else if (context == "admin_player_select" && AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            ExecutePlayerAction(player, 7);
        }
    }

    [ConsoleCommand("css_8", "Команда 8")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnMenu8Command(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ulong steamId = player.SteamID;
        string context = _playerMenuContext.ContainsKey(steamId) ? _playerMenuContext[steamId] : "";

        if (context == "admin_players" && AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            _playerSelectionAction[steamId] = "teleport_to_player";
            _playerMenuContext[steamId] = "admin_player_select";
            ShowPlayerList(player, "Выберите игрока для телепортации к нему:");
        }
        else if (context == "admin_player_select" && AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            ExecutePlayerAction(player, 8);
        }
    }

    private void ShowAdminPanel(CCSPlayerController player)
    {
        player.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} {ChatColors.Red}Админ-панель:");
        player.PrintToChat($" {ChatColors.Yellow}!1{ChatColors.Default} - Управление игроками");
        player.PrintToChat($" {ChatColors.Yellow}!2{ChatColors.Default} - Модерация");
        player.PrintToChat($" {ChatColors.Yellow}!3{ChatColors.Default} - Читы и настройки");
        player.PrintToChat($" {ChatColors.Yellow}!4{ChatColors.Default} - Настройки зон карт");
    }

    private void ShowPlayersManagement(CCSPlayerController player)
    {
        player.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} {ChatColors.Red}Управление игроками:");
        player.PrintToChat($" {ChatColors.Yellow}!1{ChatColors.Default} - Убить");
        player.PrintToChat($" {ChatColors.Yellow}!2{ChatColors.Default} - Кикнуть");
        player.PrintToChat($" {ChatColors.Yellow}!3{ChatColors.Default} - Забанить");
        player.PrintToChat($" {ChatColors.Yellow}!4{ChatColors.Default} - Шлепнуть");
        
        AddTimer(0.1f, () =>
        {
            if (!player.IsValid) return;
            player.PrintToChat($" {ChatColors.Yellow}!5{ChatColors.Default} - Заморозить");
            player.PrintToChat($" {ChatColors.Yellow}!6{ChatColors.Default} - Возродить");
            player.PrintToChat($" {ChatColors.Yellow}!7{ChatColors.Default} - Телепортировать к себе");
            player.PrintToChat($" {ChatColors.Yellow}!8{ChatColors.Default} - Телепортироваться к игроку");
            player.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Назад: !admin");
        });
    }

    private void ShowCheatsManagement(CCSPlayerController player)
    {
        player.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} {ChatColors.Red}Читы и настройки:");
        player.PrintToChat($" {ChatColors.Yellow}!1{ChatColors.Default} - Режим полёта");
        player.PrintToChat($" {ChatColors.Yellow}!2{ChatColors.Default} - Режим неуязвимости");
        player.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Назад: !admin");
    }

    private void ShowZonesManagement(CCSPlayerController player)
    {
        player.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} {ChatColors.Red}Настройки зон карт:");
        player.PrintToChat($" {ChatColors.Yellow}!1{ChatColors.Default} - Установить зону старта");
        player.PrintToChat($" {ChatColors.Yellow}!2{ChatColors.Default} - Установить зону финиша");
        player.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Назад: !admin");
    }

    private void ShowPlayerList(CCSPlayerController admin, string title)
    {
        var players = Utilities.GetPlayers().Where(p => p?.IsValid == true && !p.IsBot).ToList();
        
        admin.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} {title}");
        
        if (players.Count == 0)
        {
            admin.PrintToChat($" {ChatColors.Red}Нет доступных игроков");
            return;
        }

        for (int i = 0; i < Math.Min(players.Count, 8); i++)
        {
            var p = players[i];
            admin.PrintToChat($" {ChatColors.Yellow}!{i + 1}{ChatColors.Default} {p.PlayerName}");
        }
    }

    private void ExecutePlayerAction(CCSPlayerController admin, int playerNum)
    {
        ulong steamId = admin.SteamID;
        
        if (!_playerSelectionAction.ContainsKey(steamId))
        {
            admin.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} {ChatColors.Red}Ошибка выбора действия!");
            return;
        }

        var players = Utilities.GetPlayers().Where(p => p?.IsValid == true && !p.IsBot).ToList();
        
        if (playerNum < 1 || playerNum > players.Count || playerNum > 8)
        {
            admin.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} {ChatColors.Red}Игрок не найден!");
            return;
        }

        var target = players[playerNum - 1];
        string action = _playerSelectionAction[steamId];
        
        Console.WriteLine($"[Admin] ExecutePlayerAction: action={action}, target={target.PlayerName}, playerNum={playerNum}");

        switch (action)
        {
            case "kill":
                if (target.PlayerPawn?.Value != null)
                {
                    target.PlayerPawn.Value.CommitSuicide(false, true);
                    admin.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Игрок {target.PlayerName} убит");
                }
                break;

            case "kick":
                Server.ExecuteCommand($"kickid {target.UserId}");
                admin.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Игрок {target.PlayerName} кикнут");
                break;

            case "ban":
                Server.ExecuteCommand($"banid 60 {target.UserId}");
                admin.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Игрок {target.PlayerName} забанен на 60 минут");
                break;

            case "slap":
                if (target.PlayerPawn?.Value != null)
                {
                    target.PlayerPawn.Value.Health -= 5;
                    target.PlayerPawn.Value.Teleport(target.PlayerPawn.Value.AbsOrigin, target.PlayerPawn.Value.EyeAngles, new Vector(0, 0, 300));
                    admin.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Игрок {target.PlayerName} шлёпнут");
                }
                break;

            case "freeze":
                ulong targetId = target.SteamID;
                bool isFrozen = _playerFrozen.ContainsKey(targetId) && _playerFrozen[targetId];
                _playerFrozen[targetId] = !isFrozen;
                
                if (_playerFrozen[targetId] && target.PlayerPawn?.Value != null)
                {
                    target.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_OBSOLETE;
                    admin.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Игрок {target.PlayerName} заморожен");
                    target.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Вы заморожены администратором");
                }
                else if (target.PlayerPawn?.Value != null)
                {
                    target.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_WALK;
                    admin.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Игрок {target.PlayerName} разморожен");
                    target.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Вы разморожены");
                }
                break;

            case "respawn":
                target.Respawn();
                admin.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Игрок {target.PlayerName} возрождён");
                break;

            case "teleport_to_me":
                var adminPos = admin.PlayerPawn?.Value?.AbsOrigin;
                var adminAngles = admin.PlayerPawn?.Value?.EyeAngles;
                if (adminPos != null && adminAngles != null && target.PlayerPawn?.Value != null)
                {
                    target.PlayerPawn.Value.Teleport(adminPos, adminAngles, new Vector(0, 0, 0));
                    admin.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Игрок {target.PlayerName} телепортирован к вам");
                    target.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Вы телепортированы к администратору");
                }
                break;

            case "teleport_to_player":
                var targetPos = target.PlayerPawn?.Value?.AbsOrigin;
                var targetAngles = target.PlayerPawn?.Value?.EyeAngles;
                if (targetPos != null && targetAngles != null && admin.PlayerPawn?.Value != null)
                {
                    admin.PlayerPawn.Value.Teleport(targetPos, targetAngles, new Vector(0, 0, 0));
                    admin.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Вы телепортированы к игроку {target.PlayerName}");
                }
                break;
        }

        _playerSelectionAction.Remove(steamId);
        _playerMenuContext[steamId] = "admin_players";
        ShowPlayersManagement(admin);
    }

    private void ToggleFlyMode(CCSPlayerController player)
    {
        ulong steamId = player.SteamID;
        bool isEnabled = _playerFlyMode.ContainsKey(steamId) && _playerFlyMode[steamId];
        
        _playerFlyMode[steamId] = !isEnabled;
        
        if (player.PlayerPawn?.Value != null)
        {
            if (_playerFlyMode[steamId])
            {
                player.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_NOCLIP;
                player.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Режим полёта {ChatColors.Green}включён");
            }
            else
            {
                player.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_WALK;
                player.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Режим полёта {ChatColors.Red}выключен");
            }
        }
    }

    private void ToggleGodMode(CCSPlayerController player)
    {
        ulong steamId = player.SteamID;
        bool isEnabled = _playerGodMode.ContainsKey(steamId) && _playerGodMode[steamId];
        
        _playerGodMode[steamId] = !isEnabled;
        
        if (player.PlayerPawn?.Value != null)
        {
            player.PlayerPawn.Value.TakesDamage = !_playerGodMode[steamId];
        }
        
        if (_playerGodMode[steamId])
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Режим неуязвимости {ChatColors.Green}включён");
        }
        else
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Режим неуязвимости {ChatColors.Red}выключен");
        }
    }

    private void SetStartZone(CCSPlayerController player)
    {
        var pos = player.PlayerPawn?.Value?.AbsOrigin;
        if (pos == null)
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} {ChatColors.Red}Не удалось получить позицию!");
            return;
        }
        
        player.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Зона старта установлена на ({pos.X:F1}, {pos.Y:F1}, {pos.Z:F1})");
        Console.WriteLine($"[Admin] Зона старта: ({pos.X:F1}, {pos.Y:F1}, {pos.Z:F1})");
    }

    private void SetFinishZone(CCSPlayerController player)
    {
        var pos = player.PlayerPawn?.Value?.AbsOrigin;
        if (pos == null)
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} {ChatColors.Red}Не удалось получить позицию!");
            return;
        }
        
        player.PrintToChat($" {ChatColors.Green}[Okyes Admin]{ChatColors.Default} Зона финиша установлена на ({pos.X:F1}, {pos.Y:F1}, {pos.Z:F1})");
        Console.WriteLine($"[Admin] Зона финиша: ({pos.X:F1}, {pos.Y:F1}, {pos.Z:F1})");
    }

    public override void Unload(bool hotReload)
    {
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}
