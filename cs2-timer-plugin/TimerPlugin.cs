using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Admin;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Drawing;

namespace TimerPlugin;

public class TimerPlugin : BasePlugin
{
    public override string ModuleName => "Okyes - Map Timer";
    public override string ModuleVersion => "1.5.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Таймер прохождения карты для CS2";

    // Ярко-оранжевый цвет (≈ #FF4500) для префикса "Okyes |".
    // В чате CS2 задаётся управляющим байтом \u0010 (Orange).
    private const char Orange = '\u0010';

    private readonly Dictionary<int, PlayerTimer> _playerTimers = new();
    private readonly Dictionary<string, float> _mapRecords = new();
    private readonly Dictionary<string, string> _mapRecordHolders = new();
    private readonly Dictionary<string, MapZones> _mapZones = new();
    private readonly Dictionary<ulong, Dictionary<string, float>> _playerRecords = new();
    private readonly Dictionary<ulong, string> _playerNames = new();
    private readonly Dictionary<int, bool> _inStartZone = new();
    private readonly Dictionary<int, bool> _inEndZone = new();
    private readonly Dictionary<int, float> _lastFinishTime = new();

    // Временные первые углы для задания зоны по двум точкам (ключ — userId).
    private readonly Dictionary<int, Vector> _startCorner1 = new();
    private readonly Dictionary<int, Vector> _endCorner1 = new();
    private float _lastDebugLog = 0f;
    
    private CounterStrikeSharp.API.Modules.Timers.Timer? _beamTimer;
    private readonly List<CBeam> _zoneBeams = new();

    // Отступ сверху для сдвига текста таймера ниже центра экрана.
    // Используем margin-top у обёртки вместо пустых <br>, чтобы текст
    // не "убегал" за пределы видимой области окна.
    private const string HudTopOffset = "<div style=\"margin-top: 400px;\">";
    private const string HudTopOffsetClose = "</div>";
    
    private string ZonesFilePath => Path.Combine(ModuleDirectory, "zones.json");
    private string RecordsFilePath => Path.Combine(ModuleDirectory, "records.json");
    private string RecordHoldersFilePath => Path.Combine(ModuleDirectory, "record_holders.json");
    private string PlayerRecordsFilePath => Path.Combine(ModuleDirectory, "player_records.json");
    private string PlayerNamesFilePath => Path.Combine(ModuleDirectory, "player_names.json");

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
        LoadRecordHolders();
        LoadPlayerRecords();
        LoadPlayerNames();
        
