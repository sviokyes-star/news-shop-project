using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CS2MenuManager.API.Class;
using CS2MenuManager.API.Enum;
using CS2MenuManager.API.Menu;
using System.Text.Json;

namespace VipPlugin;

public class VipPlugin : BasePlugin
{
    public override string ModuleName => "VIP [Okyes]";
    public override string ModuleVersion => "1.1.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "VIP-плагин с меню в стиле магазина";

    // Ярко-оранжевый цвет (≈ #FF4500) для префикса "Okyes |".
    // В чате CS2 задаётся управляющим байтом \u0010 (Orange).
    private const char Orange = '\u0010';

    // Флаг прав, который выдаётся VIP-игрокам.
    private const string VipFlag = "@css/vip";

    // Количество здоровья, выдаваемое VIP-игроку при спавне.
    private const int VipHealth = 110;

    public override void Load(bool hotReload)
    {
        AddCommand("css_vip", "Открыть VIP-меню", OnVipCommand);
        AddCommandListener("say", OnPlayerSay);
        AddCommandListener("say_team", OnPlayerSay);

        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);

        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
    }

    private bool IsVip(CCSPlayerController player)
    {
        // Главный источник истины — активная запись в vips.json (выдача через
        // меню). Работает и для root, которому выдали VIP через админку.
        if (HasActiveVipRecord(player.SteamID))
            return true;

        // Запасной вариант — флаг @css/vip, но не для root
        // (root обладает всеми правами, поэтому его исключаем).
        if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            return false;

        return AdminManager.PlayerHasPermissions(player, VipFlag);
    }

    // Проверяет активную (не истёкшую) запись VIP в файле админ-плагина.
    private bool HasActiveVipRecord(ulong steamId)
    {
        try
        {
            if (!File.Exists(VipFilePath))
                return false;

            var json = File.ReadAllText(VipFilePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, long>>(json);
            if (data == null || !data.TryGetValue(steamId.ToString(), out var expires))
                return false;

            if (expires == 0)
                return true; // навсегда
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() < expires;
        }
        catch
        {
            return false;
        }
    }

    private HookResult OnPlayerSay(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        string message = info.GetArg(1).Trim();

        if (message.Equals("!vip", StringComparison.OrdinalIgnoreCase))
        {
            ShowMainMenu(player);
            return HookResult.Handled;
        }

        if (message.Equals("!viptest", StringComparison.OrdinalIgnoreCase))
        {
            HandleVipTest(player);
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }

    // Длительность пробного VIP по команде !viptest.
    private const int VipTestDurationSeconds = 3600; // 1 час

    private void HandleVipTest(CCSPlayerController player)
    {
        if (IsVip(player))
        {
            player.PrintToChat($" {Orange}Okyes |{ChatColors.White} У тебя уже есть VIP-статус");
            return;
        }

        if (GrantVip(player.SteamID, VipTestDurationSeconds))
        {
            player.PrintToChat($" {Orange}Okyes |{ChatColors.Green} Тебе выдан пробный VIP на 1 час!");
            player.PrintToChat($" {Orange}Okyes |{ChatColors.White} Напиши {ChatColors.Green}!vip{ChatColors.White} чтобы открыть меню");
        }
        else
        {
            player.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Не удалось выдать VIP, попробуй позже");
        }
    }

    // Записывает временный VIP в vips.json админ-плагина.
    private bool GrantVip(ulong steamId, int durationSeconds)
    {
        try
        {
            var path = VipFilePath;
            Dictionary<string, long> data = new();

            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                data = JsonSerializer.Deserialize<Dictionary<string, long>>(json) ?? new();
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            data[steamId.ToString()] = now + durationSeconds;

            var dir = Path.GetDirectoryName(path);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, JsonSerializer.Serialize(data,
                new JsonSerializerOptions { WriteIndented = true }));

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка выдачи пробного VIP: {ex.Message}");
            return false;
        }
    }

    public void OnVipCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null || !caller.IsValid)
            return;

        ShowMainMenu(caller);
    }

    private void ShowMainMenu(CCSPlayerController player)
    {
        if (!IsVip(player))
        {
            player.PrintToChat($" {Orange}Okyes |{ChatColors.White} У тебя нет VIP-статуса");
            return;
        }

        var menu = new WasdMenu("VIP-меню", this);

        // Срок действия VIP (из файла админ-плагина).
        string expiry = GetVipExpiryText(player.SteamID);
        if (!string.IsNullOrEmpty(expiry))
            menu.AddItem(expiry, (_, _) => { }, DisableOption.DisableHideNumber);

        // Информационный пункт — полностью некликабельный, только показывает бонус.
        menu.AddItem($"Здоровье: {VipHealth}", (_, _) => { }, DisableOption.DisableHideNumber);

        menu.Display(player, 0);
    }

    // Путь к файлу VIP админ-плагина (соседняя папка в plugins/).
    // На сервере папка админ-плагина называется "AdminOkyesPlugin".
    private string VipFilePath
    {
        get
        {
            var pluginsDir = Path.Combine(ModuleDirectory, "..");
            // Возможные имена папки админ-плагина.
            string[] candidates =
            {
                "AdminOkyesPlugin",
                "cs2-admin-okyes-plugin",
            };
            foreach (var name in candidates)
            {
                var path = Path.Combine(pluginsDir, name, "vips.json");
                if (File.Exists(path))
                    return path;
            }
            // По умолчанию — имя папки на сервере.
            return Path.Combine(pluginsDir, "AdminOkyesPlugin", "vips.json");
        }
    }

    // Возвращает строку "VIP до 27.07.2026 12:05" или "VIP: навсегда".
    private string GetVipExpiryText(ulong steamId)
    {
        try
        {
            if (!File.Exists(VipFilePath))
                return "";

            var json = File.ReadAllText(VipFilePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, long>>(json);
            if (data == null || !data.TryGetValue(steamId.ToString(), out var expires))
                return "";

            if (expires == 0)
                return "VIP: навсегда";

            // Московское время (UTC+3).
            var msk = DateTimeOffset.FromUnixTimeSeconds(expires).ToOffset(TimeSpan.FromHours(3));
            return $"VIP до {msk:dd.MM.yyyy HH:mm}";
        }
        catch
        {
            return "";
        }
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !IsVip(player))
            return HookResult.Continue;

        // Несколько попыток — pawn после спавна готов не сразу,
        // а другие плагины/режим могут выставить HP чуть позже.
        ApplyHealthDelayed(player, 0.1f);
        ApplyHealthDelayed(player, 0.5f);
        ApplyHealthDelayed(player, 1.0f);

        return HookResult.Continue;
    }

    private void ApplyHealthDelayed(CCSPlayerController player, float delay)
    {
        AddTimer(delay, () =>
        {
            if (player == null || !player.IsValid || !IsVip(player))
                return;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                return;

            SetHealth(pawn, VipHealth);
        });
    }

    private void SetHealth(CCSPlayerPawn pawn, int health)
    {
        pawn.MaxHealth = health;
        pawn.Health = health;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
    }
}