using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;

namespace InfiniteRoundPlugin;

public class InfiniteRoundPlugin : BasePlugin
{
    public override string ModuleName => "Infinite Round";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Убирает время раунда, делая его бесконечным";

    private bool _infiniteEnabled = true;

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart);

        AddTimer(1.0f, ApplyInfiniteRound);

        Console.WriteLine($"[{ModuleName}] Плагин загружен! Раунд теперь бесконечный.");
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (_infiniteEnabled)
        {
            ApplyInfiniteRound();
        }

        return HookResult.Continue;
    }

    private void ApplyInfiniteRound()
    {
        // 60 — максимальное значение для mp_roundtime и связанных с ним cvar
        Server.ExecuteCommand("mp_roundtime 60");
        Server.ExecuteCommand("mp_roundtime_deployment 60");
        Server.ExecuteCommand("mp_roundtime_defuse 60");
        Server.ExecuteCommand("mp_roundtime_hostage 60");

        // Раунд не завершается по условиям победы (убийство всех, взрыв, обезвреживание и т.д.)
        Server.ExecuteCommand("mp_ignore_round_win_conditions 1");

        Console.WriteLine($"[{ModuleName}] Таймер раунда снят, раунд бесконечный");
    }

    private void RestoreNormalRound()
    {
        Server.ExecuteCommand("mp_roundtime 1.92");
        Server.ExecuteCommand("mp_roundtime_deployment 1.92");
        Server.ExecuteCommand("mp_roundtime_defuse 1.92");
        Server.ExecuteCommand("mp_roundtime_hostage 1.92");
        Server.ExecuteCommand("mp_ignore_round_win_conditions 0");

        Console.WriteLine($"[{ModuleName}] Обычное время раунда восстановлено");
    }

    [ConsoleCommand("css_infinite", "Включить/выключить бесконечный раунд")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnInfiniteToggleCommand(CCSPlayerController? caller, CommandInfo command)
    {
        _infiniteEnabled = !_infiniteEnabled;

        if (_infiniteEnabled)
        {
            ApplyInfiniteRound();
        }
        else
        {
            RestoreNormalRound();
        }

        string status = _infiniteEnabled ? "включен" : "выключен";
        string message = $"Бесконечный раунд {status}";

        if (caller != null)
            caller.PrintToChat($" {ChatColors.Green}[Infinite Round]{ChatColors.Default} {message}");

        Console.WriteLine($"[Infinite Round] {message}");
        Server.PrintToChatAll($" {ChatColors.Green}[Infinite Round]{ChatColors.Default} {message}");
    }

    public override void Unload(bool hotReload)
    {
        RestoreNormalRound();
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}
