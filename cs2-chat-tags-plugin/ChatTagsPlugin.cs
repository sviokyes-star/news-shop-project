using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;

namespace ChatTagsPlugin;

public class ChatTagsPlugin : BasePlugin
{
    public override string ModuleName => "Chat Tags [Okyes]";
    public override string ModuleVersion => "3.0.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Теги [ADMIN] и [VIP] перед ником (чат + таблица)";

    // Ярко-оранжевый цвет (≈ #FF4500) для тега [ADMIN] в чате.
    // В чате CS2 задаётся управляющим байтом \u0010 (Orange).
    private const char Orange = '\u0010';

    private const string AdminTag = "[ADMIN]";
    private const string VipTag = "[VIP]";

    public override void Load(bool hotReload)
    {
        // Клан-тег — для таблицы (Tab).
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
        RegisterListener<Listeners.OnClientAuthorized>(OnClientAuthorized);

        // Перехват чата — для тега перед ником в сообщениях.
        AddCommandListener("say", OnSay);
        AddCommandListener("say_team", OnSayTeam);

        if (hotReload)
            foreach (var p in Utilities.GetPlayers())
                ApplyClanTag(p);

        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
    }

    // ===== Чат-тег перед ником =====

    private HookResult OnSay(CCSPlayerController? player, CommandInfo info)
        => HandleChat(player, info, teamOnly: false);

    private HookResult OnSayTeam(CCSPlayerController? player, CommandInfo info)
        => HandleChat(player, info, teamOnly: true);

    private HookResult HandleChat(CCSPlayerController? player, CommandInfo info, bool teamOnly)
    {
        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
            return HookResult.Continue;

        string message = info.GetArg(1).Trim();

        // Пустое, команды (!, /) — не трогаем.
        if (string.IsNullOrEmpty(message) || message.StartsWith("!") || message.StartsWith("/"))
            return HookResult.Continue;

        string tag = GetColoredTag(player);
        if (string.IsNullOrEmpty(tag))
            return HookResult.Continue;

        string teamPrefix = teamOnly ? $"{ChatColors.Grey}(Команда) " : "";
        string nameColor = TeamColor(player.Team);

        string formatted =
            $" {teamPrefix}{tag}{nameColor}{player.PlayerName}{ChatColors.Default}: {message}";

        bool alive = IsAlive(player);

        foreach (var target in Utilities.GetPlayers())
        {
            if (target == null || !target.IsValid || target.IsBot || target.IsHLTV)
                continue;

            if (teamOnly)
            {
                // Командный чат — только своя команда.
                if (target.Team != player.Team)
                    continue;
                // Живой пишет живым, мёртвый — мёртвым (как в игре).
                if (IsAlive(target) != alive)
                    continue;
            }

            target.PrintToChat(formatted);
        }

        // Гасим оригинальное сообщение движка, чтобы не было дубля.
        return HookResult.Handled;
    }

    private bool IsAlive(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        return pawn != null && pawn.IsValid
            && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE;
    }

    private string TeamColor(CsTeam team) => team switch
    {
        CsTeam.Terrorist => $"{ChatColors.Gold}",
        CsTeam.CounterTerrorist => $"{ChatColors.Blue}",
        _ => $"{ChatColors.Grey}",
    };

    // ===== Клан-тег для таблицы =====

    private void OnClientAuthorized(int slot, SteamID id)
    {
        var player = Utilities.GetPlayerFromSlot(slot);
        AddTimer(1.0f, () => ApplyClanTag(player));
        AddTimer(3.0f, () => ApplyClanTag(player));
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        ApplyClanTag(@event.Userid);
        return HookResult.Continue;
    }

    private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;
        AddTimer(0.2f, () => ApplyClanTag(player));
        return HookResult.Continue;
    }

    private void ApplyClanTag(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
            return;

        string tag = GetPlainTag(player);
        if (string.IsNullOrEmpty(tag) || player.Clan == tag)
            return;

        player.Clan = tag;
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
    }

    // ===== Определение тега по правам =====

    private bool IsAdmin(CCSPlayerController player)
        => AdminManager.PlayerHasPermissions(player, "@css/root")
        || AdminManager.PlayerHasPermissions(player, "@css/ban")
        || AdminManager.PlayerHasPermissions(player, "@css/kick")
        || AdminManager.PlayerHasPermissions(player, "@css/generic");

    private bool IsVip(CCSPlayerController player)
        => AdminManager.PlayerHasPermissions(player, "@css/vip");

    // Тег без цвета (для клан-тега в таблице).
    private string GetPlainTag(CCSPlayerController player)
    {
        if (IsAdmin(player)) return AdminTag;
        if (IsVip(player)) return VipTag;
        return "";
    }

    // Тег с цветом (для чата).
    private string GetColoredTag(CCSPlayerController player)
    {
        if (IsAdmin(player)) return $"{Orange}{AdminTag} ";
        if (IsVip(player)) return $"{ChatColors.Gold}{VipTag} ";
        return "";
    }
}
