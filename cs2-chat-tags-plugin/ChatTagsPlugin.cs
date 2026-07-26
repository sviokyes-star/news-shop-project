using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace ChatTagsPlugin;

public class ChatTagsPlugin : BasePlugin
{
    public override string ModuleName => "Chat Tags [Okyes]";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Префиксы [ADMIN] и [VIP] перед ником в чате";

    // Ярко-оранжевый цвет (≈ #FF4500) для тега [ADMIN].
    // В чате CS2 задаётся управляющим байтом \u0010 (Orange).
    private const char Orange = '\u0010';

    public override void Load(bool hotReload)
    {
        AddCommandListener("say", OnSay);
        AddCommandListener("say_team", OnSayTeam);

        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
    }

    private HookResult OnSay(CCSPlayerController? player, CommandInfo info)
    {
        return HandleChat(player, info, teamOnly: false);
    }

    private HookResult OnSayTeam(CCSPlayerController? player, CommandInfo info)
    {
        return HandleChat(player, info, teamOnly: true);
    }

    private HookResult HandleChat(CCSPlayerController? player, CommandInfo info, bool teamOnly)
    {
        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
            return HookResult.Continue;

        string message = info.GetArg(1).Trim();

        // Пустые сообщения и команды (!, /) пропускаем без изменений.
        if (string.IsNullOrEmpty(message) || message.StartsWith("!") || message.StartsWith("/"))
            return HookResult.Continue;

        string tag = GetTag(player);

        // Нет тега — оставляем стандартное сообщение движка.
        if (string.IsNullOrEmpty(tag))
            return HookResult.Continue;

        string teamPrefix = teamOnly ? $"{ChatColors.Grey}(Команда) " : "";
        string nameColor = TeamColor(player.Team);

        string formatted =
            $" {teamPrefix}{tag}{nameColor}{player.PlayerName}{ChatColors.Default}: {message}";

        // Кому показывать сообщение.
        foreach (var target in Utilities.GetPlayers())
        {
            if (target == null || !target.IsValid || target.IsBot || target.IsHLTV)
                continue;

            // Командный чат виден только своей команде (и мёртвым видно мёртвых —
            // упрощаем: команда должна совпадать).
            if (teamOnly && target.Team != player.Team)
                continue;

            target.PrintToChat(formatted);
        }

        // Гасим оригинальное сообщение, чтобы не было дубля.
        return HookResult.Handled;
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
