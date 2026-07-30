using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace SpawnProtectPlugin;

public class SpawnProtectConfig
{
    // Радиус зоны защиты вокруг каждой точки спавна (в юнитах игры).
    [JsonPropertyName("ZoneRadius")]
    public float ZoneRadius { get; set; } = 250f;

    // Защищать только от урона КАРТЫ (ловушки/триггеры/мир), а не от игроков.
    // true = урон от врагов проходит; false = блокируется любой урон в зоне.
    [JsonPropertyName("OnlyMapDamage")]
    public bool OnlyMapDamage { get; set; } = true;
}

public class SpawnProtectPlugin : BasePlugin
{
    public override string ModuleName => "Spawn Protect";
    public override string ModuleVersion => "1.0.0";
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

        // Хук на получение урона живым существом — здесь можно отменить урон.
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);

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

    private HookResult OnTakeDamage(DynamicHook hook)
    {
        var victim = hook.GetParam<CEntityInstance>(0);
        var info = hook.GetParam<CTakeDamageInfo>(1);

        if (victim == null || !victim.IsValid || info == null)
            return HookResult.Continue;

        // Урон получает игрок?
        if (victim.DesignerName != "player")
            return HookResult.Continue;

        var pawn = victim.As<CCSPlayerPawn>();
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        // Если защищаем только от урона карты — пропускаем урон, у которого
        // атакующий — другой игрок.
        if (Config.OnlyMapDamage && IsPlayerAttacker(info))
            return HookResult.Continue;

        // Игрок в зоне спавна? Если да — отменяем урон.
        var origin = pawn.AbsOrigin;
        if (origin == null)
            return HookResult.Continue;

        if (IsInSpawnZone(origin))
        {
            info.Damage = 0f;
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }

    // Проверяем, является ли атакующий игроком (значит это PvP, не ловушка карты).
    private bool IsPlayerAttacker(CTakeDamageInfo info)
    {
        var attacker = info.Attacker.Value;
        if (attacker == null)
            return false;
        return attacker.DesignerName == "player";
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
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
        _spawnPoints.Clear();
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}