using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CS2MenuManager.API.Menu;
using CS2TraceRay.Class;
using CS2TraceRay.Enum;
using System.Text.Json;

namespace AdminOkyesPlugin;

public class AdminOkyesPlugin : BasePlugin
{
    public override string ModuleName => "Admin [Okyes]";
    public override string ModuleVersion => "1.3.2";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Админ-панель с меню управления игроками и сервером";

    // Ярко-оранжевый цвет (≈ #FF4500) для префикса "Okyes |".
    private const char Orange = '\u0010';

    private static readonly string[] Maps =
    {
        "de_dust2",
        "de_mirage",
        "de_inferno",
        "de_nuke",
        "de_ancient",
        "de_anubis",
        "de_vertigo",
        "de_train"
    };

    public override void Load(bool hotReload)
    {
        AddCommand("css_admin", "Открыть Admin [Okyes]", OnAdminMenuCommand);
        AddCommandListener("say", OnPlayerSay);
        AddCommandListener("say_team", OnPlayerSay);

        LoadVips();
        RegisterListener<Listeners.OnClientAuthorized>(OnClientAuthorizedVip);
        // Раз в минуту снимаем VIP у тех, чей срок истёк.
        AddTimer(60.0f, CheckExpiredVips, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);

        if (hotReload)
            foreach (var p in Utilities.GetPlayers())
                ApplyVipIfActive(p);

        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
    }

    private HookResult OnPlayerSay(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        string message = info.GetArg(1).Trim();

        if (message.Equals("!admin", StringComparison.OrdinalIgnoreCase))
        {
            if (!HasAdminPermission(player))
            {
                player.PrintToChat($" {ChatColors.Red}[Admin Okyes] У вас нет прав для использования админ-меню");
                return HookResult.Handled;
            }

            ShowMainMenu(player);
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }

    private bool HasAdminPermission(CCSPlayerController player)
    {
        return AdminManager.PlayerHasPermissions(player, "@css/root") ||
               AdminManager.PlayerHasPermissions(player, "@css/kick") ||
               AdminManager.PlayerHasPermissions(player, "@css/ban") ||
               AdminManager.PlayerHasPermissions(player, "@css/generic");
    }

    public void OnAdminMenuCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null || !caller.IsValid)
            return;

        if (!HasAdminPermission(caller))
        {
            caller.PrintToChat($" {ChatColors.Red}[Admin Okyes] У вас нет прав для использования админ-меню");
            return;
        }

