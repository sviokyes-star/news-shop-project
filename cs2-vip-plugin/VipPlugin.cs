using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CS2MenuManager.API.Class;
using CS2MenuManager.API.Enum;
using CS2MenuManager.API.Menu;

namespace VipPlugin;

public class VipPlugin : BasePlugin
{
    public override string ModuleName => "VIP [Okyes]";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "VIP-плагин с меню в стиле магазина";

    // Ярко-оранжевый цвет (≈ #FF4500) для префикса "Okyes |".
    // В чате CS2 задаётся управляющим байтом \u0010 (Orange).
    private const char Orange = '\u0010';

    // Флаг прав, который выдаётся VIP-игрокам.
    private const string VipFlag = "@css/vip";

    // Количество здоровья, выдаваемое VIP-игроку при спавне.
    private const int VipHealth = 110;

    public override void Load(bool hotReload)
    {
        AddCommand("css_vip", "Открыть VIP-меню", OnVipCommand);
        AddCommandListener("say", OnPlayerSay);
        AddCommandListener("say_team", OnPlayerSay);

        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);

        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
    }

    private bool IsVip(CCSPlayerController player)
    {
        return AdminManager.PlayerHasPermissions(player, VipFlag)
            || AdminManager.PlayerHasPermissions(player, "@css/root");
    }

    private HookResult OnPlayerSay(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        string message = info.GetArg(1).Trim();

        if (message.Equals("!vip", StringComparison.OrdinalIgnoreCase))
        {
            ShowMainMenu(player);
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }

    public void OnVipCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller == null || !caller.IsValid)
            return;

        ShowMainMenu(caller);
    }

    private void ShowMainMenu(CCSPlayerController player)
    {
        if (!IsVip(player))
        {
            player.PrintToChat($" {Orange}Okyes |{ChatColors.White} У тебя нет VIP-статуса");
            return;
        }

        var menu = new WasdMenu("VIP-меню", this);

        // Информационный пункт — некликабельный, только показывает бонус.
        menu.AddItem($"Здоровье: {VipHealth}", (_, _) => { }, DisableOption.DisableShowNumber);

        menu.Display(player, 0);
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || !IsVip(player))
            return HookResult.Continue;

        // Несколько попыток — pawn после спавна готов не сразу,
        // а другие плагины/режим могут выставить HP чуть позже.
        ApplyHealthDelayed(player, 0.1f);
        ApplyHealthDelayed(player, 0.5f);
        ApplyHealthDelayed(player, 1.0f);

        return HookResult.Continue;
    }

    private void ApplyHealthDelayed(CCSPlayerController player, float delay)
    {
        AddTimer(delay, () =>
        {
            if (player == null || !player.IsValid || !IsVip(player))
                return;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                return;

            SetHealth(pawn, VipHealth);
        });
    }

    private void SetHealth(CCSPlayerPawn pawn, int health)
    {
        pawn.MaxHealth = health;
        pawn.Health = health;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
    }
}