using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace NoWarmupPlugin;

public class NoWarmupPlugin : BasePlugin
{
    public override string ModuleName => "No Warmup";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "poehali.dev";
    public override string ModuleDescription => "Убирает разминку на сервере CS2";

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventGameStart>(OnGameStart);
        
        Server.ExecuteCommand("mp_warmuptime 0");
        Server.ExecuteCommand("mp_do_warmup_period 0");
        
        Console.WriteLine($"[{ModuleName}] Плагин загружен! Разминка отключена.");
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Server.ExecuteCommand("mp_warmup_end");
        return HookResult.Continue;
    }

    private HookResult OnGameStart(EventGameStart @event, GameEventInfo info)
    {
        Server.ExecuteCommand("mp_warmuptime 0");
        Server.ExecuteCommand("mp_do_warmup_period 0");
        Server.ExecuteCommand("mp_warmup_end");
        return HookResult.Continue;
    }

    [ConsoleCommand("css_endwarmup", "Принудительно завершить разминку")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnEndWarmupCommand(CCSPlayerController? player, CommandInfo command)
    {
        Server.ExecuteCommand("mp_warmup_end");
        
        if (player != null && player.IsValid)
        {
            player.PrintToChat($" {ChatColors.Green}[NO WARMUP]{ChatColors.Default} Разминка завершена");
        }
        
        Server.PrintToChatAll($" {ChatColors.Green}[NO WARMUP]{ChatColors.Default} Разминка завершена");
    }

    public override void Unload(bool hotReload)
    {
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}
