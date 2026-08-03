using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CS2MenuManager.API.Menu;
using System.Text.Json;

namespace AdminOkyesPlugin;

public class AdminOkyesPlugin : BasePlugin
{
    public override string ModuleName => "Admin [Okyes]";
    public override string ModuleVersion => "1.4.1";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Админ-панель с меню управления игроками и сервером";

    // Ярко-оранжевый цвет (≈ #FF4500) для префикса "Okyes |".
    private const char Orange = '\u0010';

    // Список карт читается из maps.txt рядом с плагином.
    // Каждая строка — одна карта. Поддерживаются форматы:
    //   de_dust2                     — обычная карта (changelevel)
    //   workshop:3070463151          — карта Мастерской по ID (host_workshop_map)
    //   Название|3070463151          — своё название | ID из Мастерской
    private readonly List<string> _maps = new();

    private static readonly string[] DefaultMaps =
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

    private string MapsFilePath => Path.Combine(ModuleDirectory, "maps.txt");

    private void LoadMaps()
    {
        try
        {
            if (!File.Exists(MapsFilePath))
            {
                File.WriteAllText(MapsFilePath,
                    "# Список карт для админ-меню. Одна карта — одна строка.\n" +
                    "# Обычная карта:      de_dust2\n" +
                    "# Карта из Мастерской: workshop:3070463151\n" +
                    "# Со своим названием:  Моя карта|3070463151\n" +
                    string.Join("\n", DefaultMaps) + "\n");
                Console.WriteLine($"[{ModuleName}] Создан maps.txt со списком карт");
            }

            _maps.Clear();
            foreach (var line in File.ReadAllLines(MapsFilePath))
            {
                var s = line.Trim();
                if (s.Length == 0 || s.StartsWith("#"))
                    continue;
                _maps.Add(s);
            }

            if (_maps.Count == 0)
                _maps.AddRange(DefaultMaps);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка чтения maps.txt: {ex.Message}");
            _maps.Clear();
            _maps.AddRange(DefaultMaps);
        }
    }

