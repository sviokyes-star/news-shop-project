using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using System.Text.Json;

namespace BannerPlugin;

public class BannerPlugin : BasePlugin
{
    public override string ModuleName => "Banner Manager";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Размещение картиночных баннеров (Workshop-моделей) на стенах карт CS2";

    private const float DefaultDistance = 100f;

    private readonly Dictionary<string, List<BannerData>> _mapBanners = new();
    private readonly Dictionary<int, CBaseModelEntity> _spawnedBanners = new();
    private string BannersFilePath => Path.Combine(ModuleDirectory, "banners.json");

    private class BannerData
    {
        public int Id { get; set; }
        public string ModelPath { get; set; } = "";
        public SerializableVector Position { get; set; } = new();
        public SerializableAngle Angle { get; set; } = new();
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

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapStart>(OnMapStart);

        LoadBanners();
        Server.NextFrame(() => SpawnMapBanners(Server.MapName));

        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
    }

    public override void Unload(bool hotReload)
    {
        ClearSpawnedBanners();
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }

    private void OnMapStart(string mapName)
    {
        ClearSpawnedBanners();
        Server.NextFrame(() => SpawnMapBanners(mapName));
    }

    [ConsoleCommand("css_addbanner", "Добавить баннер (Workshop-модель) там, куда смотрит игрок")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 1, usage: "<путь_к_модели.vmdl> [расстояние]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnAddBannerCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.PlayerPawn.Value == null)
            return;

        string modelPath = command.GetArg(1);
        float distance = DefaultDistance;

        if (command.ArgCount > 2 && float.TryParse(command.GetArg(2), out float customDistance))
            distance = customDistance;

        var pawn = player.PlayerPawn.Value;
        var eyePos = pawn.AbsOrigin;
        var eyeAngles = pawn.EyeAngles;

        if (eyePos == null)
            return;

        double yawRad = eyeAngles.Y * Math.PI / 180.0;
        float forwardX = (float)Math.Cos(yawRad);
        float forwardY = (float)Math.Sin(yawRad);

        var position = new Vector(
            eyePos.X + forwardX * distance,
            eyePos.Y + forwardY * distance,
            eyePos.Z
        );
        var angle = new QAngle(0, eyeAngles.Y + 180, 0);

        string mapName = Server.MapName;
        if (!_mapBanners.ContainsKey(mapName))
            _mapBanners[mapName] = new List<BannerData>();

        int newId = _mapBanners[mapName].Count > 0 ? _mapBanners[mapName].Max(b => b.Id) + 1 : 1;

        var banner = new BannerData
        {
            Id = newId,
            ModelPath = modelPath,
            Position = new SerializableVector { X = position.X, Y = position.Y, Z = position.Z },
            Angle = new SerializableAngle { Pitch = angle.X, Yaw = angle.Y, Roll = angle.Z }
        };

        _mapBanners[mapName].Add(banner);
        SaveBanners();

        SpawnBanner(banner);

        player.PrintToChat($" {ChatColors.Green}[Banner Manager]{ChatColors.Default} Баннер #{newId} добавлен!");
        player.PrintToChat($" {ChatColors.Yellow}Модель: {modelPath}");
    }

    [ConsoleCommand("css_removebanner", "Удалить баннер по ID на текущей карте")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 1, usage: "<id>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnRemoveBannerCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!int.TryParse(command.GetArg(1), out int id))
        {
            command.ReplyToCommand($" {ChatColors.Red}[Banner Manager] Некорректный ID");
            return;
        }

        string mapName = Server.MapName;

        if (!_mapBanners.ContainsKey(mapName))
        {
            command.ReplyToCommand($" {ChatColors.Green}[Banner Manager]{ChatColors.Default} Баннеры не настроены");
            return;
        }

        var banner = _mapBanners[mapName].FirstOrDefault(b => b.Id == id);
        if (banner == null)
        {
            command.ReplyToCommand($" {ChatColors.Red}[Banner Manager] Баннер #{id} не найден");
            return;
        }

        _mapBanners[mapName].Remove(banner);
        SaveBanners();

        if (_spawnedBanners.TryGetValue(id, out var entity))
        {
            if (entity?.IsValid == true)
                entity.Remove();
            _spawnedBanners.Remove(id);
        }

        command.ReplyToCommand($" {ChatColors.Green}[Banner Manager]{ChatColors.Default} Баннер #{id} удалён");
    }

