using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;

namespace ChatTagsPlugin;

public class ChatTagsPlugin : BasePlugin
{
    public override string ModuleName => "Chat Tags [Okyes]";
    public override string ModuleVersion => "2.0.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Клан-теги [ADMIN] и [VIP] перед ником (чат + таблица)";

    private const string AdminTag = "[ADMIN]";
    private const string VipTag = "[VIP]";

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
        RegisterListener<Listeners.OnClientAuthorized>(OnClientAuthorized);

        // При горячей перезагрузке проставляем теги уже подключённым.
        if (hotReload)
            foreach (var p in Utilities.GetPlayers())
                ApplyTag(p);

        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
    }

    private void OnClientAuthorized(int slot, SteamID id)
    {
        var player = Utilities.GetPlayerFromSlot(slot);
        // Права подгружаются чуть позже авторизации — ставим тег с задержкой.
        AddTimer(1.0f, () => ApplyTag(player));
        AddTimer(3.0f, () => ApplyTag(player));
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        ApplyTag(@event.Userid);
        return HookResult.Continue;
    }

    private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;
        AddTimer(0.2f, () => ApplyTag(player));
        return HookResult.Continue;
    }

    private void ApplyTag(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
            return;

        string tag = GetTag(player);
        if (string.IsNullOrEmpty(tag))
            return;

        if (player.Clan == tag)
            return;

        player.Clan = tag;
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
    }

    // Возвращает тег в зависимости от прав игрока.
    private string GetTag(CCSPlayerController player)
    {
        if (AdminManager.PlayerHasPermissions(player, "@css/root")
            || AdminManager.PlayerHasPermissions(player, "@css/ban")
            || AdminManager.PlayerHasPermissions(player, "@css/kick")
            || AdminManager.PlayerHasPermissions(player, "@css/generic"))
        {
            return AdminTag;
        }

        if (AdminManager.PlayerHasPermissions(player, "@css/vip"))
        {
            return VipTag;
        }

        return "";
    }
}