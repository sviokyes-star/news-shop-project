using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CS2MenuManager.API.Enum;
using CS2MenuManager.API.Menu;
using System.Text.Json;

namespace ShopPlugin;

public class ShopPlugin : BasePlugin
{
    public override string ModuleName => "Shop [Okyes]";
    public override string ModuleVersion => "2.0.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Магазин с меню, двумя валютами (Золото/Серебро) и товарами";

    public enum Currency { Gold, Silver }

    public class ShopItem
    {
        public string Name { get; set; } = "";
        public Currency Currency { get; set; }
        public int Price { get; set; }
        public int DurationDays { get; set; }
        public string Model { get; set; } = "";
    }

    public class Purchase
    {
        public string ItemName { get; set; } = "";
        public string Category { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
        public bool Enabled { get; set; } = true;
    }

    public class PlayerData
    {
        public int Gold { get; set; } = 0;
        public int Silver { get; set; } = 0;
        public List<Purchase> Purchases { get; set; } = new();
    }

    public class ConfigItem
    {
        public string Name { get; set; } = "";
        public string Currency { get; set; } = "Gold";
        public int Price { get; set; }
        public int DurationDays { get; set; }
        public string Model { get; set; } = "";
    }

    private readonly Dictionary<string, List<ShopItem>> _categories = new();
    private readonly Dictionary<ulong, PlayerData> _playerData = new();
    private string DataFilePath => Path.Combine(ModuleDirectory, "players.json");
    private string ItemsFilePath => Path.Combine(ModuleDirectory, "items.json");

    public override void Load(bool hotReload)
    {
        InitializeItems();
        LoadData();

        AddCommand("css_shop", "Открыть магазин", OnShopCommand);
        AddCommandListener("say", OnPlayerSay);
        AddCommandListener("say_team", OnPlayerSay);
        AddCommand("css_shop_reload", "Перезагрузить товары из items.json", OnReloadCommand);

        RegisterListener<Listeners.OnServerPrecacheResources>(OnPrecacheResources);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);

        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
    }

