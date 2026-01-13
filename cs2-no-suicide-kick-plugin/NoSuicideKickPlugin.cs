using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Cvars;

namespace NoSuicideKickPlugin;

public class NoSuicideKickPlugin : BasePlugin
{
    public override string ModuleName => "No Suicide Kick";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Отключает кик игроков за самоубийства в CS2";

    private readonly Dictionary<int, int> _suicideCount = new();
    private ConVar? _maxSuicides;

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
        
        _maxSuicides = ConVar.Find("mp_autokick");
        
        if (_maxSuicides != null)
        {
            _maxSuicides.SetValue(false);
            Console.WriteLine($"[{ModuleName}] mp_autokick отключен");
        }
        
        var teamKillKick = ConVar.Find("mp_autoteambalance");
        if (teamKillKick != null)
        {
            Console.WriteLine($"[{ModuleName}] Текущее значение mp_autoteambalance: {teamKillKick.GetPrimitiveValue<bool>()}");
        }
        
        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
    }

    [ConsoleCommand("css_suicide_stats", "Показать статистику самоубийств")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnSuicideStatsCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller != null && caller.IsValid)
        {
            int userId = (int)caller.UserId!;
            int count = _suicideCount.ContainsKey(userId) ? _suicideCount[userId] : 0;
            
            caller.PrintToChat($" {ChatColors.Green}[No Suicide Kick]{ChatColors.Default} Ваши самоубийства: {ChatColors.Yellow}{count}");
        }
        else
        {
            Console.WriteLine("[No Suicide Kick] Статистика самоубийств:");
            foreach (var kvp in _suicideCount)
            {
                var player = Utilities.GetPlayerFromUserid(kvp.Key);
                string name = player?.PlayerName ?? $"UserID {kvp.Key}";
                Console.WriteLine($"  {name}: {kvp.Value}");
            }
        }
    }

    [ConsoleCommand("css_reset_suicides", "Сбросить счётчик самоубийств")]
    [CommandHelper(minArgs: 0, usage: "[имя игрока]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnResetSuicidesCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (command.ArgCount > 1)
        {
            string targetName = command.GetArg(1);
            var players = Utilities.GetPlayers();
            var target = players.FirstOrDefault(p => p?.PlayerName?.Contains(targetName, StringComparison.OrdinalIgnoreCase) == true);

            if (target == null)
            {
                if (caller != null)
                    caller.PrintToChat($" {ChatColors.Green}[No Suicide Kick]{ChatColors.Default} Игрок не найден");
                else
                    Console.WriteLine("[No Suicide Kick] Игрок не найден");
                return;
            }

            int userId = (int)target.UserId!;
            _suicideCount.Remove(userId);

            if (caller != null)
                caller.PrintToChat($" {ChatColors.Green}[No Suicide Kick]{ChatColors.Default} Счётчик {target.PlayerName} сброшен");
            
            Console.WriteLine($"[No Suicide Kick] Счётчик самоубийств для {target.PlayerName} сброшен");
        }
        else
        {
            if (caller != null && caller.IsValid)
            {
                int userId = (int)caller.UserId!;
                _suicideCount.Remove(userId);
                caller.PrintToChat($" {ChatColors.Green}[No Suicide Kick]{ChatColors.Default} Ваш счётчик сброшен");
            }
            else
            {
                _suicideCount.Clear();
                Console.WriteLine("[No Suicide Kick] Все счётчики сброшены");
            }
        }
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        var attacker = @event.Attacker;

        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (player == attacker || attacker == null)
        {
            int userId = (int)player.UserId!;
            
            if (!_suicideCount.ContainsKey(userId))
                _suicideCount[userId] = 0;
            
            _suicideCount[userId]++;
            
            Console.WriteLine($"[No Suicide Kick] {player.PlayerName} совершил самоубийство (всего: {_suicideCount[userId]})");
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        int userId = (int)player.UserId!;
        _suicideCount.Remove(userId);

        return HookResult.Continue;
    }

    private HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        int userId = (int)player.UserId!;
        _suicideCount[userId] = 0;

        return HookResult.Continue;
    }

    public override void Unload(bool hotReload)
    {
        _suicideCount.Clear();
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}