    [ConsoleCommand("css_clearbanners", "Удалить все баннеры на текущей карте")]
    [RequiresPermissions("@css/root")]
    public void OnClearBannersCommand(CCSPlayerController? player, CommandInfo command)
    {
        string mapName = Server.MapName;

        if (!_mapBanners.ContainsKey(mapName))
        {
            if (player != null)
                player.PrintToChat($" {ChatColors.Green}[Banner Manager]{ChatColors.Default} Баннеры не настроены");
            return;
        }

        int count = _mapBanners[mapName].Count;
        _mapBanners[mapName].Clear();
        SaveBanners();

        ClearSpawnedBanners();

        if (player != null)
            player.PrintToChat($" {ChatColors.Green}[Banner Manager]{ChatColors.Default} Удалено {count} баннеров");

        Console.WriteLine($"[{ModuleName}] Cleared {count} banners on {mapName}");
    }

    [ConsoleCommand("css_listbanners", "Показать список баннеров на текущей карте")]
    [RequiresPermissions("@css/root")]
    public void OnListBannersCommand(CCSPlayerController? player, CommandInfo command)
    {
        string mapName = Server.MapName;

        if (!_mapBanners.ContainsKey(mapName) || _mapBanners[mapName].Count == 0)
        {
            if (player != null)
                player.PrintToChat($" {ChatColors.Green}[Banner Manager]{ChatColors.Default} Баннеры не настроены");
            else
                Console.WriteLine($"[Banner Manager] Баннеры не настроены на {mapName}");
            return;
        }

        foreach (var banner in _mapBanners[mapName])
        {
            string line = $"#{banner.Id}: {banner.ModelPath} ({banner.Position.X:F0}, {banner.Position.Y:F0}, {banner.Position.Z:F0})";

            if (player != null)
                player.PrintToChat($" {ChatColors.Yellow}{line}");
            else
                Console.WriteLine($"[Banner Manager] {line}");
        }
    }

    private void SpawnMapBanners(string mapName)
    {
        if (!_mapBanners.ContainsKey(mapName))
            return;

        foreach (var banner in _mapBanners[mapName])
        {
            SpawnBanner(banner);
        }

        Console.WriteLine($"[{ModuleName}] Заспавнено баннеров: {_mapBanners[mapName].Count} на карте {mapName}");
    }

    private void SpawnBanner(BannerData banner)
    {
        var entity = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");
        if (entity == null)
        {
            Console.WriteLine($"[{ModuleName}] Не удалось создать entity для баннера #{banner.Id}");
            return;
        }

        entity.SetModel(banner.ModelPath);

        var position = new Vector(banner.Position.X, banner.Position.Y, banner.Position.Z);
        var angle = new QAngle(banner.Angle.Pitch, banner.Angle.Yaw, banner.Angle.Roll);

        entity.Teleport(position, angle, new Vector(0, 0, 0));
        entity.DispatchSpawn();

        _spawnedBanners[banner.Id] = entity;
    }

    private void ClearSpawnedBanners()
    {
        foreach (var entity in _spawnedBanners.Values)
        {
            if (entity?.IsValid == true)
                entity.Remove();
        }
        _spawnedBanners.Clear();
    }

    private void LoadBanners()
    {
        try
        {
            if (!File.Exists(BannersFilePath))
                return;

            string json = File.ReadAllText(BannersFilePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, List<BannerData>>>(json);

            if (data == null)
                return;

            foreach (var kvp in data)
            {
                _mapBanners[kvp.Key] = kvp.Value;
            }

            Console.WriteLine($"[{ModuleName}] Загружено баннеров для {_mapBanners.Count} карт");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка загрузки баннеров: {ex.Message}");
        }
    }

    private void SaveBanners()
    {
        try
        {
            string json = JsonSerializer.Serialize(_mapBanners, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(BannersFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка сохранения баннеров: {ex.Message}");
        }
    }
}