        _beamTimer = AddTimer(0.1f, DrawZoneBeams, TimerFlags.REPEAT);
        
        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
    }

    public override void Unload(bool hotReload)
    {
        _beamTimer?.Kill();
        ClearBeams();
        _playerTimers.Clear();
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
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
            player.PrintToChat($" {Orange}Okyes |{ChatColors.Default} Таймер не запущен");
            return;
        }

        var timer = _playerTimers[userId];
        
        if (timer.IsRunning)
        {
            float currentTime = GetCurrentTime() - timer.StartTime;
            player.PrintToChat($" {Orange}Okyes |{ChatColors.Default} Текущее время: {ChatColors.Yellow}{FormatTime(currentTime)}");
        }
        
        if (timer.BestTime != float.MaxValue)
        {
            player.PrintToChat($" {Orange}Okyes |{ChatColors.Default} Ваш лучший результат: {ChatColors.Gold}{FormatTime(timer.BestTime)}");
        }

        string mapName = Server.MapName;
        if (_mapRecords.ContainsKey(mapName))
        {
            string recordHolder = _mapRecordHolders.ContainsKey(mapName) ? _mapRecordHolders[mapName] : "???";
            player.PrintToChat($" {Orange}Okyes |{ChatColors.Default} Рекорд карты ({recordHolder}): {ChatColors.Purple}{FormatTime(_mapRecords[mapName])}");
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

        player.PrintToChat($" {Orange}Okyes |{ChatColors.Default} Зона старта установлена и сохранена!");
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

        player.PrintToChat($" {Orange}Okyes |{ChatColors.Default} Зона финиша установлена и сохранена!");
        player.PrintToChat($" {ChatColors.Yellow}Координаты: {position.X:F0}, {position.Y:F0}, {position.Z:F0}");
    }

    [ConsoleCommand("css_setstart1", "Отметить 1-й угол зоны старта")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnSetStart1Command(CCSPlayerController? player, CommandInfo command)
    {
        var pos = GetPlayerPos(player);
        if (player == null || pos == null) return;

        _startCorner1[(int)player.UserId!] = new Vector(pos.X, pos.Y, pos.Z);
        player.PrintToChat($" {Orange}Okyes |{ChatColors.Default} 1-й угол СТАРТА отмечен: {ChatColors.Yellow}{pos.X:F0}, {pos.Y:F0}, {pos.Z:F0}");
        player.PrintToChat($" {ChatColors.Green}Теперь встань в противоположный угол и введи css_setstart2");
    }

    [ConsoleCommand("css_setstart2", "Отметить 2-й угол зоны старта и создать зону")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnSetStart2Command(CCSPlayerController? player, CommandInfo command)
    {
        var pos = GetPlayerPos(player);
        if (player == null || pos == null) return;

        int userId = (int)player.UserId!;
        if (!_startCorner1.ContainsKey(userId))
        {
            player.PrintToChat($" {Orange}Okyes |{ChatColors.Default} Сначала отметь 1-й угол командой css_setstart1");
            return;
        }

        var c1 = _startCorner1[userId];
        var c2 = new Vector(pos.X, pos.Y, pos.Z);

        string mapName = Server.MapName;
        if (!_mapZones.ContainsKey(mapName))
            _mapZones[mapName] = new MapZones();

        _mapZones[mapName].StartMin = MinVector(c1, c2);
        _mapZones[mapName].StartMax = MaxVector(c1, c2);
        _startCorner1.Remove(userId);
        SaveZones();

        var size = SizeOf(_mapZones[mapName].StartMin!, _mapZones[mapName].StartMax!);
        player.PrintToChat($" {Orange}Okyes |{ChatColors.Default} Зона СТАРТА создана! Размер: {ChatColors.Yellow}{size.X:F0} x {size.Y:F0} x {size.Z:F0}");
    }

    [ConsoleCommand("css_setend1", "Отметить 1-й угол зоны финиша")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnSetEnd1Command(CCSPlayerController? player, CommandInfo command)
    {
        var pos = GetPlayerPos(player);
        if (player == null || pos == null) return;

        _endCorner1[(int)player.UserId!] = new Vector(pos.X, pos.Y, pos.Z);
        player.PrintToChat($" {Orange}Okyes |{ChatColors.Default} 1-й угол ФИНИША отмечен: {ChatColors.Yellow}{pos.X:F0}, {pos.Y:F0}, {pos.Z:F0}");
        player.PrintToChat($" {ChatColors.Green}Теперь встань в противоположный угол и введи css_setend2");
    }

    [ConsoleCommand("css_setend2", "Отметить 2-й угол зоны финиша и создать зону")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnSetEnd2Command(CCSPlayerController? player, CommandInfo command)
    {
        var pos = GetPlayerPos(player);
        if (player == null || pos == null) return;

        int userId = (int)player.UserId!;
        if (!_endCorner1.ContainsKey(userId))
        {
            player.PrintToChat($" {Orange}Okyes |{ChatColors.Default} Сначала отметь 1-й угол командой css_setend1");
            return;
        }

        var c1 = _endCorner1[userId];
        var c2 = new Vector(pos.X, pos.Y, pos.Z);

        string mapName = Server.MapName;
        if (!_mapZones.ContainsKey(mapName))
            _mapZones[mapName] = new MapZones();

        _mapZones[mapName].EndMin = MinVector(c1, c2);
        _mapZones[mapName].EndMax = MaxVector(c1, c2);
        _endCorner1.Remove(userId);
        SaveZones();

        var size = SizeOf(_mapZones[mapName].EndMin!, _mapZones[mapName].EndMax!);
        player.PrintToChat($" {Orange}Okyes |{ChatColors.Default} Зона ФИНИША создана! Размер: {ChatColors.Yellow}{size.X:F0} x {size.Y:F0} x {size.Z:F0}");
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
            player.PrintToChat($" {Orange}Okyes |{ChatColors.Default} Зоны не настроены");
            return;
        }

        var zones = _mapZones[mapName];
        
        if (zones.StartMin != null && zones.StartMax != null)
        {
            player.PrintToChat($" {Orange}Okyes |{ChatColors.Default} Зона старта:");
            player.PrintToChat($" Min: {zones.StartMin.X:F0}, {zones.StartMin.Y:F0}, {zones.StartMin.Z:F0}");
            player.PrintToChat($" Max: {zones.StartMax.X:F0}, {zones.StartMax.Y:F0}, {zones.StartMax.Z:F0}");
        }
        else
        {
            player.PrintToChat($" {Orange}Okyes | Зона старта не установлена");
        }

        if (zones.EndMin != null && zones.EndMax != null)
        {
            player.PrintToChat($" {Orange}Okyes |{ChatColors.Default} Зона финиша:");
            player.PrintToChat($" Min: {zones.EndMin.X:F0}, {zones.EndMin.Y:F0}, {zones.EndMin.Z:F0}");
            player.PrintToChat($" Max: {zones.EndMax.X:F0}, {zones.EndMax.Y:F0}, {zones.EndMax.Z:F0}");
        }
        else
        {
            player.PrintToChat($" {Orange}Okyes | Зона финиша не установлена");
        }
    }

    [ConsoleCommand("css_setrecordholder", "Установить рекордсмена карты")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 1, usage: "<имя игрока>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnSetRecordHolderCommand(CCSPlayerController? player, CommandInfo command)
    {
        string mapName = Server.MapName;
        
        if (!_mapRecords.ContainsKey(mapName))
        {
            if (player != null)
                player.PrintToChat($" {Orange}Okyes |{ChatColors.Default} На этой карте нет рекорда");
            return;
        }

        string holderName = command.GetArg(1);
        _mapRecordHolders[mapName] = holderName;
        SaveRecordHolders();

        if (player != null)
            player.PrintToChat($" {Orange}Okyes |{ChatColors.Default} Рекордсмен установлен: {holderName}");
        
        Console.WriteLine($"[TIMER] Record holder for {mapName} set to: {holderName}");
    }

    [ConsoleCommand("css_resetrecords", "Сбросить рекорды на текущей карте")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnResetRecordsCommand(CCSPlayerController? player, CommandInfo command)
    {
        string mapName = Server.MapName;

        bool had = _mapRecords.Remove(mapName);
        _mapRecordHolders.Remove(mapName);

        foreach (var kv in _playerRecords)
            kv.Value.Remove(mapName);

        SaveRecords();
        SaveRecordHolders();
        SavePlayerRecords();

        if (player != null)
        {
            if (had)
                player.PrintToChat($" {Orange}Okyes |{ChatColors.Green} Рекорды на карте {mapName} сброшены");
            else
                player.PrintToChat($" {Orange}Okyes |{ChatColors.Default} На карте {mapName} рекордов не было");
        }

        Console.WriteLine($"[TIMER] Records reset for map: {mapName}");
    }

    [ConsoleCommand("css_top", "Показать топ-10 результатов")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnTopCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        string mapName = Server.MapName;

        var sortedRecords = _playerRecords
            .Where(p => p.Value.ContainsKey(mapName))
            .Select(p => new { SteamId = p.Key, Time = p.Value[mapName] })
            .OrderBy(x => x.Time)
            .Take(10)
            .ToList();

        if (sortedRecords.Count == 0)
        {
            player.PrintToChat($" {Orange}Okyes |{ChatColors.Default} Пока нет результатов на этой карте");
            return;
        }

        player.PrintToChat($" {Orange}Okyes |{ChatColors.Default} Топ-10 на карте {ChatColors.Gold}{mapName}{ChatColors.Default}:");
        for (int i = 0; i < sortedRecords.Count; i++)
        {
            ulong sid = sortedRecords[i].SteamId;
            string playerName = _playerNames.ContainsKey(sid)
                ? _playerNames[sid]
                : (Utilities.GetPlayers().FirstOrDefault(pl => pl.IsValid && pl.SteamID == sid)?.PlayerName ?? "Unknown");
            player.PrintToChat($" {i + 1}. {playerName}: {ChatColors.Yellow}{FormatTime(sortedRecords[i].Time)}");
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
                        player.PrintToCenterHtml($"{HudTopOffset}🟩 ЗОНА СТАРТА{HudTopOffsetClose}");
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
                        player.PrintToCenterHtml($"{HudTopOffset}🟥 ЗОНА ФИНИША{HudTopOffsetClose}");
                        
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
            player.PrintToCenterHtml($"{HudTopOffset}{hudText}{HudTopOffsetClose}");
        }
    }

    private bool IsInZone(Vector position, Vector min, Vector max)
    {
        return position.X >= min.X && position.X <= max.X &&
               position.Y >= min.Y && position.Y <= max.Y &&
               position.Z >= min.Z && position.Z <= max.Z;
    }

    // Позиция игрока (или null, если игрок/пешка невалидны).
    private Vector? GetPlayerPos(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.PlayerPawn.Value == null)
            return null;
        return player.PlayerPawn.Value.AbsOrigin;
    }

    // Минимальный угол коробки из двух произвольных точек.
    private Vector MinVector(Vector a, Vector b)
        => new Vector(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));

    // Максимальный угол коробки. Прибавляем 72 по Z к верхней точке,
    // чтобы зона гарантированно накрывала игрока по высоте (рост модели).
    private Vector MaxVector(Vector a, Vector b)
        => new Vector(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z) + 72f);

    // Габариты зоны для вывода в чат.
    private Vector SizeOf(Vector min, Vector max)
        => new Vector(max.X - min.X, max.Y - min.Y, max.Z - min.Z);



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
            _playerNames[steamId] = player.PlayerName;
            SavePlayerRecords();
            SavePlayerNames();
        }

        bool isMapRecord = !_mapRecords.ContainsKey(mapName) || finalTime < _mapRecords[mapName];

        // Сообщаем всем игрокам о прохождении карты.
        if (isMapRecord)
        {
            _mapRecords[mapName] = finalTime;
            _mapRecordHolders[mapName] = player.PlayerName;
            SaveRecords();
            SaveRecordHolders();
            Console.WriteLine($"[TIMER DEBUG] New map record set for {mapName}: {FormatTime(finalTime)} by {player.PlayerName}");
            Server.PrintToChatAll($" {Orange}Okyes |{ChatColors.Default} {ChatColors.Gold}{player.PlayerName}{ChatColors.Default} прошёл карту за {ChatColors.Purple}{FormatTime(finalTime)}{ChatColors.Default} — {ChatColors.Red}новый рекорд карты!");
        }
        else if (isNewRecord)
        {
            Server.PrintToChatAll($" {Orange}Okyes |{ChatColors.Default} {ChatColors.Gold}{player.PlayerName}{ChatColors.Default} прошёл карту за {ChatColors.Yellow}{FormatTime(finalTime)}{ChatColors.Default} — {ChatColors.Green}личный рекорд!");
        }
        else
        {
            Server.PrintToChatAll($" {Orange}Okyes |{ChatColors.Default} {ChatColors.Gold}{player.PlayerName}{ChatColors.Default} прошёл карту за {ChatColors.Yellow}{FormatTime(finalTime)}");
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
        _startCorner1.Remove(userId);
        _endCorner1.Remove(userId);

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
        string mapRecordHolder = "";
        if (_mapRecords.ContainsKey(mapName))
        {
            mapRecord = FormatTime(_mapRecords[mapName]);
            if (_mapRecordHolders.ContainsKey(mapName))
            {
                mapRecordHolder = $" ({_mapRecordHolders[mapName]})";
            }
        }

        // Если в зоне старта
        if (inStartZone)
        {
            float currentTime = 0f;
            if (_playerTimers.ContainsKey(userId) && _playerTimers[userId].IsRunning)
            {
                currentTime = GetCurrentTime() - _playerTimers[userId].StartTime;
            }
            
            hudParts.Add($"{FormatTime(currentTime)}");
            hudParts.Add($"Личный рекорд: {personalRecord}");
            hudParts.Add($"Рекорд карты{mapRecordHolder}: {mapRecord}");
            return string.Join("<br>", hudParts);
        }

        // Если в зоне финиша
        if (inEndZone && _lastFinishTime.ContainsKey(userId))
        {
            hudParts.Add($"Время: {FormatTime(_lastFinishTime[userId])}");
            hudParts.Add($"Личный рекорд: {personalRecord}");
            hudParts.Add($"Рекорд карты{mapRecordHolder}: {mapRecord}");
            return string.Join("<br>", hudParts);
        }

        // Обычный HUD (во время прохождения)
        if (_playerTimers.ContainsKey(userId) && _playerTimers[userId].IsRunning)
        {
            float currentTime = GetCurrentTime() - _playerTimers[userId].StartTime;
            hudParts.Add($"{FormatTime(currentTime)}");
        }

        hudParts.Add($"Личный рекорд: {personalRecord}");
        hudParts.Add($"Рекорд карты{mapRecordHolder}: {mapRecord}");

        string result = string.Join("<br>", hudParts);
        
        // Логируем раз в секунду
        float now = GetCurrentTime();
        if (now - _lastDebugLog > 1.0f)
        {
            _lastDebugLog = now;
            Console.WriteLine($"[TIMER DEBUG] Map: {mapName}, Record: {mapRecord}, HUD Lines: {hudParts.Count}, InStart: {inStartZone}, InEnd: {inEndZone}");
            Console.WriteLine($"[TIMER DEBUG] HUD Content: {result}");
        }
        
        return result;
    }

    private void DrawZoneBeams()
    {
        string mapName = Server.MapName;
        
        if (!_mapZones.ContainsKey(mapName))
            return;

        ClearBeams();

        var zones = _mapZones[mapName];
        
        if (zones.StartMin != null && zones.StartMax != null)
        {
            CreateZoneBeams(zones.StartMin, zones.StartMax, Color.FromArgb(255, 0, 255, 0));
        }
        
        if (zones.EndMin != null && zones.EndMax != null)
        {
            CreateZoneBeams(zones.EndMin, zones.EndMax, Color.FromArgb(255, 255, 0, 0));
        }
    }

    private void ClearBeams()
    {
        foreach (var beam in _zoneBeams)
        {
            if (beam?.IsValid == true)
            {
                beam.Remove();
            }
        }
        _zoneBeams.Clear();
    }

    private void CreateZoneBeams(Vector min, Vector max, Color color)
    {
        var corners = new Vector[]
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
        
        int[][] edges = new int[][]
        {
            new int[] {0, 1}, new int[] {1, 2}, new int[] {2, 3}, new int[] {3, 0},
            new int[] {4, 5}, new int[] {5, 6}, new int[] {6, 7}, new int[] {7, 4},
            new int[] {0, 4}, new int[] {1, 5}, new int[] {2, 6}, new int[] {3, 7}
        };

        foreach (var edge in edges)
        {
            var beam = Utilities.CreateEntityByName<CBeam>("beam");
            if (beam == null)
                continue;

            beam.Teleport(corners[edge[0]], new QAngle(0, 0, 0), new Vector(0, 0, 0));
            beam.EndPos.X = corners[edge[1]].X;
            beam.EndPos.Y = corners[edge[1]].Y;
            beam.EndPos.Z = corners[edge[1]].Z;
            beam.Width = 2.0f;
            beam.Render = color;
            
            beam.DispatchSpawn();
            _zoneBeams.Add(beam);
        }
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

    private void LoadRecordHolders()
    {
        try
        {
            if (!File.Exists(RecordHoldersFilePath))
                return;

            string json = File.ReadAllText(RecordHoldersFilePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (data == null)
                return;

            foreach (var kvp in data)
            {
                _mapRecordHolders[kvp.Key] = kvp.Value;
            }

            Console.WriteLine($"[{ModuleName}] Загружено рекордсменов: {_mapRecordHolders.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка загрузки рекордсменов: {ex.Message}");
        }
    }

    private void SaveRecordHolders()
    {
        try
        {
            string json = JsonSerializer.Serialize(_mapRecordHolders, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(RecordHoldersFilePath, json);

            Console.WriteLine($"[{ModuleName}] Рекордсмены сохранены: {_mapRecordHolders.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка сохранения рекордсменов: {ex.Message}");
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

    private void LoadPlayerNames()
    {
        try
        {
            if (!File.Exists(PlayerNamesFilePath))
                return;

            string json = File.ReadAllText(PlayerNamesFilePath);
            var data = JsonSerializer.Deserialize<Dictionary<ulong, string>>(json);

            if (data == null)
                return;

            foreach (var kvp in data)
                _playerNames[kvp.Key] = kvp.Value;

            Console.WriteLine($"[{ModuleName}] Загружено имён игроков: {_playerNames.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка загрузки имён игроков: {ex.Message}");
        }
    }

    private void SavePlayerNames()
    {
        try
        {
            string json = JsonSerializer.Serialize(_playerNames, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(PlayerNamesFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка сохранения имён игроков: {ex.Message}");
        }
    }

}