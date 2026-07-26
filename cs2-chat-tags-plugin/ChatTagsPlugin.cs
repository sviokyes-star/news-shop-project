using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;

namespace ChatTagsPlugin;

public class ChatTagsPlugin : BasePlugin
{
    public override string ModuleName => "Chat Tags [Okyes]";
    public override string ModuleVersion => "1.1.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Префиксы [ADMIN] и [VIP] перед ником в чате";

    // Ярко-оранжевый цвет (≈ #FF4500) для тега [ADMIN].
    // В чате CS2 задаётся управляющим байтом \u0010 (Orange).
    private const char Orange = '\u0010';

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerChat>(OnPlayerChat, HookMode.Post);

        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
    }

    private HookResult OnPlayerChat(EventPlayerChat @event, GameEventInfo info)
    {
        // В EventPlayerChat userid приходит как slot/handle, а не UserId.
        var player = Utilities.GetPlayerFromUserid(@event.Userid)
            ?? Utilities.GetPlayers().FirstOrDefault(p =>
                   p != null && p.IsValid && (int)p.Index - 1 == @event.Userid);

        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
            return HookResult.Continue;

        string message = @event.Text?.Trim() ?? "";

        // Пустые сообщения и команды (!, /) пропускаем.
        if (string.IsNullOrEmpty(message) || message.StartsWith("!") || message.StartsWith("/"))
            return HookResult.Continue;

        string tag = GetTag(player);
        if (string.IsNullOrEmpty(tag))
            return HookResult.Continue;

        bool teamOnly = @event.Teamonly;
        string teamPrefix = teamOnly ? $"{ChatColors.Grey}(Команда) " : "";
        string nameColor = TeamColor(player.Team);

        string formatted =
            $" {teamPrefix}{tag}{nameColor}{player.PlayerName}{ChatColors.Default}: {message}";

        foreach (var target in Utilities.GetPlayers())
        {
            if (target == null || !target.IsValid || target.IsBot || target.IsHLTV)
                continue;

            if (teamOnly && target.Team != player.Team)
                continue;

            target.PrintToChat(formatted);
        }

        return HookResult.Continue;
    }

    // Возвращает цветной тег в зависимости от прав игрока.
    private string GetTag(CCSPlayerController player)
    {
        if (AdminManager.PlayerHasPermissions(player, "@css/root")
            || AdminManager.PlayerHasPermissions(player, "@css/ban")
            || AdminManager.PlayerHasPermissions(player, "@css/kick")
            || AdminManager.PlayerHasPermissions(player, "@css/generic"))
        {
            return $"{Orange}[ADMIN] ";
        }

        if (AdminManager.PlayerHasPermissions(player, "@css/vip"))
        {
            return $"{ChatColors.Gold}[VIP] ";
        }

        return "";
    }

    private string TeamColor(CsTeam team)
    {
        return team switch
        {
            CsTeam.Terrorist => $"{ChatColors.Gold}",
            CsTeam.CounterTerrorist => $"{ChatColors.Blue}",
            _ => $"{ChatColors.Grey}",
        };
    }
}
