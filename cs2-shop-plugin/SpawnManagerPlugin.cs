using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using System.Drawing;
using System.Text.Json;

namespace SpawnManagerPlugin;

public class SpawnManagerPlugin : BasePlugin
{
    public override string ModuleName => "Spawn & Gift Manager";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Управление спавнами и подарками для CS2";

    private readonly List<CBaseModelEntity> _giftBoxes = new();
    private readonly Dictionary<ulong, HashSet<int>> _collectedGifts = new();
    private readonly List<GiftData> _giftPositions = new();
    private readonly List<CBaseModelEntity> _spawnMarkers = new();
    private readonly List<SpawnData> _customSpawns = new();
    private const int GiftSilverReward = 1000;
    private string GiftsFilePath => Path.Combine(ModuleDirectory, "gifts_data.json");
    private string SpawnsFilePath => Path.Combine(ModuleDirectory, "spawns_data.json");

    private class GiftData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public int SilverAmount { get; set; }
    }

    private class SpawnData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float AngleX { get; set; }
        public float AngleY { get; set; }
        public float AngleZ { get; set; }
        public string Team { get; set; } = "CT";
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        
        LoadGiftsData();
        LoadSpawns();
        
        AddTimer(1.0f, CheckGiftPickups, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
        
        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
        Console.WriteLine($"[{ModuleName}] Загружено подарков: {_giftPositions.Count}");
        Console.WriteLine($"[{ModuleName}] Загружено спавнов: {_customSpawns.Count}");
    }

    [ConsoleCommand("css_addgift", "Добавить подарок на карту")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 1, usage: "<сумма серебра>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnAddGiftCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        if (!int.TryParse(command.GetArg(1), out int silverAmount))
        {
            player.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} Неверная сумма!");
            return;
        }

        if (silverAmount <= 0)
        {
            player.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} Сумма должна быть больше 0!");
            return;
        }

        var playerPos = player.PlayerPawn?.Value?.AbsOrigin;
        if (playerPos == null)
        {
            player.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} Не удалось получить позицию!");
            return;
        }

        SpawnGiftBox(playerPos, silverAmount);
        player.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} Подарок создан! Награда: {ChatColors.Silver}{silverAmount} серебра");
    }

    [ConsoleCommand("css_removegifts", "Удалить все подарки")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnRemoveGiftsCommand(CCSPlayerController? caller, CommandInfo command)
    {
        int count = _giftBoxes.Count;
        
        foreach (var gift in _giftBoxes)
        {
            gift?.Remove();
        }
        
        _giftBoxes.Clear();
        _giftPositions.Clear();
        SaveGifts();
        
        string msg = $" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} Удалено подарков: {count}";
        
        if (caller != null)
            caller.PrintToChat(msg);
        else
            Console.WriteLine($"[Spawn Manager] Удалено подарков: {count}");
    }

    [ConsoleCommand("css_listgifts", "Список всех подарков")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnListGiftsCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (_giftPositions.Count == 0)
        {
            if (caller != null)
                caller.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} Подарков нет");
            else
                Console.WriteLine("[Spawn Manager] Подарков нет");
            return;
        }

        if (caller != null)
            caller.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} {ChatColors.Red}Список подарков:");

        for (int i = 0; i < _giftPositions.Count; i++)
        {
            var gift = _giftPositions[i];
            string msg = $" {ChatColors.Yellow}#{i + 1}{ChatColors.Default} Позиция: ({gift.X:F1}, {gift.Y:F1}, {gift.Z:F1}) | Награда: {gift.SilverAmount}";
            
            if (caller != null)
                caller.PrintToChat(msg);
            else
                Console.WriteLine($"[Spawn Manager] #{i + 1} Позиция: ({gift.X:F1}, {gift.Y:F1}, {gift.Z:F1}) | Награда: {gift.SilverAmount}");
        }
    }

    [ConsoleCommand("css_addspawn", "Добавить спавн")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 1, usage: "<CT/T>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnAddSpawnCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        string team = command.GetArg(1).ToUpper();
        if (team != "CT" && team != "T")
        {
            player.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} Используйте: !addspawn <CT/T>");
            return;
        }

        var playerPos = player.PlayerPawn?.Value?.AbsOrigin;
        var playerAng = player.PlayerPawn?.Value?.EyeAngles;
        
        if (playerPos == null || playerAng == null)
        {
            player.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} Не удалось получить позицию!");
            return;
        }

        var spawnData = new SpawnData
        {
            X = playerPos.X,
            Y = playerPos.Y,
            Z = playerPos.Z,
            AngleX = playerAng.X,
            AngleY = playerAng.Y,
            AngleZ = playerAng.Z,
            Team = team
        };

        _customSpawns.Add(spawnData);
        SaveSpawns();

        player.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} Спавн для {ChatColors.Yellow}{team}{ChatColors.Default} добавлен!");
        Console.WriteLine($"[Spawn Manager] Спавн добавлен: {team} на ({playerPos.X:F1}, {playerPos.Y:F1}, {playerPos.Z:F1})");
    }

    [ConsoleCommand("css_removespawns", "Удалить все спавны")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnRemoveSpawnsCommand(CCSPlayerController? caller, CommandInfo command)
    {
        int count = _customSpawns.Count;
        _customSpawns.Clear();
        
        foreach (var marker in _spawnMarkers)
        {
            marker?.Remove();
        }
        _spawnMarkers.Clear();
        
        SaveSpawns();
        
        string msg = $" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} Удалено спавнов: {count}";
        
        if (caller != null)
            caller.PrintToChat(msg);
        else
            Console.WriteLine($"[Spawn Manager] Удалено спавнов: {count}");
    }

    [ConsoleCommand("css_listspawns", "Список всех спавнов")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnListSpawnsCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (_customSpawns.Count == 0)
        {
            if (caller != null)
                caller.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} Спавнов нет");
            else
                Console.WriteLine("[Spawn Manager] Спавнов нет");
            return;
        }

        if (caller != null)
            caller.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} {ChatColors.Red}Список спавнов:");

        for (int i = 0; i < _customSpawns.Count; i++)
        {
            var spawn = _customSpawns[i];
            string msg = $" {ChatColors.Yellow}#{i + 1} [{spawn.Team}]{ChatColors.Default} Позиция: ({spawn.X:F1}, {spawn.Y:F1}, {spawn.Z:F1})";
            
            if (caller != null)
                caller.PrintToChat(msg);
            else
                Console.WriteLine($"[Spawn Manager] #{i + 1} [{spawn.Team}] Позиция: ({spawn.X:F1}, {spawn.Y:F1}, {spawn.Z:F1})");
        }
    }

    [ConsoleCommand("css_showspawns", "Показать маркеры спавнов")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnShowSpawnsCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        foreach (var marker in _spawnMarkers)
        {
            marker?.Remove();
        }
        _spawnMarkers.Clear();

        foreach (var spawn in _customSpawns)
        {
            var marker = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");
            if (marker == null)
                continue;

            marker.SetModel("models/props/cs_office/cardboard_box01.mdl");
            var position = new Vector(spawn.X, spawn.Y, spawn.Z);
            marker.Teleport(position, new QAngle(0, 0, 0), new Vector(0, 0, 0));
            marker.DispatchSpawn();

            if (spawn.Team == "CT")
            {
                marker.Glow.GlowColorOverride = Color.FromArgb(255, 0, 150, 255);
            }
            else
            {
                marker.Glow.GlowColorOverride = Color.FromArgb(255, 255, 50, 0);
            }
            
            marker.Glow.GlowRange = 1000;
            marker.Glow.GlowRangeMin = 0;
            marker.Glow.GlowType = 3;
            marker.Glow.GlowTeam = -1;

            _spawnMarkers.Add(marker);
        }

        player.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} Показано маркеров: {_spawnMarkers.Count}");
    }

    [ConsoleCommand("css_hidespawns", "Скрыть маркеры спавнов")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnHideSpawnsCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        foreach (var marker in _spawnMarkers)
        {
            marker?.Remove();
        }
        
        int count = _spawnMarkers.Count;
        _spawnMarkers.Clear();

        player.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} Скрыто маркеров: {count}");
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        ulong steamId = player.SteamID;
        if (_collectedGifts.ContainsKey(steamId))
        {
            _collectedGifts[steamId].Clear();
        }

        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Server.NextFrame(() =>
        {
            SpawnAllGifts();
        });

        return HookResult.Continue;
    }

    private void SpawnGiftBox(Vector position, int silverAmount)
    {
        var gift = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");
        if (gift == null)
            return;

        gift.SetModel("models/props/cs_office/cardboard_box01.mdl");
        gift.Teleport(position, new QAngle(0, 0, 0), new Vector(0, 0, 0));
        gift.DispatchSpawn();

        gift.Glow.GlowColorOverride = Color.FromArgb(255, 255, 215, 0);
        gift.Glow.GlowRange = 2000;
        gift.Glow.GlowRangeMin = 0;
        gift.Glow.GlowType = 3;
        gift.Glow.GlowTeam = -1;

        _giftBoxes.Add(gift);
        _giftPositions.Add(new GiftData 
        { 
            X = position.X, 
            Y = position.Y, 
            Z = position.Z, 
            SilverAmount = silverAmount 
        });
        
        SaveGifts();
        
        Console.WriteLine($"[Spawn Manager] Подарок создан на позиции {position.X}, {position.Y}, {position.Z} | Награда: {silverAmount} серебра");
    }

    private void CheckGiftPickups()
    {
        var players = Utilities.GetPlayers().Where(p => p?.IsValid == true && p.PawnIsAlive).ToList();
        
        foreach (var player in players)
        {
            var playerPos = player.PlayerPawn?.Value?.AbsOrigin;
            if (playerPos == null)
                continue;

            for (int i = _giftBoxes.Count - 1; i >= 0; i--)
            {
                var gift = _giftBoxes[i];
                if (gift == null || !gift.IsValid)
                {
                    _giftBoxes.RemoveAt(i);
                    continue;
                }

                var giftPos = gift.AbsOrigin;
                if (giftPos == null)
                    continue;

                float distance = CalculateDistance(playerPos, giftPos);
                
                if (distance < 100.0f)
                {
                    ulong steamId = player.SteamID;
                    
                    if (!_collectedGifts.ContainsKey(steamId))
                    {
                        _collectedGifts[steamId] = new HashSet<int>();
                    }

                    if (_collectedGifts[steamId].Contains(i))
                        continue;

                    _collectedGifts[steamId].Add(i);

                    player.PrintToChat($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} Вы подобрали подарок! +{ChatColors.Silver}{GiftSilverReward} серебра");
                    Server.PrintToChatAll($" {ChatColors.Green}[Spawn Manager]{ChatColors.Default} {player.PlayerName} подобрал подарок!");
                }
            }
        }
    }

    private float CalculateDistance(Vector pos1, Vector pos2)
    {
        float dx = pos1.X - pos2.X;
        float dy = pos1.Y - pos2.Y;
        float dz = pos1.Z - pos2.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private void LoadGiftsData()
    {
        try
        {
            if (!File.Exists(GiftsFilePath))
                return;

            string json = File.ReadAllText(GiftsFilePath);
            var gifts = JsonSerializer.Deserialize<List<GiftData>>(json);

            if (gifts == null)
                return;

            _giftPositions.Clear();
            _giftPositions.AddRange(gifts);

            Console.WriteLine($"[{ModuleName}] Загружено данных подарков: {_giftPositions.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка загрузки подарков: {ex.Message}");
        }
    }

    private void SpawnAllGifts()
    {
        foreach (var gift in _giftBoxes)
        {
            gift?.Remove();
        }
        _giftBoxes.Clear();

        foreach (var giftData in _giftPositions)
        {
            var position = new Vector(giftData.X, giftData.Y, giftData.Z);
            SpawnGiftBoxFromData(position, giftData.SilverAmount);
        }

        Console.WriteLine($"[{ModuleName}] Заспавнено подарков: {_giftBoxes.Count}");
    }

    private void SaveGifts()
    {
        try
        {
            string json = JsonSerializer.Serialize(_giftPositions, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(GiftsFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка сохранения подарков: {ex.Message}");
        }
    }

    private void SpawnGiftBoxFromData(Vector position, int silverAmount)
    {
        var gift = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");
        if (gift == null)
            return;

        gift.SetModel("models/props/cs_office/cardboard_box01.mdl");
        gift.Teleport(position, new QAngle(0, 0, 0), new Vector(0, 0, 0));
        gift.DispatchSpawn();

        gift.Glow.GlowColorOverride = Color.FromArgb(255, 255, 215, 0);
        gift.Glow.GlowRange = 2000;
        gift.Glow.GlowRangeMin = 0;
        gift.Glow.GlowType = 3;
        gift.Glow.GlowTeam = -1;

        _giftBoxes.Add(gift);
    }

    private void LoadSpawns()
    {
        try
        {
            if (!File.Exists(SpawnsFilePath))
                return;

            string json = File.ReadAllText(SpawnsFilePath);
            var spawns = JsonSerializer.Deserialize<List<SpawnData>>(json);

            if (spawns == null)
                return;

            _customSpawns.AddRange(spawns);

            Console.WriteLine($"[{ModuleName}] Загружено спавнов: {_customSpawns.Count}");
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
            string json = JsonSerializer.Serialize(_customSpawns, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SpawnsFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка сохранения спавнов: {ex.Message}");
        }
    }

    public override void Unload(bool hotReload)
    {
        SaveGifts();
        SaveSpawns();
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}
