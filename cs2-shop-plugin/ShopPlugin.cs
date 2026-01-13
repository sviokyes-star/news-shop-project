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
    public override string ModuleVersion => "1.1.2";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Магазин со скинами и валютой для CS2";

    private readonly Dictionary<ulong, PlayerData> _playerData = new();
    private readonly Dictionary<string, ShopItem> _shopItems = new();
    private readonly Dictionary<ulong, string?> _previewSkins = new();
    private readonly Dictionary<ulong, CounterStrikeSharp.API.Modules.Timers.Timer?> _previewTimers = new();
    private readonly List<CBaseModelEntity> _giftBoxes = new();
    private readonly Dictionary<ulong, HashSet<int>> _collectedGifts = new();
    private const float PreviewDuration = 30.0f;
    private const int GiftSilverReward = 1000;
    private string DataFilePath => Path.Combine(ModuleDirectory, "shop_data.json");

    private class PlayerData
    {
        public int Gold { get; set; } = 0;
        public int Silver { get; set; } = 0;
        public List<string> OwnedSkins { get; set; } = new();
        public string? ActiveSkin { get; set; }
        public string? PreviewSkin { get; set; }
        public List<string> OwnedTrails { get; set; } = new();
        public string? ActiveTrail { get; set; }
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
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        
        LoadData();
        InitializeShopItems();
        
        AddTimer(1.0f, CheckGiftPickups, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
        
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

        ShowShopItems(player, "skin");
    }

    [ConsoleCommand("css_12", "Следы")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnMenu12Command(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ShowShopItems(player, "trail");
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

    [ConsoleCommand("css_preview", "Предпросмотр скина")]
    [CommandHelper(minArgs: 1, usage: "<id скина>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnPreviewCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        string skinId = command.GetArg(1).ToLower();
        ulong steamId = player.SteamID;
        var data = GetPlayerData(steamId);

        if (!_shopItems.ContainsKey(skinId))
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Скин не найден!");
            return;
        }

        if (data.OwnedSkins.Contains(skinId))
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} У вас уже есть этот скин! Используйте !setskin {skinId}");
            return;
        }

        if (_previewTimers.ContainsKey(steamId) && _previewTimers[steamId] != null)
        {
            _previewTimers[steamId]?.Kill();
            _previewTimers.Remove(steamId);
        }

        _previewSkins[steamId] = skinId;
        var item = _shopItems[skinId];
        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Предпросмотр: {ChatColors.Yellow}{item.Name}{ChatColors.Default} ({PreviewDuration} сек)");
        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Купить: !buy {skinId} | Отменить: !stoppreview");
        
        ApplySkin(player, skinId);

        _previewTimers[steamId] = AddTimer(PreviewDuration, () => 
        {
            if (player.IsValid && _previewSkins.ContainsKey(steamId))
            {
                _previewSkins.Remove(steamId);
                player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Время предпросмотра истекло");

                if (data.ActiveSkin != null)
                {
                    ApplySkin(player, data.ActiveSkin);
                }
                else
                {
                    RemoveSkin(player);
                }
            }
            _previewTimers.Remove(steamId);
        });
    }

    [ConsoleCommand("css_stoppreview", "Остановить предпросмотр")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnStopPreviewCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ulong steamId = player.SteamID;
        var data = GetPlayerData(steamId);

        if (!_previewSkins.ContainsKey(steamId) || _previewSkins[steamId] == null)
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Предпросмотр не активен");
            return;
        }

        if (_previewTimers.ContainsKey(steamId) && _previewTimers[steamId] != null)
        {
            _previewTimers[steamId]?.Kill();
            _previewTimers.Remove(steamId);
        }

        _previewSkins.Remove(steamId);
        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Предпросмотр отменён");

        if (data.ActiveSkin != null)
        {
            ApplySkin(player, data.ActiveSkin);
        }
        else
        {
            RemoveSkin(player);
        }
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

        if (_previewSkins.ContainsKey(steamId))
        {
            _previewSkins.Remove(steamId);
        }

        if (_previewTimers.ContainsKey(steamId) && _previewTimers[steamId] != null)
        {
            _previewTimers[steamId]?.Kill();
            _previewTimers.Remove(steamId);
        }

        data.ActiveSkin = skinId;
        SaveData();

        var item = _shopItems[skinId];
        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Скин {ChatColors.Yellow}{item.Name}{ChatColors.Default} активирован!");
        
        ApplySkin(player, skinId);
    }

    [ConsoleCommand("css_sell", "Продать товар")]
    [CommandHelper(minArgs: 1, usage: "<id товара>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnSellCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        string itemId = command.GetArg(1).ToLower();
        ulong steamId = player.SteamID;
        var data = GetPlayerData(steamId);

        bool hasItem = data.OwnedSkins.Contains(itemId) || data.OwnedTrails.Contains(itemId);
        if (!hasItem)
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} У вас нет этого товара!");
            return;
        }

        if (!_shopItems.ContainsKey(itemId))
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Товар не найден!");
            return;
        }

        var item = _shopItems[itemId];
        int sellPrice = item.GoldPrice > 0 ? item.GoldPrice / 2 : item.SilverPrice / 2;

        if (data.ActiveSkin == itemId || data.ActiveTrail == itemId)
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Нельзя продать активный товар!");
            return;
        }

        if (item.Type == "skin")
        {
            data.OwnedSkins.Remove(itemId);
        }
        else if (item.Type == "trail")
        {
            data.OwnedTrails.Remove(itemId);
        }
        
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

    [ConsoleCommand("css_addgift", "Добавить подарок на карту")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 1, usage: "<сумма серебра>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnAddGiftCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        if (!int.TryParse(command.GetArg(1), out int silverAmount))
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Неверная сумма!");
            return;
        }

        if (silverAmount <= 0)
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Сумма должна быть больше 0!");
            return;
        }

        var playerPos = player.PlayerPawn?.Value?.AbsOrigin;
        if (playerPos == null)
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Не удалось получить позицию!");
            return;
        }

        SpawnGiftBox(playerPos, silverAmount);
        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Подарок создан! Награда: {ChatColors.Silver}{silverAmount} серебра");
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
        
        string msg = $" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Удалено подарков: {count}";
        
        if (caller != null)
            caller.PrintToChat(msg);
        else
            Console.WriteLine($"[Shop] Удалено подарков: {count}");
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
        var player = @event.Userid;
        if (player != null && player.IsValid)
        {
            ulong steamId = player.SteamID;
            if (_collectedGifts.ContainsKey(steamId))
            {
                _collectedGifts.Remove(steamId);
            }
        }
        
        SaveData();
        return HookResult.Continue;
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



    private void ShowShopMenu(CCSPlayerController player)
    {
        ulong steamId = player.SteamID;
        var data = GetPlayerData(steamId);

        int ownedItems = data.OwnedSkins.Count + data.OwnedTrails.Count;
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
        int totalSkins = _shopItems.Values.Count(i => i.Type == "skin");
        int ownedTrails = data.OwnedTrails.Count;
        int totalTrails = _shopItems.Values.Count(i => i.Type == "trail");

        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Категории товаров:");
        player.PrintToChat($" {ChatColors.Yellow}Скины [{ownedSkins}/{totalSkins}]{ChatColors.Default} - !11");
        player.PrintToChat($" {ChatColors.Yellow}Следы [{ownedTrails}/{totalTrails}]{ChatColors.Default} - !12");
        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Назад: !shop");
    }

    private void ShowShopItems(CCSPlayerController player, string itemType)
    {
        ulong steamId = player.SteamID;
        var data = GetPlayerData(steamId);

        string categoryName = itemType == "skin" ? "скины" : "следы";
        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Доступные {categoryName}:");

        bool hasAvailableItems = false;
        var ownedList = itemType == "skin" ? data.OwnedSkins : data.OwnedTrails;

        foreach (var item in _shopItems.Values.Where(i => i.Type == itemType))
        {
            if (!ownedList.Contains(item.Id))
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
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Вы купили все {categoryName}! - !1 назад");
        }
        else
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Купить: !buy <id> | Предпросмотр: !preview <id>");
        }
    }

    private void ShowSellMenu(CCSPlayerController player)
    {
        ulong steamId = player.SteamID;
        var data = GetPlayerData(steamId);

        int totalItems = data.OwnedSkins.Count + data.OwnedTrails.Count;
        if (totalItems == 0)
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} У вас нет товаров для продажи - !shop назад");
            return;
        }

        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Ваши товары:");

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

        foreach (var trailId in data.OwnedTrails)
        {
            if (_shopItems.ContainsKey(trailId))
            {
                var item = _shopItems[trailId];
                int sellPrice = item.GoldPrice > 0 ? item.GoldPrice / 2 : item.SilverPrice / 2;
                string currency = item.GoldPrice > 0 ? "🪙" : "⚪";
                string active = trailId == data.ActiveTrail ? $" {ChatColors.Green}[АКТИВЕН]" : "";
                
                player.PrintToChat($" {ChatColors.Yellow}{trailId}{ChatColors.Default} - {item.Name} (продать за {sellPrice} {currency}){active}");
            }
        }

        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Продать: !sell <id> | Назад: !shop");
    }

    private void ShowInventory(CCSPlayerController player)
    {
        ulong steamId = player.SteamID;
        var data = GetPlayerData(steamId);

        int totalItems = data.OwnedSkins.Count + data.OwnedTrails.Count;
        if (totalItems == 0)
        {
            player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} У вас пока нет товаров - !shop назад");
            return;
        }

        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Ваш инвентарь:");
        
        if (data.OwnedSkins.Count > 0)
        {
            player.PrintToChat($" {ChatColors.Green}Скины:");
            foreach (var skinId in data.OwnedSkins)
            {
                if (_shopItems.ContainsKey(skinId))
                {
                    var item = _shopItems[skinId];
                    string active = skinId == data.ActiveSkin ? $" {ChatColors.Green}[АКТИВЕН]" : "";
                    player.PrintToChat($" {ChatColors.Yellow}{skinId}{ChatColors.Default} - {item.Name}{active}");
                }
            }
        }

        if (data.OwnedTrails.Count > 0)
        {
            player.PrintToChat($" {ChatColors.Green}Следы:");
            foreach (var trailId in data.OwnedTrails)
            {
                if (_shopItems.ContainsKey(trailId))
                {
                    var item = _shopItems[trailId];
                    string active = trailId == data.ActiveTrail ? $" {ChatColors.Green}[АКТИВЕН]" : "";
                    player.PrintToChat($" {ChatColors.Yellow}{trailId}{ChatColors.Default} - {item.Name}{active}");
                }
            }
        }

        player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Надеть: !setskin <id> | Назад: !shop");
    }

    private void BuyItem(CCSPlayerController player, string itemId)
    {
        ulong steamId = player.SteamID;
        var data = GetPlayerData(steamId);
        var item = _shopItems[itemId];

        var ownedList = item.Type == "skin" ? data.OwnedSkins : data.OwnedTrails;
        
        if (ownedList.Contains(itemId))
        {
            player.PrintToChat($" {ChatColors.Green}[Магазин]{ChatColors.Default} У вас уже есть этот товар!");
            return;
        }

        if (_previewSkins.ContainsKey(steamId))
        {
            _previewSkins.Remove(steamId);
        }

        if (_previewTimers.ContainsKey(steamId) && _previewTimers[steamId] != null)
        {
            _previewTimers[steamId]?.Kill();
            _previewTimers.Remove(steamId);
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

        if (item.Type == "skin")
        {
            data.OwnedSkins.Add(itemId);
        }
        else if (item.Type == "trail")
        {
            data.OwnedTrails.Add(itemId);
        }
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

    private void RemoveSkin(CCSPlayerController player)
    {
        Console.WriteLine($"[Shop] Снятие скина для игрока {player.PlayerName}");
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
        
        Console.WriteLine($"[Shop] Подарок создан на позиции {position.X}, {position.Y}, {position.Z} | Награда: {silverAmount} серебра");
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

                    var data = GetPlayerData(steamId);
                    data.Silver += GiftSilverReward;
                    SaveData();

                    player.PrintToChat($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} Вы подобрали подарок! +{ChatColors.Silver}{GiftSilverReward} серебра");
                    Server.PrintToChatAll($" {ChatColors.Green}[Okyes Shop]{ChatColors.Default} {player.PlayerName} подобрал подарок!");
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
            Name = "Джокер",
            GoldPrice = 0,
            SilverPrice = 100000,
            Type = "skin"
        };

        _shopItems["trail1"] = new ShopItem
        {
            Id = "trail1",
            Name = "Мяч",
            GoldPrice = 0,
            SilverPrice = 100000,
            Type = "trail"
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