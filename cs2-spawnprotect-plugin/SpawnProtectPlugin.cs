using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace SpawnProtectPlugin;

public class SpawnProtectConfig
{
    // Радиус зоны защиты вокруг каждой точки спавна (в юнитах игры).
    [JsonPropertyName("ZoneRadius")]
    public float ZoneRadius { get; set; } = 250f;
}

public class SpawnProtectPlugin : BasePlugin
{
    public override string ModuleName => "Spawn Protect";
    public override string ModuleVersion => "1.1.0";
    public override string ModuleAuthor => "poehali.dev";
    public override string ModuleDescription => "Защита игроков от урона карты в зоне спавна";

    public SpawnProtectConfig Config { get; set; } = new();

    // Позиции всех точек спавна, собранные на старте раунда.
    private readonly List<Vector> _spawnPoints = new();

    private string ConfigPath => Path.Combine(ModuleDirectory, "spawnprotect_config.json");

    public override void Load(bool hotReload)
    {
        LoadOrCreateConfig();

        RegisterEventHandler<EventRoundStart>(OnRoundStart);

        // Каждый тик восстанавливаем HP игрокам в зоне спавна — надёжный способ
        // без хрупких хуков урона: ловушка карты снимает HP, а мы возвращаем его,
        // делая игрока фактически бессмертным в зоне спавна.
        RegisterListener<Listeners.OnTick>(OnTick);

        // Соберём спавны сразу при загрузке (если карта уже идёт).
        CollectSpawnPoints();

        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
    }

    private void LoadOrCreateConfig()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var loaded = JsonSerializer.Deserialize<SpawnProtectConfig>(File.ReadAllText(ConfigPath));
                if (loaded != null) Config = loaded;
                Console.WriteLine($"[{ModuleName}] Конфиг загружен: {ConfigPath}");
            }
            else
            {
                SaveConfig();
                Console.WriteLine($"[{ModuleName}] Создан конфиг по умолчанию: {ConfigPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка конфига: {ex.Message}");
            Config = new SpawnProtectConfig();
        }
    }

    private void SaveConfig()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(Config, options));
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        CollectSpawnPoints();
        return HookResult.Continue;
    }

    // Собираем координаты всех точек спавна обеих команд.
    private void CollectSpawnPoints()
    {
        _spawnPoints.Clear();

        foreach (var name in new[] { "info_player_terrorist", "info_player_counterterrorist" })
        {
            foreach (var ent in Utilities.FindAllEntitiesByDesignerName<SpawnPoint>(name))
            {
                var origin = ent.AbsOrigin;
                if (origin != null)
                    _spawnPoints.Add(new Vector(origin.X, origin.Y, origin.Z));
            }
        }

        Console.WriteLine($"[{ModuleName}] Найдено точек спавна: {_spawnPoints.Count}");
    }

    private void OnTick()
    {
        // Нет точек спавна — нечего защищать.
        if (_spawnPoints.Count == 0)
            return;

        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive)
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            var origin = pawn.AbsOrigin;
            if (origin == null)
                continue;

            if (!IsInSpawnZone(origin))
                continue;

            // Игрок в зоне спавна — держим HP на максимуме, чтобы урон карты
            // не мог его убить. Обновляем только если HP просело (без лишней работы).
            int maxHp = pawn.MaxHealth > 0 ? pawn.MaxHealth : 100;
            if (pawn.Health < maxHp)
            {
                pawn.Health = maxHp;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            }
        }
    }

    private bool IsInSpawnZone(Vector origin)
    {
        float r2 = Config.ZoneRadius * Config.ZoneRadius;
        foreach (var sp in _spawnPoints)
        {
            float dx = origin.X - sp.X;
            float dy = origin.Y - sp.Y;
            float dz = origin.Z - sp.Z;
            if (dx * dx + dy * dy + dz * dz <= r2)
                return true;
        }
        return false;
    }

    [ConsoleCommand("css_spawnprotect_reload", "Перезагрузить конфиг защиты спавна")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnReloadCommand(CCSPlayerController? player, CommandInfo command)
    {
        LoadOrCreateConfig();
        CollectSpawnPoints();
        command.ReplyToCommand($"[Spawn Protect] Конфиг перезагружен. Радиус зоны: {Config.ZoneRadius}, точек спавна: {_spawnPoints.Count}");
    }

    public override void Unload(bool hotReload)
    {
        _spawnPoints.Clear();
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}