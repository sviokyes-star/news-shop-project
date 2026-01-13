using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpawnManagerPlugin;

public class SpawnManagerPlugin : BasePlugin
{
    public override string ModuleName => "Spawn Manager";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Управление точками спавна игроков на картах CS2";

    private readonly Dictionary<string, MapSpawns> _mapSpawns = new();
    private string SpawnsFilePath => Path.Combine(ModuleDirectory, "spawns.json");

    private class MapSpawns
    {
        public List<SpawnPoint> CTSpawns { get; set; } = new();
        public List<SpawnPoint> TSpawns { get; set; } = new();
    }

    private class SpawnPoint
    {
        public Vector Position { get; set; } = new();
        public QAngle Angle { get; set; } = new();
    }

    private class SerializableVector
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    private class SerializableAngle
    {
        public float Pitch { get; set; }
        public float Yaw { get; set; }
        public float Roll { get; set; }
    }

    private class SerializableSpawnPoint
    {
        public SerializableVector? Position { get; set; }
        public SerializableAngle? Angle { get; set; }
    }

    private class SerializableMapSpawns
    {
        public List<SerializableSpawnPoint> CTSpawns { get; set; } = new();
        public List<SerializableSpawnPoint> TSpawns { get; set; } = new();
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        LoadSpawns();
        
        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
    }

    [ConsoleCommand("css_setctspawn", "Установить спавн для CT команды")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnSetCTSpawnCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.PlayerPawn.Value == null)
            return;

        var position = player.PlayerPawn.Value.AbsOrigin;
        var angle = player.PlayerPawn.Value.AbsRotation;
        
        if (position == null || angle == null)
            return;

        string mapName = Server.MapName;
        if (!_mapSpawns.ContainsKey(mapName))
            _mapSpawns[mapName] = new MapSpawns();

        var spawn = new SpawnPoint
        {
            Position = new Vector(position.X, position.Y, position.Z),
            Angle = new QAngle(angle.X, angle.Y, angle.Z)
        };

        _mapSpawns[mapName].CTSpawns.Add(spawn);
        SaveSpawns();