    public override void Load(bool hotReload)
    {
        AddCommand("css_admin", "Открыть Admin [Okyes]", OnAdminMenuCommand);
        AddCommandListener("say", OnPlayerSay);
        AddCommandListener("say_team", OnPlayerSay);

        LoadVips();
        LoadMaps();
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
                player.PrintToChat($" {Orange}Okyes |{ChatColors.Red} У вас нет прав для использования админ-меню");
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
            caller.PrintToChat($" {Orange}Okyes |{ChatColors.Red} У вас нет прав для использования админ-меню");
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

        menu.AddItem("Управление таймером", (controller, option) =>
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
                controller.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для бана");
                return;
            }

            ShowPlayerSelectMenu(controller, "Кого забанить?", target =>
            {
                Server.ExecuteCommand($"banid 60 {target.UserId}");
                Server.PrintToChatAll($" {Orange}Okyes |{ChatColors.Red} {controller.PlayerName} забанил {target.PlayerName} на 60 минут");
            });
        });

        menu.AddItem("Кикнуть", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/kick") &&
                !AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для кика");
                return;
            }

            ShowPlayerSelectMenu(controller, "Кого кикнуть?", target =>
            {
                Server.ExecuteCommand($"kickid {target.UserId}");
                Server.PrintToChatAll($" {Orange}Okyes |{ChatColors.Red} {controller.PlayerName} кикнул {target.PlayerName}");
            });
        });

        menu.AddItem("Режим полёта", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/slay") &&
                !AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для режима полёта");
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
                controller.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для телепорта");
                return;
            }

            ShowPlayerSelectMenu(controller, "Кого телепортировать?", target =>
            {
                if (TeleportToCrosshair(controller, target))
                    Server.PrintToChatAll($" {Orange}Okyes |{ChatColors.Green} {controller.PlayerName} телепортировал {target.PlayerName} к прицелу");
                else
                    controller.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Не удалось выполнить телепорт");
            });
        });

        menu.AddItem("Бессмертие", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/slay") &&
                !AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для бессмертия");
                return;
            }

            ShowPlayerSelectMenu(controller, "Кому включить бессмертие?", target =>
            {
                bool enabled = ToggleGodMode(target);
                if (enabled)
                    Server.PrintToChatAll($" {Orange}Okyes |{ChatColors.Green} Админ {controller.PlayerName} включил бессмертие для {target.PlayerName}");
                else
                    Server.PrintToChatAll($" {Orange}Okyes |{ChatColors.Yellow} Админ {controller.PlayerName} выключил бессмертие для {target.PlayerName}");
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

    // Включает/выключает бессмертие у игрока. Возвращает true, если бессмертие включено.
    private bool ToggleGodMode(CCSPlayerController target)
    {
        var pawn = target.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return false;

        // TakesDamage=true — урон проходит; false — бессмертие.
        bool enable = pawn.TakesDamage;
        pawn.TakesDamage = !enable;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_bTakesDamage");

        return enable;
    }

    // Телепортирует игрока в точку, куда смотрит прицел админа.
    // Луч считается вручную (без нативных сигнатур) — надёжно на любой версии CS2.
    private bool TeleportToCrosshair(CCSPlayerController admin, CCSPlayerController target)
    {
        try
        {
            var adminPawn = admin.PlayerPawn.Value;
            var targetPawn = target.PlayerPawn.Value;
            if (adminPawn == null || !adminPawn.IsValid || targetPawn == null || !targetPawn.IsValid)
            {
                admin.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Телепорт: игрок недоступен");
                return false;
            }

            var origin = adminPawn.AbsOrigin;
            if (origin == null)
                return false;

            // Уровень пола (ноги админа) и точка его глаз.
            float floorZ = origin.Z;
            var eye = new Vector(origin.X, origin.Y, origin.Z + 64f);

            // Горизонтальное направление взгляда (только по X/Y — куда смотрит прицел).
            var ang = adminPawn.EyeAngles;
            double yaw = ang.Y * Math.PI / 180.0;
            double fx = Math.Cos(yaw);
            double fy = Math.Sin(yaw);

            // Дальность по горизонтали зависит от наклона взгляда:
            // смотришь вниз — ближе, смотришь вперёд — дальше (в разумных пределах).
            float pitch = ang.X; // >0 вниз, <0 вверх
            float dist = 250f + Math.Clamp((0f - pitch), -30f, 60f) * 6f; // ~70..610
            dist = Math.Clamp(dist, 100f, 700f);

            // Ставим игрока НАД точкой (запас по высоте) и включаем гравитацию —
            // движок сам опустит его на реальный пол под точкой.
            // Так не проваливаемся сквозь карту и не застреваем в полу.
            var dest = new Vector(
                eye.X + (float)(fx * dist),
                eye.Y + (float)(fy * dist),
                floorZ + 80f
            );

            // Небольшая скорость вниз, чтобы приземление было мгновенным и стабильным.
            targetPawn.Teleport(dest, targetPawn.AbsRotation, new Vector(0f, 0f, -50f));
            return true;
        }
        catch (Exception ex)
        {
            admin.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Ошибка телепорта: {ex.Message}");
            Console.WriteLine($"[{ModuleName}] TeleportToCrosshair error: {ex}");
            return false;
        }
    }

    private void ShowServerMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Управление сервером", this);

        menu.AddItem("Сменить карту", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root") &&
                !AdminManager.PlayerHasPermissions(controller, "@css/changemap"))
            {
                controller.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для смены карты");
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

        foreach (var entry in _maps)
        {
            // Разбираем строку на отображаемое имя и команду смены карты.
            string display;
            string command;

            if (entry.Contains('|'))
            {
                // "Название|ID" — карта Мастерской со своим названием.
                var parts = entry.Split('|', 2);
                display = parts[0].Trim();
                command = $"host_workshop_map {parts[1].Trim()}";
            }
            else if (entry.StartsWith("workshop:", StringComparison.OrdinalIgnoreCase))
            {
                string id = entry.Substring("workshop:".Length).Trim();
                display = $"Мастерская {id}";
                command = $"host_workshop_map {id}";
            }
            else
            {
                display = entry;
                command = $"changelevel {entry}";
            }

            menu.AddItem(display, (controller, option) =>
            {
                Server.PrintToChatAll($" {Orange}Okyes |{ChatColors.Green} {controller.PlayerName} меняет карту на {display}...");
                Server.ExecuteCommand(command);
            });
        }

        menu.Display(player, 0);
    }

    private void ShowTimerMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Управление таймером", this);

        menu.AddItem("Старт: 1-й угол", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для настройки таймера");
                return;
            }

            if (controller.PlayerPawn.Value == null)
                return;

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_setstart1"));
        });

        menu.AddItem("Старт: 2-й угол (создать)", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для настройки таймера");
                return;
            }

            if (controller.PlayerPawn.Value == null)
                return;

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_setstart2"));
        });

        menu.AddItem("Финиш: 1-й угол", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для настройки таймера");
                return;
            }

            if (controller.PlayerPawn.Value == null)
                return;

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_setend1"));
        });

        menu.AddItem("Финиш: 2-й угол (создать)", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для настройки таймера");
                return;
            }

            if (controller.PlayerPawn.Value == null)
                return;

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_setend2"));
        });

        menu.PrevMenu = GetMainMenu();

        menu.Display(player, 0);
    }

    private void ShowSpawnsMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Управление спавнами", this);

        menu.AddItem("Добавить спавн CT", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для управления спавнами");
                return;
            }

            if (controller.PlayerPawn.Value == null)
                return;

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_setctspawn"));
        });

        menu.AddItem("Добавить спавн T", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для управления спавнами");
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
                controller.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для управления спавнами");
                return;
            }

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_clearctspawns"));
        });

        menu.AddItem("Удалить все T спавны", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для управления спавнами");
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
            caller.PrintToChat($" {Orange}Okyes |{ChatColors.Yellow} На сервере нет игроков");
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

        menu.AddItem("Управление таймером", (controller, option) =>
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
                controller.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для управления магазином");
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
                controller.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для управления магазином");
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
            player.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для управления магазином");
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
                    controller.PrintToChat($" {Orange}Okyes |{ChatColors.Green} {actionName}: {value} {currencyName} — {target.PlayerName}");
                });
            }

            amountMenu.Display(player, 0);
        });
    }

    private static readonly int[] GiftAmounts = { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000 };

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
                controller.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для управления подарками");
                return;
            }

            Server.NextFrame(() => controller.ExecuteClientCommandFromServer("css_gift_remove"));
        });

        menu.AddItem("Убрать все подарки", (controller, option) =>
        {
            if (!AdminManager.PlayerHasPermissions(controller, "@css/root"))
            {
                controller.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для управления подарками");
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
            player.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для управления подарками");
            return;
        }

        var menu = new WasdMenu($"Сколько {currencyName}?", this);

        foreach (int amount in GiftAmounts)
        {
            int value = amount;
            menu.AddItem(value.ToString(), (controller, option) =>
            {
                Server.NextFrame(() => controller.ExecuteClientCommandFromServer($"css_gift {currencyArg} {value}"));
                controller.PrintToChat($" {Orange}Okyes |{ChatColors.Green} Подарок установлен: +{value} {currencyName}");
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
            command.ReplyToCommand($" {Orange}Okyes |{ChatColors.Red} Игрок не найден");
            return;
        }

        string minutes = command.ArgCount > 2 ? command.GetArg(2) : "60";
        string reason = command.ArgCount > 3 ? command.GetArg(3) : "Нарушение правил";
        string adminName = caller?.PlayerName ?? "Console";

        Server.ExecuteCommand($"banid {minutes} {target.UserId}");
        Server.PrintToChatAll($" {Orange}Okyes |{ChatColors.Red} {adminName} забанил {target.PlayerName} на {minutes} мин. Причина: {reason}");
    }

    [ConsoleCommand("css_okkick", "Кикнуть игрока")]
    [RequiresPermissions("@css/kick")]
    [CommandHelper(minArgs: 1, usage: "<имя/userid> [причина]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnKickCommand(CCSPlayerController? caller, CommandInfo command)
    {
        var target = FindTarget(command.GetArg(1));
        if (target == null)
        {
            command.ReplyToCommand($" {Orange}Okyes |{ChatColors.Red} Игрок не найден");
            return;
        }

        string reason = command.ArgCount > 2 ? command.GetArg(2) : "Нарушение правил";
        string adminName = caller?.PlayerName ?? "Console";

        Server.ExecuteCommand($"kickid {target.UserId} {reason}");
        Server.PrintToChatAll($" {Orange}Okyes |{ChatColors.Red} {adminName} кикнул {target.PlayerName}. Причина: {reason}");
    }

    [ConsoleCommand("css_okmap", "Сменить карту")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 1, usage: "<название карты>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnMapCommand(CCSPlayerController? caller, CommandInfo command)
    {
        string map = command.GetArg(1);
        string adminName = caller?.PlayerName ?? "Console";

        Server.PrintToChatAll($" {Orange}Okyes |{ChatColors.Green} {adminName} меняет карту на {map}...");
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
                online.PrintToChat($" {Orange}Okyes |{ChatColors.Yellow} Ваш VIP-статус истёк");
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
            player.PrintToChat($" {Orange}Okyes |{ChatColors.Red} Недостаточно прав для управления VIP");
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
                Server.PrintToChatAll($" {Orange}Okyes |{ChatColors.Green} {controller.PlayerName} забрал VIP у {target.PlayerName}");
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
                    Server.PrintToChatAll($" {Orange}Okyes |{ChatColors.Green} {controller.PlayerName} выдал VIP игроку {target.PlayerName} ({capturedLabel})");
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