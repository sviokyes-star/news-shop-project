using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Admin;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TimerPlugin;

public class TimerPlugin : BasePlugin
{
    public override string ModuleName => "Map Timer";
    public override string ModuleVersion => "1.1.1";
    public override string ModuleAuthor => "poehali.dev";
    public override string ModuleDescription => "Таймер прохождения карты для CS2";

    private readonly Dictionary<int, PlayerTimer> _playerTimers = new();
    private readonly Dictionary<string, float> _mapRecords = new();
    private readonly Dictionary<string, MapZones> _mapZones = new();
    private readonly Dictionary<ulong, Dictionary<string, float>> _playerRecords = new();
    private readonly Dictionary<int, bool> _inStartZone = new();
    private readonly Dictionary<int, bool> _inEndZone = new();
    private readonly Dictionary<int, float> _lastFinishTime = new();
    
    private string ZonesFilePath => Path.Combine(ModuleDirectory, "zones.json");
    private string RecordsFilePath => Path.Combine(ModuleDirectory, "records.json");
    private string PlayerRecordsFilePath => Path.Combine(ModuleDirectory, "player_records.json");

    private class MapZones
    {
        public Vector? StartMin { get; set; }
        public Vector? StartMax { get; set; }
        public Vector? EndMin { get; set; }
        public Vector? EndMax { get; set; }
    }
    
    private class SerializableVector
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
    
    private class SerializableMapZones
    {
        public SerializableVector? StartMin { get; set; }
        public SerializableVector? StartMax { get; set; }
        public SerializableVector? EndMin { get; set; }
        public SerializableVector? EndMax { get; set; }
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
        
        LoadZones();
        LoadRecords();
        LoadPlayerRecords();
        
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
        
        SaveZones();

        player.PrintToChat($" {ChatColors.Green}[TIMER]{ChatColors.Default} Зона старта установлена и сохранена!");
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
        
        SaveZones();

        player.PrintToChat($" {ChatColors.Green}[TIMER]{ChatColors.Default} Зона финиша установлена и сохранена!");
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
                    bool isInStart = IsInZone(position, zones.StartMin, zones.StartMax);
                    bool wasInStart = _inStartZone.ContainsKey(userId) && _inStartZone[userId];
                    
                    if (isInStart && !wasInStart)
                    {
                        player.PrintToCenter("🟩 ЗОНА СТАРТА");
                        StartTimer(player);
                    }
                    
                    _inStartZone[userId] = isInStart;
                }
                