    private void OnPrecacheResources(ResourceManifest manifest)
    {
        foreach (var category in _categories.Values)
        {
            foreach (var item in category)
            {
                if (!string.IsNullOrEmpty(item.Model))
                    manifest.AddResource(item.Model);
            }
        }
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        var skinModel = GetActiveSkinModel(player);
        if (string.IsNullOrEmpty(skinModel))
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            if (!player.IsValid)
                return;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                return;

            pawn.SetModel(skinModel);
        });

        return HookResult.Continue;
    }

    private string GetActiveSkinModel(CCSPlayerController player)
    {
        var data = GetData(player);

        var activeSkin = data.Purchases.FirstOrDefault(p =>
            p.Category == "Скины" && p.Enabled && p.ExpiresAt > DateTime.UtcNow);

        if (activeSkin == null)
            return "";

        if (!_categories.TryGetValue("Скины", out var skins))
            return "";

        var item = skins.FirstOrDefault(s => s.Name == activeSkin.ItemName);
        return item?.Model ?? "";
    }

    public override void Unload(bool hotReload)
    {
        SaveData();
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }

    private void InitializeItems()
    {
        _categories.Clear();

        if (!File.Exists(ItemsFilePath))
        {
            CreateDefaultItemsConfig();
        }

        try
        {
            string json = File.ReadAllText(ItemsFilePath);
            var config = JsonSerializer.Deserialize<Dictionary<string, List<ConfigItem>>>(json);

            if (config == null)
            {
                Console.WriteLine($"[{ModuleName}] Файл items.json пустой или некорректный");
                return;
            }

            foreach (var category in config)
            {
                var items = new List<ShopItem>();
                foreach (var configItem in category.Value)
                {
                    var currency = configItem.Currency.Equals("Silver", StringComparison.OrdinalIgnoreCase)
                        ? Currency.Silver
                        : Currency.Gold;

                    items.Add(new ShopItem
                    {
                        Name = configItem.Name,
                        Currency = currency,
                        Price = configItem.Price,
                        DurationDays = configItem.DurationDays,
                        Model = configItem.Model
                    });
                }
                _categories[category.Key] = items;
            }

            Console.WriteLine($"[{ModuleName}] Загружено категорий: {_categories.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка загрузки items.json: {ex.Message}");
        }
    }

    private void CreateDefaultItemsConfig()
    {
        var defaultConfig = new Dictionary<string, List<ConfigItem>>
        {
            ["Скины"] = new List<ConfigItem>
            {
                new ConfigItem { Name = "Спецназовец", Currency = "Gold", Price = 10, DurationDays = 10, Model = "characters/models/ctm_sas/ctm_sas.vmdl" },
                new ConfigItem { Name = "Пират", Currency = "Gold", Price = 25, DurationDays = 30, Model = "characters/models/tm_phoenix/tm_phoenix.vmdl" },
                new ConfigItem { Name = "Профессионал", Currency = "Silver", Price = 100, DurationDays = 7, Model = "characters/models/ctm_fbi/ctm_fbi.vmdl" },
            },
            ["Трейлы"] = new List<ConfigItem>
            {
                new ConfigItem { Name = "Огненный трейл", Currency = "Gold", Price = 15, DurationDays = 10, Model = "" },
                new ConfigItem { Name = "Ледяной трейл", Currency = "Silver", Price = 150, DurationDays = 14, Model = "" },
            }
        };

        try
        {
            string json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(ItemsFilePath, json);
            Console.WriteLine($"[{ModuleName}] Создан файл конфигурации items.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка создания items.json: {ex.Message}");
        }
    }

    private PlayerData GetData(CCSPlayerController player)
    {
        ulong id = player.SteamID;
        if (!_playerData.ContainsKey(id))
            _playerData[id] = new PlayerData();
        return _playerData[id];
    }

    private HookResult OnPlayerSay(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        string message = info.GetArg(1).Trim();

        if (message.Equals("!shop", StringComparison.OrdinalIgnoreCase))
        {
            ShowMainMenu(player);
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }

    public void OnShopCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null || !caller.IsValid)
            return;

        ShowMainMenu(caller);
    }

    [RequiresPermissions("@css/root")]
    public void OnReloadCommand(CCSPlayerController? caller, CommandInfo command)
    {
        InitializeItems();

        string message = $" {ChatColors.Green}[Магазин] Товары перезагружены. Категорий: {_categories.Count}";
        if (caller != null && caller.IsValid)
            caller.PrintToChat(message);
        else
            command.ReplyToCommand(message);
    }

    private void ShowMainMenu(CCSPlayerController player)
    {
        var data = GetData(player);
        var menu = new WasdMenu($"Золото: {data.Gold} | Серебро: {data.Silver}", this);

        menu.AddItem("Товары", (controller, option) =>
        {
            ShowCategoriesMenu(controller);
        });

        menu.AddItem("Мои покупки", (controller, option) =>
        {
            ShowPurchasesMenu(controller);
        });

        menu.Display(player, 0);
    }

    private void ShowPurchasesMenu(CCSPlayerController player)
    {
        var data = GetData(player);
        CleanupExpired(data);

        var menu = new WasdMenu("Мои покупки", this);

        if (data.Purchases.Count == 0)
        {
            menu.AddItem("Нет активных покупок", (_, _) => { }, DisableOption.DisableShowNumber);
        }
        else
        {
            foreach (var purchase in data.Purchases)
            {
                var current = purchase;
                int daysLeft = Math.Max(0, (int)Math.Ceiling((current.ExpiresAt - DateTime.UtcNow).TotalDays));
                string status = current.Enabled ? "[Вкл]" : "[Выкл]";
                menu.AddItem($"{status} {current.ItemName} — осталось {daysLeft} дн.", (controller, option) =>
                {
                    ShowPurchaseMenu(controller, current);
                });
            }
        }

        menu.PrevMenu = GetMainMenu(player);
        menu.Display(player, 0);
    }

    private void ShowPurchaseMenu(CCSPlayerController player, Purchase purchase)
    {
        int daysLeft = Math.Max(0, (int)Math.Ceiling((purchase.ExpiresAt - DateTime.UtcNow).TotalDays));
        var menu = new WasdMenu(purchase.ItemName, this);

        menu.AddItem($"Категория: {purchase.Category}", (_, _) => { }, DisableOption.DisableShowNumber);
        menu.AddItem($"Осталось: {daysLeft} дней", (_, _) => { }, DisableOption.DisableShowNumber);
        menu.AddItem($"Статус: {(purchase.Enabled ? "Включён" : "Отключён")}", (_, _) => { }, DisableOption.DisableShowNumber);

        string actionText = purchase.Enabled ? "Отключить" : "Включить";
        menu.AddItem(actionText, (controller, option) =>
        {
            var data = GetData(controller);
            var target = data.Purchases.FirstOrDefault(p => p.ItemName == purchase.ItemName);
            if (target == null)
            {
                ShowPurchasesMenu(controller);
                return;
            }

            target.Enabled = !target.Enabled;

            if (target.Enabled)
            {
                foreach (var other in data.Purchases)
                {
                    if (other.Category == target.Category && other.ItemName != target.ItemName)
                        other.Enabled = false;
                }
            }

            SaveData();

            if (target.Enabled)
                controller.PrintToChat($" {ChatColors.Green}[Магазин] {target.ItemName} включён");
            else
                controller.PrintToChat($" {ChatColors.Red}[Магазин] {target.ItemName} отключён");

            ShowPurchaseMenu(controller, target);
        });

        menu.PrevMenu = GetPurchasesMenu(player);
        menu.Display(player, 0);
    }

    private WasdMenu GetPurchasesMenu(CCSPlayerController player)
    {
        var data = GetData(player);
        var menu = new WasdMenu("Мои покупки", this);

        if (data.Purchases.Count == 0)
        {
            menu.AddItem("Нет активных покупок", (_, _) => { }, DisableOption.DisableShowNumber);
        }
        else
        {
            foreach (var purchase in data.Purchases)
            {
                var current = purchase;
                int daysLeft = Math.Max(0, (int)Math.Ceiling((current.ExpiresAt - DateTime.UtcNow).TotalDays));
                string status = current.Enabled ? "[Вкл]" : "[Выкл]";
                menu.AddItem($"{status} {current.ItemName} — осталось {daysLeft} дн.", (controller, option) =>
                {
                    ShowPurchaseMenu(controller, current);
                });
            }
        }

        menu.PrevMenu = GetMainMenu(player);
        return menu;
    }

    private void CleanupExpired(PlayerData data)
    {
        int before = data.Purchases.Count;
        data.Purchases.RemoveAll(p => p.ExpiresAt <= DateTime.UtcNow);
        if (data.Purchases.Count != before)
            SaveData();
    }

    private void ShowCategoriesMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Товары", this);

        foreach (var category in _categories.Keys)
        {
            string categoryName = category;
            menu.AddItem(categoryName, (controller, option) =>
            {
                ShowItemsMenu(controller, categoryName);
            });
        }

        menu.PrevMenu = GetMainMenu(player);
        menu.Display(player, 0);
    }

    private bool IsOwned(CCSPlayerController player, ShopItem item)
    {
        var data = GetData(player);
        return data.Purchases.Any(p => p.ItemName == item.Name && p.ExpiresAt > DateTime.UtcNow);
    }

    private string ItemLabel(CCSPlayerController player, ShopItem item)
    {
        return IsOwned(player, item) ? $"{item.Name} (куплено)" : item.Name;
    }

    private void ShowItemsMenu(CCSPlayerController player, string categoryName)
    {
        var menu = new WasdMenu(categoryName, this);

        if (_categories.TryGetValue(categoryName, out var items))
        {
            foreach (var item in items)
            {
                var shopItem = item;
                menu.AddItem(ItemLabel(player, shopItem), (controller, option) =>
                {
                    ShowItemMenu(controller, categoryName, shopItem);
                });
            }
        }

        menu.PrevMenu = GetCategoriesMenu(player);
        menu.Display(player, 0);
    }

    private void ShowItemMenu(CCSPlayerController player, string categoryName, ShopItem item)
    {
        string currencyName = item.Currency == Currency.Gold ? "золота" : "серебра";
        var menu = new WasdMenu(item.Name, this);

        bool owned = IsOwned(player, item);

        menu.AddItem($"Цена: {item.Price} {currencyName}", (_, _) => { }, DisableOption.DisableShowNumber);
        menu.AddItem($"Длительность: {item.DurationDays} дней", (_, _) => { }, DisableOption.DisableShowNumber);

        if (owned)
            menu.AddItem("Уже куплено", (_, _) => { }, DisableOption.DisableShowNumber);

        menu.AddItem(owned ? "Продлить" : "Купить", (controller, option) =>
        {
            BuyItem(controller, categoryName, item);
        });

        menu.PrevMenu = GetItemsMenu(player, categoryName);
        menu.Display(player, 0);
    }

    private void BuyItem(CCSPlayerController player, string categoryName, ShopItem item)
    {
        var data = GetData(player);
        int balance = item.Currency == Currency.Gold ? data.Gold : data.Silver;
        string currencyName = item.Currency == Currency.Gold ? "золота" : "серебра";

        if (balance < item.Price)
        {
            player.PrintToChat($" {ChatColors.Red}[Магазин] Недостаточно {currencyName}! Нужно: {item.Price}, у вас: {balance}");
            return;
        }

        var existing = data.Purchases.FirstOrDefault(p => p.ItemName == item.Name);
        if (existing != null)
        {
            existing.ExpiresAt = existing.ExpiresAt.AddDays(item.DurationDays);
        }
        else
        {
            data.Purchases.Add(new Purchase
            {
                ItemName = item.Name,
                Category = categoryName,
                ExpiresAt = DateTime.UtcNow.AddDays(item.DurationDays)
            });
        }

        if (item.Currency == Currency.Gold)
            data.Gold -= item.Price;
        else
            data.Silver -= item.Price;

        SaveData();

        player.PrintToChat($" {ChatColors.Green}[Магазин] Вы купили {ChatColors.Gold}{item.Name}{ChatColors.Green} на {item.DurationDays} дней!");
    }

    private WasdMenu GetMainMenu(CCSPlayerController player)
    {
        var data = GetData(player);
        var menu = new WasdMenu($"Золото: {data.Gold} | Серебро: {data.Silver}", this);
        menu.AddItem("Товары", (controller, option) => ShowCategoriesMenu(controller));
        menu.AddItem("Мои покупки", (controller, option) => ShowPurchasesMenu(controller));
        return menu;
    }

    private WasdMenu GetCategoriesMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Товары", this);
        foreach (var category in _categories.Keys)
        {
            string categoryName = category;
            menu.AddItem(categoryName, (controller, option) => ShowItemsMenu(controller, categoryName));
        }
        menu.PrevMenu = GetMainMenu(player);
        return menu;
    }

    private WasdMenu GetItemsMenu(CCSPlayerController player, string categoryName)
    {
        var menu = new WasdMenu(categoryName, this);
        if (_categories.TryGetValue(categoryName, out var items))
        {
            foreach (var item in items)
            {
                var shopItem = item;
                menu.AddItem(ItemLabel(player, shopItem), (controller, option) => ShowItemMenu(controller, categoryName, shopItem));
            }
        }
        menu.PrevMenu = GetCategoriesMenu(player);
        return menu;
    }

    [ConsoleCommand("css_givegold", "Выдать золото игроку")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 2, usage: "<userid> <количество>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnGiveGoldCommand(CCSPlayerController? caller, CommandInfo command)
    {
        ChangeCurrency(command, Currency.Gold, false);
    }

    [ConsoleCommand("css_givesilver", "Выдать серебро игроку")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 2, usage: "<userid> <количество>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnGiveSilverCommand(CCSPlayerController? caller, CommandInfo command)
    {
        ChangeCurrency(command, Currency.Silver, false);
    }

    [ConsoleCommand("css_takegold", "Забрать золото у игрока")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 2, usage: "<userid> <количество>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnTakeGoldCommand(CCSPlayerController? caller, CommandInfo command)
    {
        ChangeCurrency(command, Currency.Gold, true);
    }

    [ConsoleCommand("css_takesilver", "Забрать серебро у игрока")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 2, usage: "<userid> <количество>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnTakeSilverCommand(CCSPlayerController? caller, CommandInfo command)
    {
        ChangeCurrency(command, Currency.Silver, true);
    }

    [ConsoleCommand("css_balance", "Показать баланс игрока")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 1, usage: "<userid>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnBalanceCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (!int.TryParse(command.GetArg(1), out int userId))
        {
            command.ReplyToCommand($" {ChatColors.Red}[Магазин] Некорректные аргументы");
            return;
        }

        var target = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.UserId == userId);
        if (target == null)
        {
            command.ReplyToCommand($" {ChatColors.Red}[Магазин] Игрок не найден");
            return;
        }

        var data = GetData(target);
        string message = $" {ChatColors.Green}[Магазин] Баланс {target.PlayerName}: {ChatColors.Gold}Золото: {data.Gold}{ChatColors.Green} | {ChatColors.Silver}Серебро: {data.Silver}";

        if (caller != null && caller.IsValid)
            caller.PrintToChat(message);
        else
            command.ReplyToCommand(message);
    }

    private void ChangeCurrency(CommandInfo command, Currency currency, bool take)
    {
        if (!int.TryParse(command.GetArg(1), out int userId) || !int.TryParse(command.GetArg(2), out int amount) || amount < 0)
        {
            command.ReplyToCommand($" {ChatColors.Red}[Магазин] Некорректные аргументы");
            return;
        }

        var target = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.UserId == userId);
        if (target == null)
        {
            command.ReplyToCommand($" {ChatColors.Red}[Магазин] Игрок не найден");
            return;
        }

        var data = GetData(target);
        string currencyName = currency == Currency.Gold ? "золота" : "серебра";

        if (currency == Currency.Gold)
            data.Gold = take ? Math.Max(0, data.Gold - amount) : data.Gold + amount;
        else
            data.Silver = take ? Math.Max(0, data.Silver - amount) : data.Silver + amount;

        SaveData();

        if (take)
            target.PrintToChat($" {ChatColors.Red}[Магазин] У вас забрали {amount} {currencyName}");
        else
            target.PrintToChat($" {ChatColors.Green}[Магазин] Вам выдано {amount} {currencyName}!");
    }

    private void LoadData()
    {
        try
        {
            if (!File.Exists(DataFilePath))
                return;

            string json = File.ReadAllText(DataFilePath);
            var data = JsonSerializer.Deserialize<Dictionary<ulong, PlayerData>>(json);

            if (data == null)
                return;

            foreach (var kvp in data)
                _playerData[kvp.Key] = kvp.Value;

            Console.WriteLine($"[{ModuleName}] Загружено данных: {_playerData.Count} игроков");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка загрузки данных: {ex.Message}");
        }
    }

    private void SaveData()
    {
        try
        {
            string json = JsonSerializer.Serialize(_playerData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(DataFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка сохранения данных: {ex.Message}");
        }
    }
}