using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;

namespace InstantRespawnPlugin;

public class InstantRespawnPlugin : BasePlugin
{
    public override string ModuleName => "Instant Respawn";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Мгновенный респавн игроков после смерти в CS2";

    private bool _respawnEnabled = true;
    private float _respawnDelay = 0.5f;

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        
        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
        Console.WriteLine($"[{ModuleName}] Респавн включен, задержка: {_respawnDelay} сек");
    }

    [ConsoleCommand("css_respawn", "Возродить игрока")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 0, usage: "[имя игрока]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnRespawnCommand(CCSPlayerController? caller, CommandInfo command)
    {
        CCSPlayerController? target = null;

        if (command.ArgCount > 1)
        {
            string targetName = command.GetArg(1);
            var players = Utilities.GetPlayers();
            target = players.FirstOrDefault(p => p?.PlayerName?.Contains(targetName, StringComparison.OrdinalIgnoreCase) == true);

            if (target == null)
            {
                if (caller != null)
                    caller.PrintToChat($" {ChatColors.Green}[Respawn]{ChatColors.Default} Игрок не найден");
                return;
            }
        }
        else
        {
            if (caller == null)
            {
                Console.WriteLine("[Respawn] Укажите имя игрока");
                return;
            }
            target = caller;
        }

        if (target?.IsValid == true)
        {
            RespawnPlayer(target);
            
            if (caller != null)
                caller.PrintToChat($" {ChatColors.Green}[Respawn]{ChatColors.Default} Игрок {target.PlayerName} возрождён");
        }
    }

    [ConsoleCommand("css_respawn_toggle", "Включить/выключить автореспавн")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnRespawnToggleCommand(CCSPlayerController? caller, CommandInfo command)
    {
        _respawnEnabled = !_respawnEnabled;

        string status = _respawnEnabled ? "включен" : "выключен";
        
        if (caller != null)
            caller.PrintToChat($" {ChatColors.Green}[Respawn]{ChatColors.Default} Автореспавн {status}");
        
        Console.WriteLine($"[Respawn] Auto-respawn {(_respawnEnabled ? "enabled" : "disabled")}");
        Server.PrintToChatAll($" {ChatColors.Green}[Respawn]{ChatColors.Default} Автореспавн {status}");
    }

    [ConsoleCommand("css_respawn_delay", "Установить задержку респавна")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 1, usage: "<секунды>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnRespawnDelayCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (!float.TryParse(command.GetArg(1), out float delay) || delay < 0)
        {
            if (caller != null)
                caller.PrintToChat($" {ChatColors.Green}[Respawn]{ChatColors.Default} Неверное значение (0 и больше)");
            return;
        }

        _respawnDelay = delay;

        if (caller != null)
            caller.PrintToChat($" {ChatColors.Green}[Respawn]{ChatColors.Default} Задержка респавна: {delay} сек");
        
        Console.WriteLine($"[Respawn] Respawn delay set to {delay}s");
    }

    [ConsoleCommand("css_respawn_info", "Информация о респавне")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnRespawnInfoCommand(CCSPlayerController? caller, CommandInfo command)
    {
        string status = _respawnEnabled ? $"{ChatColors.Green}включен" : $"{ChatColors.Red}выключен";
        
        if (caller != null)
        {
            caller.PrintToChat($" {ChatColors.Green}[Respawn]{ChatColors.Default} Автореспавн: {status}");
            caller.PrintToChat($" {ChatColors.Green}[Respawn]{ChatColors.Default} Задержка: {ChatColors.Yellow}{_respawnDelay} сек");
        }
        else
        {
            Console.WriteLine($"[Respawn] Auto-respawn: {(_respawnEnabled ? "enabled" : "disabled")}");
            Console.WriteLine($"[Respawn] Delay: {_respawnDelay}s");
        }
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (!_respawnEnabled)
            return HookResult.Continue;

        var player = @event.Userid;
        
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        AddTimer(_respawnDelay, () =>
        {
            if (player.IsValid && player.Connected == PlayerConnectedState.PlayerConnected)
            {
                RespawnPlayer(player);
            }
        });

        return HookResult.Continue;
    }

    private void RespawnPlayer(CCSPlayerController player)
    {
        if (player?.PlayerPawn?.Value == null)
            return;

        if (player.Team == CsTeam.Spectator || player.Team == CsTeam.None)
            return;

        player.Respawn();
        
        Server.NextFrame(() =>
        {
            if (player.PlayerPawn?.Value != null)
            {
                player.PlayerPawn.Value.Health = 100;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
            }
        });
    }
}