                if (zones.EndMin != null && zones.EndMax != null)
                {
                    bool isInEnd = IsInZone(position, zones.EndMin, zones.EndMax);
                    bool wasInEnd = _inEndZone.ContainsKey(userId) && _inEndZone[userId];
                    
                    if (isInEnd && !wasInEnd)
                    {
                        player.PrintToCenter("🟥 ЗОНА ФИНИША");
                        
                        if (_playerTimers.ContainsKey(userId) && _playerTimers[userId].IsRunning)
                        {
                            float finalTime = GetCurrentTime() - _playerTimers[userId].StartTime;
                            _lastFinishTime[userId] = finalTime;
                            StopTimer(player);
                        }
                    }
                    
                    _inEndZone[userId] = isInEnd;
                }
            }
            
            // Отображаем HUD с информацией
            bool inStart = _inStartZone.ContainsKey(userId) && _inStartZone[userId];
            bool inEnd = _inEndZone.ContainsKey(userId) && _inEndZone[userId];
            string hudText = BuildHudText(userId, mapName, inStart, inEnd);
            player.PrintToCenterHtml(hudText);
        }
    }

    private bool IsInZone(Vector position, Vector min, Vector max)
    {
        return position.X >= min.X && position.X <= max.X &&
               position.Y >= min.Y && position.Y <= max.Y &&
               position.Z >= min.Z && position.Z <= max.Z;
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

        string mapName = Server.MapName;

        bool isNewRecord = finalTime < timer.BestTime;
        if (isNewRecord)
        {
            timer.BestTime = finalTime;
            
            ulong steamId = player.SteamID;
            if (!_playerRecords.ContainsKey(steamId))
                _playerRecords[steamId] = new Dictionary<string, float>();
            
            _playerRecords[steamId][mapName] = finalTime;
            SavePlayerRecords();
            
            player.PrintToChat($" {ChatColors.Green}[TIMER]{ChatColors.Default} Новый личный рекорд: {ChatColors.Gold}{FormatTime(finalTime)}!");
        }
        else
        {
            player.PrintToChat($" {ChatColors.Green}[TIMER]{ChatColors.Default} Время прохождения: {ChatColors.Yellow}{FormatTime(finalTime)}");
        }

        if (!_mapRecords.ContainsKey(mapName) || finalTime < _mapRecords[mapName])
        {
            _mapRecords[mapName] = finalTime;
            SaveRecords();
            Server.PrintToChatAll($" {ChatColors.Green}[TIMER]{ChatColors.Default} {player.PlayerName} установил новый рекорд карты: {ChatColors.Purple}{FormatTime(finalTime)}!");
        }
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        int userId = (int)player.UserId!;
        ulong steamId = player.SteamID;
        string mapName = Server.MapName;

        if (!_playerTimers.ContainsKey(userId))
            _playerTimers[userId] = new PlayerTimer();

        if (_playerRecords.ContainsKey(steamId) && _playerRecords[steamId].ContainsKey(mapName))
        {
            _playerTimers[userId].BestTime = _playerRecords[steamId][mapName];
        }

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

    private string BuildHudText(int userId, string mapName, bool inStartZone, bool inEndZone)
    {
        var hudParts = new List<string>();

        // Личный рекорд
        string personalRecord = "---";
        if (_playerTimers.ContainsKey(userId) && _playerTimers[userId].BestTime != float.MaxValue)
        {
            personalRecord = FormatTime(_playerTimers[userId].BestTime);
        }

        // Рекорд карты
        string mapRecord = "---";
        if (_mapRecords.ContainsKey(mapName))
        {
            mapRecord = FormatTime(_mapRecords[mapName]);
        }

        // Если в зоне старта
        if (inStartZone)
        {
            float currentTime = 0f;
            if (_playerTimers.ContainsKey(userId) && _playerTimers[userId].IsRunning)
            {
                currentTime = GetCurrentTime() - _playerTimers[userId].StartTime;
            }
            
            hudParts.Add($"<font class='fontSize-l' color='#00ff00'>⏱ {FormatTime(currentTime)}</font>");
            hudParts.Add($"<font class='fontSize-m' color='#ffd700'>★ Личный: {personalRecord}</font>");
            hudParts.Add($"<font class='fontSize-m' color='#ff00ff'>🏆 Рекорд карты: {mapRecord}</font>");
            return string.Join("<br>", hudParts);
        }

        // Если в зоне финиша
        if (inEndZone && _lastFinishTime.ContainsKey(userId))
        {
            hudParts.Add($"<font class='fontSize-l' color='#00ff00'>Вы прошли карту за {FormatTime(_lastFinishTime[userId])}</font>");
            hudParts.Add($"<font class='fontSize-m' color='#ffd700'>★ Личный: {personalRecord}</font>");
            hudParts.Add($"<font class='fontSize-m' color='#ff00ff'>🏆 Рекорд карты: {mapRecord}</font>");
            return string.Join("<br>", hudParts);
        }

        // Обычный HUD (во время прохождения)
        if (_playerTimers.ContainsKey(userId) && _playerTimers[userId].IsRunning)
        {
            float currentTime = GetCurrentTime() - _playerTimers[userId].StartTime;
            hudParts.Add($"<font class='fontSize-l' color='#00ff00'>⏱ {FormatTime(currentTime)}</font>");
        }

        hudParts.Add($"<font class='fontSize-m' color='#ffd700'>★ Личный: {personalRecord}</font>");
        hudParts.Add($"<font class='fontSize-m' color='#ff00ff'>🏆 Рекорд карты: {mapRecord}</font>");

        return string.Join("<br>", hudParts);
    }

    private void LoadZones()
    {
        try
        {
            if (!File.Exists(ZonesFilePath))
                return;

            string json = File.ReadAllText(ZonesFilePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, SerializableMapZones>>(json);

            if (data == null)
                return;

            foreach (var kvp in data)
            {
                var zone = new MapZones();
                
                if (kvp.Value.StartMin != null)
                    zone.StartMin = new Vector(kvp.Value.StartMin.X, kvp.Value.StartMin.Y, kvp.Value.StartMin.Z);
                
                if (kvp.Value.StartMax != null)
                    zone.StartMax = new Vector(kvp.Value.StartMax.X, kvp.Value.StartMax.Y, kvp.Value.StartMax.Z);
                
                if (kvp.Value.EndMin != null)
                    zone.EndMin = new Vector(kvp.Value.EndMin.X, kvp.Value.EndMin.Y, kvp.Value.EndMin.Z);
                
                if (kvp.Value.EndMax != null)
                    zone.EndMax = new Vector(kvp.Value.EndMax.X, kvp.Value.EndMax.Y, kvp.Value.EndMax.Z);

                _mapZones[kvp.Key] = zone;
            }

            Console.WriteLine($"[{ModuleName}] Загружено зон: {_mapZones.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка загрузки зон: {ex.Message}");
        }
    }

    private void SaveZones()
    {
        try
        {
            var data = new Dictionary<string, SerializableMapZones>();

            foreach (var kvp in _mapZones)
            {
                var zone = new SerializableMapZones();
                
                if (kvp.Value.StartMin != null)
                    zone.StartMin = new SerializableVector { X = kvp.Value.StartMin.X, Y = kvp.Value.StartMin.Y, Z = kvp.Value.StartMin.Z };
                
                if (kvp.Value.StartMax != null)
                    zone.StartMax = new SerializableVector { X = kvp.Value.StartMax.X, Y = kvp.Value.StartMax.Y, Z = kvp.Value.StartMax.Z };
                
                if (kvp.Value.EndMin != null)
                    zone.EndMin = new SerializableVector { X = kvp.Value.EndMin.X, Y = kvp.Value.EndMin.Y, Z = kvp.Value.EndMin.Z };
                
                if (kvp.Value.EndMax != null)
                    zone.EndMax = new SerializableVector { X = kvp.Value.EndMax.X, Y = kvp.Value.EndMax.Y, Z = kvp.Value.EndMax.Z };

                data[kvp.Key] = zone;
            }

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ZonesFilePath, json);

            Console.WriteLine($"[{ModuleName}] Зоны сохранены: {data.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка сохранения зон: {ex.Message}");
        }
    }

    private void LoadRecords()
    {
        try
        {
            if (!File.Exists(RecordsFilePath))
                return;

            string json = File.ReadAllText(RecordsFilePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, float>>(json);

            if (data == null)
                return;

            foreach (var kvp in data)
            {
                _mapRecords[kvp.Key] = kvp.Value;
            }

            Console.WriteLine($"[{ModuleName}] Загружено рекордов: {_mapRecords.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка загрузки рекордов: {ex.Message}");
        }
    }

    private void SaveRecords()
    {
        try
        {
            string json = JsonSerializer.Serialize(_mapRecords, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(RecordsFilePath, json);

            Console.WriteLine($"[{ModuleName}] Рекорды сохранены: {_mapRecords.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка сохранения рекордов: {ex.Message}");
        }
    }

    private void LoadPlayerRecords()
    {
        try
        {
            if (!File.Exists(PlayerRecordsFilePath))
                return;

            string json = File.ReadAllText(PlayerRecordsFilePath);
            var data = JsonSerializer.Deserialize<Dictionary<ulong, Dictionary<string, float>>>(json);

            if (data == null)
                return;

            foreach (var kvp in data)
            {
                _playerRecords[kvp.Key] = kvp.Value;
            }

            Console.WriteLine($"[{ModuleName}] Загружено личных рекордов: {_playerRecords.Count} игроков");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка загрузки личных рекордов: {ex.Message}");
        }
    }

    private void SavePlayerRecords()
    {
        try
        {
            string json = JsonSerializer.Serialize(_playerRecords, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(PlayerRecordsFilePath, json);

            Console.WriteLine($"[{ModuleName}] Личные рекорды сохранены: {_playerRecords.Count} игроков");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка сохранения личных рекордов: {ex.Message}");
        }
    }

    public override void Unload(bool hotReload)
    {
        _playerTimers.Clear();
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}