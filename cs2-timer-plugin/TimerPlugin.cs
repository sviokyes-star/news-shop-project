using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Admin;

namespace TimerPlugin;

public class TimerPlugin : BasePlugin
{
    public override string ModuleName => "Map Timer";
    public override string ModuleVersion => "1.0.3";
    public override string ModuleAuthor => "poehali.dev";
    public override string ModuleDescription => "Таймер прохождения карты для CS2";

    private readonly Dictionary<int, PlayerTimer> _playerTimers = new();
    private readonly Dictionary<string, float> _mapRecords = new();
    private readonly Dictionary<string, MapZones> _mapZones = new();

    private class MapZones
    {
        public Vector? StartMin { get; set; }
        public Vector? StartMax { get; set; }
        public Vector? EndMin { get; set; }
        public Vector? EndMax { get; set; }
    }

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

    [ConsoleCommand("css_setstart", "Установить зону старта")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnSetStartCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.PlayerPawn.Value == null)
            return;

        var position = player.PlayerPawn.Value.AbsOrigin;
        if (position == null)
            return;

        string mapName = Server.MapName;
        if (!_mapZones.ContainsKey(mapName))
            _mapZones[mapName] = new MapZones();

        var min = new Vector(position.X - 50, position.Y - 50, position.Z - 10);
        var max = new Vector(position.X + 50, position.Y + 50, position.Z + 100);

        _mapZones[mapName].StartMin = min;
        _mapZones[mapName].StartMax = max;

        player.PrintToChat($" {ChatColors.Green}[TIMER]{ChatColors.Default} Зона старта установлена!");
        player.PrintToChat($" {ChatColors.Yellow}Координаты: {position.X:F0}, {position.Y:F0}, {position.Z:F0}");
    }

    [ConsoleCommand("css_setend", "Установить зону финиша")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnSetEndCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.PlayerPawn.Value == null)
            return;

        var position = player.PlayerPawn.Value.AbsOrigin;
        if (position == null)
            return;

        string mapName = Server.MapName;
        if (!_mapZones.ContainsKey(mapName))
            _mapZones[mapName] = new MapZones();

        var min = new Vector(position.X - 50, position.Y - 50, position.Z - 10);
        var max = new Vector(position.X + 50, position.Y + 50, position.Z + 100);

        _mapZones[mapName].EndMin = min;
        _mapZones[mapName].EndMax = max;

        player.PrintToChat($" {ChatColors.Green}[TIMER]{ChatColors.Default} Зона финиша установлена!");
        player.PrintToChat($" {ChatColors.Yellow}Координаты: {position.X:F0}, {position.Y:F0}, {position.Z:F0}");
    }

    [ConsoleCommand("css_showzones", "Показать зоны старта и финиша")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnShowZonesCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        string mapName = Server.MapName;
        if (!_mapZones.ContainsKey(mapName))
        {
            player.PrintToChat($" {ChatColors.Green}[TIMER]{ChatColors.Default} Зоны не настроены");
            return;
        }

        var zones = _mapZones[mapName];
        
        if (zones.StartMin != null && zones.StartMax != null)
        {
            player.PrintToChat($" {ChatColors.Green}[TIMER]{ChatColors.Default} Зона старта:");
            player.PrintToChat($" Min: {zones.StartMin.X:F0}, {zones.StartMin.Y:F0}, {zones.StartMin.Z:F0}");
            player.PrintToChat($" Max: {zones.StartMax.X:F0}, {zones.StartMax.Y:F0}, {zones.StartMax.Z:F0}");
        }
        else
        {
            player.PrintToChat($" {ChatColors.Red}[TIMER] Зона старта не установлена");
        }

        if (zones.EndMin != null && zones.EndMax != null)
        {
            player.PrintToChat($" {ChatColors.Green}[TIMER]{ChatColors.Default} Зона финиша:");
            player.PrintToChat($" Min: {zones.EndMin.X:F0}, {zones.EndMin.Y:F0}, {zones.EndMin.Z:F0}");
            player.PrintToChat($" Max: {zones.EndMax.X:F0}, {zones.EndMax.Y:F0}, {zones.EndMax.Z:F0}");
        }
        else
        {
            player.PrintToChat($" {ChatColors.Red}[TIMER] Зона финиша не установлена");
        }
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
            var position = player.PlayerPawn.Value.AbsOrigin;
            
            if (position == null)
                continue;

            string mapName = Server.MapName;
            
            if (_mapZones.ContainsKey(mapName))
            {
                var zones = _mapZones[mapName];
                
                if (zones.StartMin != null && zones.StartMax != null)
                {
                    DrawZoneBox(player, zones.StartMin, zones.StartMax, "00ff00");
                    
                    if (IsInZone(position, zones.StartMin, zones.StartMax))
                    {
                        if (!_playerTimers.ContainsKey(userId) || !_playerTimers[userId].IsRunning)
                        {
                            StartTimer(player);
                        }
                    }
                }
                
                if (zones.EndMin != null && zones.EndMax != null)
                {
                    DrawZoneBox(player, zones.EndMin, zones.EndMax, "ff0000");
                    
                    if (_playerTimers.ContainsKey(userId) && _playerTimers[userId].IsRunning)
                    {
                        if (IsInZone(position, zones.EndMin, zones.EndMax))
                        {
                            StopTimer(player);
                        }
                    }
                }
            }
            
            if (!_playerTimers.ContainsKey(userId))
                continue;

            var timer = _playerTimers[userId];
            if (!timer.IsRunning)
                continue;

            float currentTime = GetCurrentTime() - timer.StartTime;
            
            player.PrintToCenterHtml($"<font color='#00ff00'>Время: {FormatTime(currentTime)}</font>");
        }
    }

    private bool IsInZone(Vector position, Vector min, Vector max)
    {
        return position.X >= min.X && position.X <= max.X &&
               position.Y >= min.Y && position.Y <= max.Y &&
               position.Z >= min.Z && position.Z <= max.Z;
    }

    private void DrawZoneBox(CCSPlayerController player, Vector min, Vector max, string color)
    {
        var corners = new Vector[8]
        {
            new Vector(min.X, min.Y, min.Z),
            new Vector(max.X, min.Y, min.Z),
            new Vector(max.X, max.Y, min.Z),
            new Vector(min.X, max.Y, min.Z),
            new Vector(min.X, min.Y, max.Z),
            new Vector(max.X, min.Y, max.Z),
            new Vector(max.X, max.Y, max.Z),
            new Vector(min.X, max.Y, max.Z)
        };

        DrawLine(player, corners[0], corners[1], color);
        DrawLine(player, corners[1], corners[2], color);
        DrawLine(player, corners[2], corners[3], color);
        DrawLine(player, corners[3], corners[0], color);

        DrawLine(player, corners[4], corners[5], color);
        DrawLine(player, corners[5], corners[6], color);
        DrawLine(player, corners[6], corners[7], color);
        DrawLine(player, corners[7], corners[4], color);

        DrawLine(player, corners[0], corners[4], color);
        DrawLine(player, corners[1], corners[5], color);
        DrawLine(player, corners[2], corners[6], color);
        DrawLine(player, corners[3], corners[7], color);
    }

    private void DrawLine(CCSPlayerController player, Vector start, Vector end, string color)
    {
        player.PrintToCenterHtml($"<font class='fontSize-l' color='#{color}'>■</font>");
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