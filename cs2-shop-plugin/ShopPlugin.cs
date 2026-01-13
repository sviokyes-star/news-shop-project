using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using System.Text.Json;

namespace ShopPlugin;

public class ShopPlugin : BasePlugin
{
    public override string ModuleName => "Shop";
    public override string ModuleVersion => "1.0.7";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Магазин со скинами и валютой для CS2";

    private readonly Dictionary<ulong, PlayerData> _playerData = new();
    private readonly Dictionary<string, ShopItem> _shopItems = new();
    private string DataFilePath => Path.Combine(ModuleDirectory, "shop_data.json");

    private class PlayerData
    {
        public int Gold { get; set; } = 0;
        public int Silver { get; set; } = 0;
        public List<string> OwnedSkins { get; set; } = new();
        public string? ActiveSkin { get; set; }
    }

    private class ShopItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int GoldPrice { get; set; } = 0;
        public int SilverPrice { get; set; } = 0;
        public string Type { get; set; } = "skin";
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        
        LoadData();
        InitializeShopItems();
        
        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
        Console.WriteLine($"[{ModuleName}] Магазин содержит {_shopItems.Count} товаров");
    }

    [ConsoleCommand("css_shop", "Открыть магазин")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnShopCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ShowShopMenu(player);
    }

    [ConsoleCommand("css_1", "Товары")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnMenu1Command(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ShowShopCategories(player);
    }

    [ConsoleCommand("css_11", "Скины")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnMenu11Command(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ShowShopItems(player);
    }

    [ConsoleCommand("css_2", "Продать")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnMenu2Command(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ShowSellMenu(player);
    }

    [ConsoleCommand("css_3", "Инвентарь")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnMenu3Command(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ShowInventory(player);
    }

    [ConsoleCommand("css_balance", "Показать баланс")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnBalanceCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ulong steamId = player.SteamID;
        var data = GetPlayerData(steamId);

        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Ваш баланс:");
        player.PrintToChat($" {ChatColors.Gold}🪙 Золото: {data.Gold}");
        player.PrintToChat($" {ChatColors.Silver}⚪ Серебро: {data.Silver}");
    }

    [ConsoleCommand("css_buy", "Купить товар")]
    [CommandHelper(minArgs: 1, usage: "<id товара>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnBuyCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        string itemId = command.GetArg(1).ToLower();
        
        if (!_shopItems.ContainsKey(itemId))
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Товар не найден! Используйте /shop");
            return;
        }

        BuyItem(player, itemId);
    }

    [ConsoleCommand("css_setskin", "Надеть скин")]
    [CommandHelper(minArgs: 1, usage: "<id скина>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnSetSkinCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        string skinId = command.GetArg(1).ToLower();
        ulong steamId = player.SteamID;
        var data = GetPlayerData(steamId);

        if (!data.OwnedSkins.Contains(skinId))
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} У вас нет этого скина!");
            return;
        }

        data.ActiveSkin = skinId;
        SaveData();

        var item = _shopItems[skinId];
        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Скин {ChatColors.Yellow}{item.Name}{ChatColors.Default} активирован!");
        
        ApplySkin(player, skinId);
    }

    [ConsoleCommand("css_sell", "Продать скин")]
    [CommandHelper(minArgs: 1, usage: "<id скина>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnSellCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        string skinId = command.GetArg(1).ToLower();
        ulong steamId = player.SteamID;
        var data = GetPlayerData(steamId);

        if (!data.OwnedSkins.Contains(skinId))
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} У вас нет этого скина!");
            return;
        }

        if (!_shopItems.ContainsKey(skinId))
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Товар не найден!");
            return;
        }

        var item = _shopItems[skinId];
        int sellPrice = item.GoldPrice > 0 ? item.GoldPrice / 2 : item.SilverPrice / 2;

        if (data.ActiveSkin == skinId)
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Нельзя продать активный скин!");
            return;
        }

        data.OwnedSkins.Remove(skinId);
        
        if (item.GoldPrice > 0)
        {
            int refund = item.GoldPrice / 2;
            data.Gold += refund;
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Продано: {ChatColors.Yellow}{item.Name}{ChatColors.Default} за {ChatColors.Gold}{refund} 🪙");
        }
        else
        {
            data.Silver += sellPrice;
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Продано: {ChatColors.Yellow}{item.Name}{ChatColors.Default} за {ChatColors.Silver}{sellPrice} ⚪");
        }

        SaveData();
    }

    [ConsoleCommand("css_addgold", "Добавить золото игроку")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 2, usage: "<имя игрока> <количество>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnAddGoldCommand(CCSPlayerController? caller, CommandInfo command)
    {
        string targetName = command.GetArg(1);
        if (!int.TryParse(command.GetArg(2), out int amount))
        {
            if (caller != null)
                caller.PrintToChat($" {ChatColors.Green}[Магазин]{ChatColors.Default} Неверное количество");
            return;
        }

        var players = Utilities.GetPlayers();
        var target = players.FirstOrDefault(p => p?.PlayerName?.Contains(targetName, StringComparison.OrdinalIgnoreCase) == true);

        if (target == null)
        {
            if (caller != null)
                caller.PrintToChat($" {ChatColors.Green}[Магазин]{ChatColors.Default} Игрок не найден");
            return;
        }

        var data = GetPlayerData(target.SteamID);
        data.Gold += amount;
        SaveData();

        target.PrintToChat($" {ChatColors.Green}[Магазин]{ChatColors.Default} Вам начислено {ChatColors.Gold}{amount} золота");
        
        if (caller != null)
            caller.PrintToChat($" {ChatColors.Green}[Магазин]{ChatColors.Default} {target.PlayerName} получил {amount} золота");
        
        Console.WriteLine($"[Shop] {target.PlayerName} получил {amount} золота");
    }

    [ConsoleCommand("css_addsilver", "Добавить серебро игроку")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 2, usage: "<имя игрока> <количество>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnAddSilverCommand(CCSPlayerController? caller, CommandInfo command)
    {
        string targetName = command.GetArg(1);
        if (!int.TryParse(command.GetArg(2), out int amount))
        {
            if (caller != null)
                caller.PrintToChat($" {ChatColors.Green}[Магазин]{ChatColors.Default} Неверное количество");
            return;
        }

        var players = Utilities.GetPlayers();
        var target = players.FirstOrDefault(p => p?.PlayerName?.Contains(targetName, StringComparison.OrdinalIgnoreCase) == true);

        if (target == null)
        {
            if (caller != null)
                caller.PrintToChat($" {ChatColors.Green}[Магазин]{ChatColors.Default} Игрок не найден");
            return;
        }

        var data = GetPlayerData(target.SteamID);
        data.Silver += amount;
        SaveData();

        target.PrintToChat($" {ChatColors.Green}[Магазин]{ChatColors.Default} Вам начислено {ChatColors.Silver}{amount} серебра");
        
        if (caller != null)
            caller.PrintToChat($" {ChatColors.Green}[Магазин]{ChatColors.Default} {target.PlayerName} получил {amount} серебра");
        
        Console.WriteLine($"[Shop] {target.PlayerName} получил {amount} серебра");
    }

    private HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        ulong steamId = player.SteamID;
        var data = GetPlayerData(steamId);

        if (data.ActiveSkin != null)
        {
            Server.NextFrame(() =>
            {
                if (player.IsValid && player.PlayerPawn?.Value != null)
                {
                    ApplySkin(player, data.ActiveSkin);
                }
            });
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        SaveData();
        return HookResult.Continue;
    }



    private void ShowShopMenu(CCSPlayerController player)
    {
        ulong steamId = player.SteamID;
        var data = GetPlayerData(steamId);

        int ownedItems = data.OwnedSkins.Count;
        int totalItems = _shopItems.Count;

        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} {ChatColors.Gold}Золото: {data.Gold}{ChatColors.Default} | {ChatColors.Silver}Серебро: {data.Silver}");
        player.PrintToChat($" {ChatColors.Yellow}Товары [{ownedItems}/{totalItems}]{ChatColors.Default} - !1");
        player.PrintToChat($" {ChatColors.Yellow}Продать [{ownedItems}]{ChatColors.Default} - !2");
        player.PrintToChat($" {ChatColors.Yellow}Инвентарь [{ownedItems}]{ChatColors.Default} - !3");
    }

    private void ShowShopCategories(CCSPlayerController player)
    {
        ulong steamId = player.SteamID;
        var data = GetPlayerData(steamId);

        int ownedSkins = data.OwnedSkins.Count;
        int totalSkins = _shopItems.Count;

        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Категории товаров:");
        player.PrintToChat($" {ChatColors.Yellow}Скины [{ownedSkins}/{totalSkins}]{ChatColors.Default} - !11");
        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Назад: !shop");
    }

    private void ShowShopItems(CCSPlayerController player)
    {
        ulong steamId = player.SteamID;
        var data = GetPlayerData(steamId);

        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Доступные скины:");

        bool hasAvailableItems = false;
        foreach (var item in _shopItems.Values)
        {
            if (!data.OwnedSkins.Contains(item.Id))
            {
                hasAvailableItems = true;
                string price = item.GoldPrice > 0 
                    ? $"{ChatColors.Gold}{item.GoldPrice} 🪙" 
                    : $"{ChatColors.Silver}{item.SilverPrice} ⚪";
                
                player.PrintToChat($" {ChatColors.Yellow}{item.Id}{ChatColors.Default} - {item.Name} ({price})");
            }
        }

        if (!hasAvailableItems)
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Вы купили все скины! - !1 назад");
        }
        else
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Купить: !buy <id> | Назад: !1");
        }
    }

    private void ShowSellMenu(CCSPlayerController player)
    {
        ulong steamId = player.SteamID;
        var data = GetPlayerData(steamId);

        if (data.OwnedSkins.Count == 0)
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} У вас нет скинов для продажи - !shop назад");
            return;
        }

        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Ваши скины:");

        foreach (var skinId in data.OwnedSkins)
        {
            if (_shopItems.ContainsKey(skinId))
            {
                var item = _shopItems[skinId];
                int sellPrice = item.GoldPrice > 0 ? item.GoldPrice / 2 : item.SilverPrice / 2;
                string currency = item.GoldPrice > 0 ? "🪙" : "⚪";
                string active = skinId == data.ActiveSkin ? $" {ChatColors.Green}[АКТИВЕН]" : "";
                
                player.PrintToChat($" {ChatColors.Yellow}{skinId}{ChatColors.Default} - {item.Name} (продать за {sellPrice} {currency}){active}");
            }
        }

        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Продать: !sell <id> | Назад: !shop");
    }

    private void ShowInventory(CCSPlayerController player)
    {
        ulong steamId = player.SteamID;
        var data = GetPlayerData(steamId);

        if (data.OwnedSkins.Count == 0)
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} У вас пока нет скинов - !shop назад");
            return;
        }

        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Ваш инвентарь:");
        foreach (var skinId in data.OwnedSkins)
        {
            if (_shopItems.ContainsKey(skinId))
            {
                var item = _shopItems[skinId];
                string active = skinId == data.ActiveSkin ? $" {ChatColors.Green}[АКТИВЕН]" : "";
                player.PrintToChat($" {ChatColors.Yellow}{skinId}{ChatColors.Default} - {item.Name}{active}");
            }
        }
        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Надеть: !setskin <id> | Назад: !shop");
    }

    private void BuyItem(CCSPlayerController player, string itemId)
    {
        ulong steamId = player.SteamID;
        var data = GetPlayerData(steamId);
        var item = _shopItems[itemId];

        if (data.OwnedSkins.Contains(itemId))
        {
            player.PrintToChat($" {ChatColors.Green}[Магазин]{ChatColors.Default} У вас уже есть этот товар!");
            return;
        }

        if (item.GoldPrice > 0)
        {
            if (data.Gold < item.GoldPrice)
            {
                player.PrintToChat($" {ChatColors.Green}[Магазин]{ChatColors.Default} Недостаточно золота! Нужно: {ChatColors.Gold}{item.GoldPrice}");
                return;
            }
            data.Gold -= item.GoldPrice;
        }
        else
        {
            if (data.Silver < item.SilverPrice)
            {
                player.PrintToChat($" {ChatColors.Green}[Магазин]{ChatColors.Default} Недостаточно серебра! Нужно: {ChatColors.Silver}{item.SilverPrice}");
                return;
            }
            data.Silver -= item.SilverPrice;
        }

        data.OwnedSkins.Add(itemId);
        SaveData();

        player.PrintToChat($" {ChatColors.Green}[Магазин]{ChatColors.Default} Куплено: {ChatColors.Yellow}{item.Name}");
        player.PrintToChat($" {ChatColors.Green}[Магазин]{ChatColors.Default} Используйте /setskin {itemId} чтобы надеть");
        
        Server.PrintToChatAll($" {ChatColors.Green}[Магазин]{ChatColors.Default} {player.PlayerName} купил {ChatColors.Yellow}{item.Name}!");
    }

    private void ApplySkin(CCSPlayerController player, string skinId)
    {
        if (!_shopItems.ContainsKey(skinId))
            return;

        var item = _shopItems[skinId];
        
        Console.WriteLine($"[Shop] Применение скина {item.Name} для игрока {player.PlayerName}");
    }

    private PlayerData GetPlayerData(ulong steamId)
    {
        if (!_playerData.ContainsKey(steamId))
        {
            _playerData[steamId] = new PlayerData();
        }
        return _playerData[steamId];
    }

    private void InitializeShopItems()
    {
        _shopItems["skin1"] = new ShopItem
        {
            Id = "skin1",
            Name = "Огненный воин",
            GoldPrice = 0,
            SilverPrice = 100000,
            Type = "skin"
        };

        _shopItems["skin2"] = new ShopItem
        {
            Id = "skin2",
            Name = "Ледяной страж",
            GoldPrice = 0,
            SilverPrice = 100000,
            Type = "skin"
        };

        _shopItems["skin3"] = new ShopItem
        {
            Id = "skin3",
            Name = "Темный рыцарь",
            GoldPrice = 0,
            SilverPrice = 100000,
            Type = "skin"
        };

        _shopItems["skin4"] = new ShopItem
        {
            Id = "skin4",
            Name = "Золотой боец",
            GoldPrice = 0,
            SilverPrice = 100000,
            Type = "skin"
        };

        _shopItems["skin5"] = new ShopItem
        {
            Id = "skin5",
            Name = "Призрак",
            GoldPrice = 0,
            SilverPrice = 100000,
            Type = "skin"
        };
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
            {
                _playerData[kvp.Key] = kvp.Value;
            }

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

    public override void Unload(bool hotReload)
    {
        SaveData();
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}