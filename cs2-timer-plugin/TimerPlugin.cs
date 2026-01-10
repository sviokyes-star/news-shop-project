using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;

namespace TimerPlugin;

public class TimerPlugin : BasePlugin
{
    public override string ModuleName => "Map Timer";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "poehali.dev";
    public override string ModuleDescription => "Таймер прохождения карты для CS2";

    private readonly Dictionary<int, PlayerTimer> _playerTimers = new();
    private readonly Dictionary<string, float> _mapRecords = new();

    private class PlayerTimer
    {
        public float StartTime { get; set; }
        public bool IsRunning { get; set; }
        public float BestTime { get; set; } = float.MaxValue;
    }

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnTick>(OnTick);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        
        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
    }

    [ConsoleCommand("css_timer", "Показать текущее время")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnTimerCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        int userId = (int)player.UserId!;
        
        if (!_playerTimers.ContainsKey(userId))
        {
            player.PrintToChat($" {ChatColors.Green}[TIMER]{ChatColors.Default} Таймер не запущен");
            return;
        }

        var timer = _playerTimers[userId];
        
        if (timer.IsRunning)
        {
            float currentTime = GetCurrentTime() - timer.StartTime;
            player.PrintToChat($" {ChatColors.Green}[TIMER]{ChatColors.Default} Текущее время: {ChatColors.Yellow}{FormatTime(currentTime)}");
        }
        
        if (timer.BestTime != float.MaxValue)
        {
            player.PrintToChat($" {ChatColors.Green}[TIMER]{ChatColors.Default} Ваш лучший результат: {ChatColors.Gold}{FormatTime(timer.BestTime)}");
        }

        string mapName = Server.MapName;
        if (_mapRecords.ContainsKey(mapName))
        {
            player.PrintToChat($" {ChatColors.Green}[TIMER]{ChatColors.Default} Рекорд карты: {ChatColors.Purple}{FormatTime(_mapRecords[mapName])}");
        }
    }

    [ConsoleCommand("css_restart", "Перезапустить таймер")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnRestartCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.PlayerPawn.Value == null)
            return;

        var pawn = player.PlayerPawn.Value;
        pawn.Teleport(pawn.AbsOrigin, pawn.AbsRotation, new Vector(0, 0, 0));
        
        StartTimer(player);
        player.PrintToChat($" {ChatColors.Green}[TIMER]{ChatColors.Default} Таймер перезапущен!");
    }

    [ConsoleCommand("css_top", "Показать топ-10 результатов")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnTopCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        var sortedTimers = _playerTimers
            .Where(t => t.Value.BestTime != float.MaxValue)
            .OrderBy(t => t.Value.BestTime)
            .Take(10)
            .ToList();

        if (sortedTimers.Count == 0)
        {
            player.PrintToChat($" {ChatColors.Green}[TIMER]{ChatColors.Default} Пока нет результатов");
            return;
        }

        player.PrintToChat($" {ChatColors.Green}[TIMER]{ChatColors.Default} Топ-10 результатов:");
        for (int i = 0; i < sortedTimers.Count; i++)
        {
            var playerController = Utilities.GetPlayerFromUserid(sortedTimers[i].Key);
            string playerName = playerController?.PlayerName ?? "Unknown";
            player.PrintToChat($" {i + 1}. {playerName}: {ChatColors.Yellow}{FormatTime(sortedTimers[i].Value.BestTime)}");
        }
    }

    private void OnTick()
    {
        var players = Utilities.GetPlayers();
        foreach (var player in players)
        {
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null)
                continue;

            int userId = (int)player.UserId!;
            
            if (!_playerTimers.ContainsKey(userId))
                continue;

            var timer = _playerTimers[userId];
            if (!timer.IsRunning)
                continue;

            float currentTime = GetCurrentTime() - timer.StartTime;
            
            player.PrintToCenterHtml($"<font color='#00ff00'>Время: {FormatTime(currentTime)}</font>");
        }
    }

    private void StartTimer(CCSPlayerController player)
    {
        int userId = (int)player.UserId!;
        
        if (!_playerTimers.ContainsKey(userId))
            _playerTimers[userId] = new PlayerTimer();

        _playerTimers[userId].StartTime = GetCurrentTime();
        _playerTimers[userId].IsRunning = true;
    }

    private void StopTimer(CCSPlayerController player)
    {
        int userId = (int)player.UserId!;
        
        if (!_playerTimers.ContainsKey(userId) || !_playerTimers[userId].IsRunning)
            return;

        var timer = _playerTimers[userId];
        float finalTime = GetCurrentTime() - timer.StartTime;
        timer.IsRunning = false;

        bool isNewRecord = finalTime < timer.BestTime;
        if (isNewRecord)
        {
            timer.BestTime = finalTime;
            player.PrintToChat($" {ChatColors.Green}[TIMER]{ChatColors.Default} Новый личный рекорд: {ChatColors.Gold}{FormatTime(finalTime)}!");
        }
        else
        {
            player.PrintToChat($" {ChatColors.Green}[TIMER]{ChatColors.Default} Время прохождения: {ChatColors.Yellow}{FormatTime(finalTime)}");
        }

        string mapName = Server.MapName;
        if (!_mapRecords.ContainsKey(mapName) || finalTime < _mapRecords[mapName])
        {
            _mapRecords[mapName] = finalTime;
            Server.PrintToChatAll($" {ChatColors.Green}[TIMER]{ChatColors.Default} {player.PlayerName} установил новый рекорд карты: {ChatColors.Purple}{FormatTime(finalTime)}!");
        }
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        AddTimer(1.0f, () => StartTimer(player));

        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        foreach (var kvp in _playerTimers)
        {
            kvp.Value.IsRunning = false;
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null)
            return HookResult.Continue;

        int userId = (int)player.UserId!;
        _playerTimers.Remove(userId);

        return HookResult.Continue;
    }

    private float GetCurrentTime()
    {
        return Server.CurrentTime;
    }

    private string FormatTime(float seconds)
    {
        int minutes = (int)(seconds / 60);
        int secs = (int)(seconds % 60);
        int milliseconds = (int)((seconds - (int)seconds) * 1000);
        
        return $"{minutes:D2}:{secs:D2}.{milliseconds:D3}";
    }

    public override void Unload(bool hotReload)
    {
        _playerTimers.Clear();
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}