        ShowMainMenu(caller);
    }

    private void ShowMainMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Admin [Okyes]", this);

        menu.AddItem("Управление игроками", (controller, option) =>
        {
            ShowPlayersMenu(controller);
        });

        menu.AddItem("Управление сервером", (controller, option) =>
        {
            ShowServerMenu(controller);
        });

        menu.AddItem("Управление Таймером", (controller, option) =>
        {
            ShowTimerMenu(controller);
        });

        menu.AddItem("Управление спавнами", (controller, option) =>
        {
            ShowSpawnsMenu(controller);
        });

        menu.AddItem("Управление магазином", (controller, option) =>
        {
            ShowShopMenu(controller);
        });

        menu.AddItem("Управление подарками", (controller, option) =>
        {
            ShowGiftsMenu(controller);
        });

        menu.AddItem("Управление VIP", (controller, option) =>
        {
            ShowVipMenu(controller);
        });

        menu.Display(player, 0);
    }

    private void ShowPlayersMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Управление игроками", this);

        menu.AddItem("Забанить", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/ban") &&
                !AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для бана");
                return;
            }

            ShowPlayerSelectMenu(controller, "Кого забанить?", target =>
            {
                Server.ExecuteCommand($"banid 60 {target.UserId}");
                Server.PrintToChatAll($" {ChatColors.Red}[Admin Okyes] {controller.PlayerName} забанил {target.PlayerName} на 60 минут");
            });
        });

        menu.AddItem("Кикнуть", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/kick") &&
                !AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для кика");
                return;
            }

            ShowPlayerSelectMenu(controller, "Кого кикнуть?", target =>
            {
                Server.ExecuteCommand($"kickid {target.UserId}");
                Server.PrintToChatAll($" {ChatColors.Red}[Admin Okyes] {controller.PlayerName} кикнул {target.PlayerName}");
            });
        });

        menu.AddItem("Режим полёта", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/slay") &&
                !AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для режима полёта");
                return;
            }

            ShowPlayerSelectMenu(controller, "Кому включить полёт?", target =>
            {
                bool enabled = ToggleNoclip(target);
                if (enabled)
                    Server.PrintToChatAll($" {Orange}Okyes |{ChatColors.Green} Админ {controller.PlayerName} включил режим полёта для {target.PlayerName}");
                else
                    Server.PrintToChatAll($" {Orange}Okyes |{ChatColors.Yellow} Админ {controller.PlayerName} выключил режим полёта для {target.PlayerName}");
            });
        });

        menu.AddItem("Телепорт к прицелу", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/slay") &&
                !AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для телепорта");
                return;
            }

            ShowPlayerSelectMenu(controller, "Кого телепортировать?", target =>
            {
                if (TeleportToCrosshair(controller, target))
                    Server.PrintToChatAll($" {ChatColors.Green}[Admin Okyes] {controller.PlayerName} телепортировал {target.PlayerName} к прицелу");
                else
                    controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Не удалось выполнить телепорт");
            });
        });

        menu.PrevMenu = GetMainMenu();

        menu.Display(player, 0);
    }

    // Кто сейчас в режиме полёта (SteamID64).
    private readonly HashSet<ulong> _flying = new();

    // Включает/выключает режим полёта (noclip) у игрока. Возвращает true, если полёт включён.
    private bool ToggleNoclip(CCSPlayerController target)
    {
        var pawn = target.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return false;

        bool enable = !_flying.Contains(target.SteamID);

        // noclip требует sv_cheats — временно включаем, выполняем от имени игрока, возвращаем.
        Server.ExecuteCommand("sv_cheats 1");
        Server.NextFrame(() =>
        {
            target.ExecuteClientCommandFromServer("noclip");
            AddTimer(0.2f, () => Server.ExecuteCommand("sv_cheats 0"));
        });

        if (enable)
            _flying.Add(target.SteamID);
        else
            _flying.Remove(target.SteamID);

        return enable;
    }

    // Телепортирует игрока в точку, куда смотрит прицел админа.
    private bool TeleportToCrosshair(CCSPlayerController admin, CCSPlayerController target)
    {
        var adminPawn = admin.PlayerPawn.Value;
        var targetPawn = target.PlayerPawn.Value;
        if (adminPawn == null || !adminPawn.IsValid || targetPawn == null || !targetPawn.IsValid)
            return false;

        // Трассировка луча из глаз админа до первого препятствия (стена/пол).
        var trace = admin.GetGameTraceByEyePosition(TraceMask.MaskShot, Contents.Solid, adminPawn);
        if (trace == null)
            return false;

        var hit = trace.Value.EndPos;

        // Немного отступаем от поверхности по нормали и приподнимаем,
        // чтобы игрок не застрял в стене/полу.
        var normal = trace.Value.Normal;
        var dest = new Vector(
            hit.X + normal.X * 16f,
            hit.Y + normal.Y * 16f,
            hit.Z + normal.Z * 16f + 10f
        );

        targetPawn.Teleport(dest, targetPawn.AbsRotation, new Vector(0, 0, 0));
        return true;
    }

    private void ShowServerMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Управление сервером", this);

        menu.AddItem("Сменить карту", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root") &&
                !AdminManager.PlayerHasPermissions(controller, "@css/changemap"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для смены карты");
                return;
            }

            ShowMapSelectMenu(controller);
        });

        menu.PrevMenu = GetMainMenu();

        menu.Display(player, 0);
    }

    private void ShowMapSelectMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Выберите карту", this);

        foreach (var map in Maps)
        {
            menu.AddItem(map, (controller, option) =>
            {
                Server.PrintToChatAll($" {ChatColors.Green}[Admin Okyes] {controller.PlayerName} меняет карту на {map}...");
                Server.ExecuteCommand($"changelevel {map}");
            });
        }

        menu.Display(player, 0);
    }

    private void ShowTimerMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Управление Таймером", this);

        menu.AddItem("Добавить старт", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для настройки таймера");
                return;
            }

            if (controller.PlayerPawn.Value == null)
                return;

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_setstart"));
        });

        menu.AddItem("Добавить финиш", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для настройки таймера");
                return;
            }

            if (controller.PlayerPawn.Value == null)
                return;

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_setend"));
        });

        menu.PrevMenu = GetMainMenu();

        menu.Display(player, 0);
    }

    private void ShowSpawnsMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Управление спавнами", this);

        menu.AddItem("Добавить точку спавна CT", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления спавнами");
                return;
            }

            if (controller.PlayerPawn.Value == null)
                return;

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_setctspawn"));
        });

        menu.AddItem("Добавить точку спавна T", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления спавнами");
                return;
            }

            if (controller.PlayerPawn.Value == null)
                return;

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_settspawn"));
        });

        menu.AddItem("Удалить все CT спавны", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления спавнами");
                return;
            }

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_clearctspawns"));
        });

        menu.AddItem("Удалить все T спавны", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления спавнами");
                return;
            }

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_cleartspawns"));
        });

        menu.PrevMenu = GetMainMenu();

        menu.Display(player, 0);
    }

    private void ShowPlayerSelectMenu(CCSPlayerController caller, string title, Action<CCSPlayerController> onSelect)
    {
        var menu = new WasdMenu(title, this);

        var players = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot).ToList();

        if (players.Count == 0)
        {
            caller.PrintToChat($" {ChatColors.Yellow}[Admin Okyes] На сервере нет игроков");
            return;
        }

        foreach (var target in players)
        {
            menu.AddItem(target.PlayerName, (controller, option) =>
            {
                onSelect(target);
            });
        }

        menu.Display(caller, 0);
    }

    private WasdMenu GetMainMenu()
    {
        var menu = new WasdMenu("Admin [Okyes]", this);

        menu.AddItem("Управление игроками", (controller, option) =>
        {
            ShowPlayersMenu(controller);
        });

        menu.AddItem("Управление сервером", (controller, option) =>
        {
            ShowServerMenu(controller);
        });

        menu.AddItem("Управление Таймером", (controller, option) =>
        {
            ShowTimerMenu(controller);
        });

        menu.AddItem("Управление спавнами", (controller, option) =>
        {
            ShowSpawnsMenu(controller);
        });

        menu.AddItem("Управление магазином", (controller, option) =>
        {
            ShowShopMenu(controller);
        });

        menu.AddItem("Управление подарками", (controller, option) =>
        {
            ShowGiftsMenu(controller);
        });

        menu.AddItem("Управление VIP", (controller, option) =>
        {
            ShowVipMenu(controller);
        });

        return menu;
    }

    private static readonly int[] ShopAmounts = { 10, 50, 100, 500, 1000 };

    private void ShowShopMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Управление магазином", this);

        menu.AddItem("Выдать золото", (controller, option) =>
            ShowShopActionMenu(controller, "css_givegold", "золото", "Выдать"));

        menu.AddItem("Выдать серебро", (controller, option) =>
            ShowShopActionMenu(controller, "css_givesilver", "серебро", "Выдать"));

        menu.AddItem("Забрать золото", (controller, option) =>
            ShowShopActionMenu(controller, "css_takegold", "золото", "Забрать"));

        menu.AddItem("Забрать серебро", (controller, option) =>
            ShowShopActionMenu(controller, "css_takesilver", "серебро", "Забрать"));

        menu.AddItem("Посмотреть баланс игрока", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления магазином");
                return;
            }

            ShowPlayerSelectMenu(controller, "Чей баланс посмотреть?", target =>
            {
                int targetUserId = target.UserId ?? 0;
                Server.NextFrame(() => controller.ExecuteClientCommandFromServer($"css_balance {targetUserId}"));
            });
        });

        menu.AddItem("Перезагрузить товары", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления магазином");
                return;
            }

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_shop_reload"));
        });

        menu.PrevMenu = GetMainMenu();
        menu.Display(player, 0);
    }

    private void ShowShopActionMenu(CCSPlayerController player, string command, string currencyName, string actionName)
    {
        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            player.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления магазином");
            return;
        }

        ShowPlayerSelectMenu(player, $"{actionName} {currencyName} кому?", target =>
        {
            var amountMenu = new WasdMenu($"Сколько {currencyName}?", this);

            foreach (int amount in ShopAmounts)
            {
                int value = amount;
                amountMenu.AddItem(value.ToString(), (controller, option) =>
                {
                    Server.ExecuteCommand($"{command} {target.UserId} {value}");
                    controller.PrintToChat($" {ChatColors.Green}[Admin Okyes] {actionName}: {value} {currencyName} — {target.PlayerName}");
                });
            }

            amountMenu.Display(player, 0);
        });
    }

    private static readonly int[] GiftAmounts = { 5, 10, 25, 50, 100 };

    private void ShowGiftsMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Управление подарками", this);

        menu.AddItem("Поставить подарок (золото)", (controller, option) =>
            ShowGiftAmountMenu(controller, "gold", "золота"));

        menu.AddItem("Поставить подарок (серебро)", (controller, option) =>
            ShowGiftAmountMenu(controller, "silver", "серебра"));

        menu.AddItem("Убрать ближайший подарок", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления подарками");
                return;
            }

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_gift_remove"));
        });

        menu.AddItem("Убрать все подарки", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления подарками");
                return;
            }

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_gift_clear"));
        });

        menu.PrevMenu = GetMainMenu();
        menu.Display(player, 0);
    }

    private void ShowGiftAmountMenu(CCSPlayerController player, string currencyArg, string currencyName)
    {
        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            player.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления подарками");
            return;
        }

        var menu = new WasdMenu($"Сколько {currencyName}?", this);

        foreach (int amount in GiftAmounts)
        {
            int value = amount;
            menu.AddItem(value.ToString(), (controller, option) =>
            {
                Server.NextFrame(() => controller.ExecuteClientCommandFromServer($"css_gift {currencyArg} {value}"));
                controller.PrintToChat($" {ChatColors.Green}[Admin Okyes] Подарок установлен: +{value} {currencyName}");
            });
        }

        menu.Display(player, 0);
    }

    [ConsoleCommand("css_okban", "Забанить игрока")]
    [RequiresPermissions("@css/ban")]
    [CommandHelper(minArgs: 1, usage: "<имя/userid> [время] [причина]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnBanCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var target = FindTarget(command.GetArg(1));
        if (target == null)
        {
            command.ReplyToCommand($" {ChatColors.Red}[Admin Okyes] Игрок не найден");
            return;
        }

        string minutes = command.ArgCount > 2 ? command.GetArg(2) : "60";
        string reason = command.ArgCount > 3 ? command.GetArg(3) : "Нарушение правил";
        string adminName = caller?.PlayerName ?? "Console";

        Server.ExecuteCommand($"banid {minutes} {target.UserId}");
        Server.PrintToChatAll($" {ChatColors.Red}[Admin Okyes] {adminName} забанил {target.PlayerName} на {minutes} мин. Причина: {reason}");
    }

    [ConsoleCommand("css_okkick", "Кикнуть игрока")]
    [RequiresPermissions("@css/kick")]
    [CommandHelper(minArgs: 1, usage: "<имя/userid> [причина]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnKickCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var target = FindTarget(command.GetArg(1));
        if (target == null)
        {
            command.ReplyToCommand($" {ChatColors.Red}[Admin Okyes] Игрок не найден");
            return;
        }

        string reason = command.ArgCount > 2 ? command.GetArg(2) : "Нарушение правил";
        string adminName = caller?.PlayerName ?? "Console";

        Server.ExecuteCommand($"kickid {target.UserId} {reason}");
        Server.PrintToChatAll($" {ChatColors.Red}[Admin Okyes] {adminName} кикнул {target.PlayerName}. Причина: {reason}");
    }

    [ConsoleCommand("css_okmap", "Сменить карту")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 1, usage: "<название карты>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnMapCommand(CCSPlayerController? caller, CommandInfo command)
    {
        string map = command.GetArg(1);
        string adminName = caller?.PlayerName ?? "Console";

        Server.PrintToChatAll($" {ChatColors.Green}[Admin Okyes] {adminName} меняет карту на {map}...");
        Server.ExecuteCommand($"changelevel {map}");
    }

    private CCSPlayerController? FindTarget(string search)
    {
        if (int.TryParse(search, out int userId))
        {
            return Utilities.GetPlayerFromUserid(userId);
        }

        var players = Utilities.GetPlayers();
        return players.FirstOrDefault(p =>
            p.PlayerName.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    // ============ Управление VIP ============

    private const string VipFlag = "@css/vip";

    // SteamID64 -> Unix-время окончания (0 = навсегда).
    private readonly Dictionary<ulong, long> _vips = new();
    private string VipFilePath => Path.Combine(ModuleDirectory, "vips.json");

    private readonly (string Label, long Seconds)[] _vipDurations =
    {
        ("На 1 час", 3600),
        ("На 1 день", 86400),
        ("На 1 неделю", 604800),
        ("На 1 месяц", 2592000),
        ("Навсегда", 0),
    };

    private void LoadVips()
    {
        try
        {
            if (!File.Exists(VipFilePath))
                return;

            var json = File.ReadAllText(VipFilePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, long>>(json);
            if (data == null)
                return;

            _vips.Clear();
            foreach (var kv in data)
                if (ulong.TryParse(kv.Key, out var sid))
                    _vips[sid] = kv.Value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка загрузки vips.json: {ex.Message}");
        }
    }

    private void SaveVips()
    {
        try
        {
            var data = _vips.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
            File.WriteAllText(VipFilePath, JsonSerializer.Serialize(data,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка сохранения vips.json: {ex.Message}");
        }
    }

    private bool IsVipActive(ulong steamId)
    {
        if (!_vips.TryGetValue(steamId, out var expires))
            return false;
        if (expires == 0)
            return true; // навсегда
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() < expires;
    }

    private void OnClientAuthorizedVip(int slot, SteamID id)
    {
        var player = Utilities.GetPlayerFromSlot(slot);
        AddTimer(2.0f, () => ApplyVipIfActive(player));
    }

    private void ApplyVipIfActive(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        // root уже имеет все права — VIP ему не выдаём и не трогаем.
        if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            return;

        ulong sid = player.SteamID;

        // Просроченную/удалённую запись подчищаем.
        if (_vips.ContainsKey(sid) && !IsVipActive(sid))
        {
            _vips.Remove(sid);
            SaveVips();
        }

        // Полная синхронизация флага с нашим файлом:
        // активен в файле -> выдаём, иначе -> обязательно снимаем.
        if (IsVipActive(sid))
        {
            if (!AdminManager.PlayerHasPermissions(player, VipFlag))
                AdminManager.AddPlayerPermissions(player, VipFlag);
        }
        else
        {
            if (AdminManager.PlayerHasPermissions(player, VipFlag))
                AdminManager.RemovePlayerPermissions(player, VipFlag);
        }
    }

    private void CheckExpiredVips()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expired = _vips.Where(kv => kv.Value != 0 && kv.Value <= now)
                           .Select(kv => kv.Key).ToList();
        if (expired.Count == 0)
            return;

        foreach (var sid in expired)
        {
            _vips.Remove(sid);
            var online = Utilities.GetPlayers()
                .FirstOrDefault(p => p.IsValid && !p.IsBot && p.SteamID == sid);
            if (online != null)
            {
                AdminManager.RemovePlayerPermissions(online, VipFlag);
                online.PrintToChat($" {ChatColors.Yellow}[Admin Okyes] Ваш VIP-статус истёк");
            }
        }
        SaveVips();
    }

    private void GrantVip(CCSPlayerController target, long durationSeconds)
    {
        ulong sid = target.SteamID;
        long expires = durationSeconds == 0
            ? 0
            : DateTimeOffset.UtcNow.ToUnixTimeSeconds() + durationSeconds;

        _vips[sid] = expires;
        SaveVips();

        if (!AdminManager.PlayerHasPermissions(target, VipFlag))
            AdminManager.AddPlayerPermissions(target, VipFlag);
    }

    private void RevokeVip(CCSPlayerController target)
    {
        ulong sid = target.SteamID;
        _vips.Remove(sid);
        SaveVips();
        AdminManager.RemovePlayerPermissions(target, VipFlag);

        // Повторное снятие с задержкой — на случай, если права
        // перечитываются из кеша сразу после отзыва.
        AddTimer(0.5f, () =>
        {
            if (target != null && target.IsValid
                && AdminManager.PlayerHasPermissions(target, VipFlag))
            {
                AdminManager.RemovePlayerPermissions(target, VipFlag);
            }
        });
    }

    private bool HasVipPermission(CCSPlayerController player)
    {
        return AdminManager.PlayerHasPermissions(player, "@css/root") ||
               AdminManager.PlayerHasPermissions(player, "@css/ban");
    }

    private void ShowVipMenu(CCSPlayerController player)
    {
        if (!HasVipPermission(player))
        {
            player.PrintToChat($" {ChatColors.Red}[Admin Okyes] Недостаточно прав для управления VIP");
            return;
        }

        var menu = new WasdMenu("Управление VIP", this);

        menu.AddItem("Дать игроку VIP", (controller, option) =>
        {
            ShowVipDurationMenu(controller);
        });

        menu.AddItem("Забрать у игрока VIP", (controller, option) =>
        {
            ShowPlayerSelectMenu(controller, "У кого забрать VIP?", target =>
            {
                RevokeVip(target);
                Server.PrintToChatAll($" {ChatColors.Green}[Admin Okyes] {controller.PlayerName} забрал VIP у {target.PlayerName}");
            });
        });

        menu.PrevMenu = GetMainMenu();
        menu.Display(player, 0);
    }

    private void ShowVipDurationMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("На какой срок выдать VIP?", this);

        foreach (var (label, seconds) in _vipDurations)
        {
            var capturedLabel = label;
            var capturedSeconds = seconds;

            menu.AddItem(capturedLabel, (controller, option) =>
            {
                ShowPlayerSelectMenu(controller, "Кому выдать VIP?", target =>
                {
                    GrantVip(target, capturedSeconds);
                    Server.PrintToChatAll($" {ChatColors.Green}[Admin Okyes] {controller.PlayerName} выдал VIP игроку {target.PlayerName} ({capturedLabel})");
                });
            });
        }

        menu.Display(player, 0);
    }

    public override void Unload(bool hotReload)
    {
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}