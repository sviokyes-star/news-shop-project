using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
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
    }

    public class PlayerData
    {
        public int Gold { get; set; } = 0;
        public int Silver { get; set; } = 0;
    }

    private readonly Dictionary<string, List<ShopItem>> _categories = new();
    private readonly Dictionary<ulong, PlayerData> _playerData = new();
    private string DataFilePath => Path.Combine(ModuleDirectory, "players.json");

    public override void Load(bool hotReload)
    {
        InitializeItems();
        LoadData();

        AddCommand("css_shop", "Открыть магазин", OnShopCommand);
        AddCommandListener("say", OnPlayerSay);
        AddCommandListener("say_team", OnPlayerSay);

        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
    }

    public override void Unload(bool hotReload)
    {
        SaveData();
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }

    private void InitializeItems()
    {
        _categories["Скины"] = new List<ShopItem>
        {
            new ShopItem { Name = "AK-47 | Neon", Currency = Currency.Gold, Price = 10, DurationDays = 10 },
            new ShopItem { Name = "AWP | Dragon", Currency = Currency.Gold, Price = 25, DurationDays = 30 },
            new ShopItem { Name = "M4A1 | Silver", Currency = Currency.Silver, Price = 100, DurationDays = 7 },
        };

        _categories["Трейлы"] = new List<ShopItem>
        {
            new ShopItem { Name = "Огненный трейл", Currency = Currency.Gold, Price = 15, DurationDays = 10 },
            new ShopItem { Name = "Ледяной трейл", Currency = Currency.Silver, Price = 150, DurationDays = 14 },
        };
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

    private void ShowMainMenu(CCSPlayerController player)
    {
        var data = GetData(player);
        var menu = new WasdMenu($"Золото: {data.Gold} | Серебро: {data.Silver}", this);

        menu.AddItem("Товары", (controller, option) =>
        {
            ShowCategoriesMenu(controller);
        });

        menu.Display(player, 0);
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

    private void ShowItemsMenu(CCSPlayerController player, string categoryName)
    {
        var menu = new WasdMenu(categoryName, this);

        if (_categories.TryGetValue(categoryName, out var items))
        {
            foreach (var item in items)
            {
                var shopItem = item;
                menu.AddItem(shopItem.Name, (controller, option) =>
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

        menu.AddItem($"Цена: {item.Price} {currencyName}", (_, _) => { }, DisableOption.DisableShowNumber);
        menu.AddItem($"Длительность: {item.DurationDays} дней", (_, _) => { }, DisableOption.DisableShowNumber);

        menu.AddItem("Купить", (controller, option) =>
        {
            BuyItem(controller, item);
        });

        menu.PrevMenu = GetItemsMenu(player, categoryName);
        menu.Display(player, 0);
    }

    private void BuyItem(CCSPlayerController player, ShopItem item)
    {
        var data = GetData(player);
        int balance = item.Currency == Currency.Gold ? data.Gold : data.Silver;
        string currencyName = item.Currency == Currency.Gold ? "золота" : "серебра";

        if (balance < item.Price)
        {
            player.PrintToChat($" {ChatColors.Red}[Магазин] Недостаточно {currencyName}! Нужно: {item.Price}, у вас: {balance}");
            return;
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
                menu.AddItem(shopItem.Name, (controller, option) => ShowItemMenu(controller, categoryName, shopItem));
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
        GiveCurrency(command, Currency.Gold);
    }

    [ConsoleCommand("css_givesilver", "Выдать серебро игроку")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 2, usage: "<userid> <количество>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnGiveSilverCommand(CCSPlayerController? caller, CommandInfo command)
    {
        GiveCurrency(command, Currency.Silver);
    }

    private void GiveCurrency(CommandInfo command, Currency currency)
    {
        if (!int.TryParse(command.GetArg(1), out int userId) || !int.TryParse(command.GetArg(2), out int amount))
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
        string currencyName;
        if (currency == Currency.Gold)
        {
            data.Gold += amount;
            currencyName = "золота";
        }
        else
        {
            data.Silver += amount;
            currencyName = "серебра";
        }

        SaveData();
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