        player.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} CT спавн #{_mapSpawns[mapName].CTSpawns.Count} добавлен!");
        player.PrintToChat($" {ChatColors.Yellow}Координаты: {position.X:F0}, {position.Y:F0}, {position.Z:F0}");
    }

    [ConsoleCommand("css_settspawn", "Установить спавн для T команды")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnSetTSpawnCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.PlayerPawn.Value == null)
            return;

        var position = player.PlayerPawn.Value.AbsOrigin;
        var angle = player.PlayerPawn.Value.AbsRotation;
        
        if (position == null || angle == null)
            return;

        string mapName = Server.MapName;
        if (!_mapSpawns.ContainsKey(mapName))
            _mapSpawns[mapName] = new MapSpawns();

        var spawn = new SpawnPoint
        {
            Position = new Vector(position.X, position.Y, position.Z),
            Angle = new QAngle(angle.X, angle.Y, angle.Z)
        };

        _mapSpawns[mapName].TSpawns.Add(spawn);
        SaveSpawns();

        player.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} T спавн #{_mapSpawns[mapName].TSpawns.Count} добавлен!");
        player.PrintToChat($" {ChatColors.Yellow}Координаты: {position.X:F0}, {position.Y:F0}, {position.Z:F0}");
    }

    [ConsoleCommand("css_clearctspawns", "Очистить все CT спавны")]
    [RequiresPermissions("@css/root")]
    public void OnClearCTSpawnsCommand(CCSPlayerController? player, CommandInfo command)
    {
        string mapName = Server.MapName;
        
        if (!_mapSpawns.ContainsKey(mapName))
        {
            if (player != null)
                player.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} Спавны не настроены");
            return;
        }

        int count = _mapSpawns[mapName].CTSpawns.Count;
        _mapSpawns[mapName].CTSpawns.Clear();
        SaveSpawns();

        if (player != null)
            player.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} Удалено {count} CT спавнов");
        
        Console.WriteLine($"[Spawn Manager] Cleared {count} CT spawns on {mapName}");
    }

    [ConsoleCommand("css_cleartspawns", "Очистить все T спавны")]
    [RequiresPermissions("@css/root")]
    public void OnClearTSpawnsCommand(CCSPlayerController? player, CommandInfo command)
    {
        string mapName = Server.MapName;
        
        if (!_mapSpawns.ContainsKey(mapName))
        {
            if (player != null)
                player.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} Спавны не настроены");
            return;
        }

        int count = _mapSpawns[mapName].TSpawns.Count;
        _mapSpawns[mapName].TSpawns.Clear();
        SaveSpawns();

        if (player != null)
            player.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} Удалено {count} T спавнов");
        
        Console.WriteLine($"[Spawn Manager] Cleared {count} T spawns on {mapName}");
    }

    [ConsoleCommand("css_listspawns", "Показать список спавнов")]
    [RequiresPermissions("@css/root")]
    public void OnListSpawnsCommand(CCSPlayerController? player, CommandInfo command)
    {
        string mapName = Server.MapName;
        
        if (!_mapSpawns.ContainsKey(mapName))
        {
            if (player != null)
                player.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} Спавны не настроены");
            return;
        }

        var spawns = _mapSpawns[mapName];
        
        if (player != null)
        {
            player.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} Спавны на карте {mapName}:");
            player.PrintToChat($" {ChatColors.Blue}CT спавнов: {ChatColors.Yellow}{spawns.CTSpawns.Count}");
            player.PrintToChat($" {ChatColors.Red}T спавнов: {ChatColors.Yellow}{spawns.TSpawns.Count}");
        }
        else
        {
            Console.WriteLine($"[Spawn Manager] Spawns on {mapName}:");
            Console.WriteLine($"  CT spawns: {spawns.CTSpawns.Count}");
            Console.WriteLine($"  T spawns: {spawns.TSpawns.Count}");
        }
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        
        if (player == null || !player.IsValid || player.PlayerPawn.Value == null)
            return HookResult.Continue;

        string mapName = Server.MapName;
        
        if (!_mapSpawns.ContainsKey(mapName))
            return HookResult.Continue;

        var spawns = _mapSpawns[mapName];
        List<SpawnPoint>? teamSpawns = null;

        if (player.Team == CsTeam.CounterTerrorist && spawns.CTSpawns.Count > 0)
        {
            teamSpawns = spawns.CTSpawns;
        }
        else if (player.Team == CsTeam.Terrorist && spawns.TSpawns.Count > 0)
        {
            teamSpawns = spawns.TSpawns;
        }

        if (teamSpawns == null || teamSpawns.Count == 0)
            return HookResult.Continue;

        var spawn = teamSpawns[Random.Shared.Next(teamSpawns.Count)];
        
        Server.NextFrame(() =>
        {
            if (player.PlayerPawn.Value != null && player.PlayerPawn.Value.IsValid)
            {
                player.PlayerPawn.Value.Teleport(spawn.Position, spawn.Angle, new Vector(0, 0, 0));
            }
        });

        return HookResult.Continue;
    }

    private void LoadSpawns()
    {
        try
        {
            if (!File.Exists(SpawnsFilePath))
                return;

            string json = File.ReadAllText(SpawnsFilePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, SerializableMapSpawns>>(json);

            if (data == null)
                return;

            foreach (var kvp in data)
            {
                var mapSpawns = new MapSpawns();
                
                foreach (var spawn in kvp.Value.CTSpawns)
                {
                    if (spawn.Position != null && spawn.Angle != null)
                    {
                        mapSpawns.CTSpawns.Add(new SpawnPoint
                        {
                            Position = new Vector(spawn.Position.X, spawn.Position.Y, spawn.Position.Z),
                            Angle = new QAngle(spawn.Angle.Pitch, spawn.Angle.Yaw, spawn.Angle.Roll)
                        });
                    }
                }
                
                foreach (var spawn in kvp.Value.TSpawns)
                {
                    if (spawn.Position != null && spawn.Angle != null)
                    {
                        mapSpawns.TSpawns.Add(new SpawnPoint
                        {
                            Position = new Vector(spawn.Position.X, spawn.Position.Y, spawn.Position.Z),
                            Angle = new QAngle(spawn.Angle.Pitch, spawn.Angle.Yaw, spawn.Angle.Roll)
                        });
                    }
                }

                _mapSpawns[kvp.Key] = mapSpawns;
            }

            Console.WriteLine($"[{ModuleName}] Загружено спавнов для {_mapSpawns.Count} карт");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка загрузки спавнов: {ex.Message}");
        }
    }

    private void SaveSpawns()
    {
        try
        {
            var data = new Dictionary<string, SerializableMapSpawns>();

            foreach (var kvp in _mapSpawns)
            {
                var mapSpawns = new SerializableMapSpawns();
                
                foreach (var spawn in kvp.Value.CTSpawns)
                {
                    mapSpawns.CTSpawns.Add(new SerializableSpawnPoint
                    {
                        Position = new SerializableVector { X = spawn.Position.X, Y = spawn.Position.Y, Z = spawn.Position.Z },
                        Angle = new SerializableAngle { Pitch = spawn.Angle.X, Yaw = spawn.Angle.Y, Roll = spawn.Angle.Z }
                    });
                }
                
                foreach (var spawn in kvp.Value.TSpawns)
                {
                    mapSpawns.TSpawns.Add(new SerializableSpawnPoint
                    {
                        Position = new SerializableVector { X = spawn.Position.X, Y = spawn.Position.Y, Z = spawn.Position.Z },
                        Angle = new SerializableAngle { Pitch = spawn.Angle.X, Yaw = spawn.Angle.Y, Roll = spawn.Angle.Z }
                    });
                }

                data[kvp.Key] = mapSpawns;
            }

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SpawnsFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка сохранения спавнов: {ex.Message}");
        }
    }
}
