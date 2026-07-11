using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;

namespace InfiniteRoundPlugin;

public class InfiniteRoundPlugin : BasePlugin
{
    public override string ModuleName => "Infinite Round";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Убирает время раунда, делая его бесконечным";

    private bool _infiniteEnabled = true;
    private bool _cheatsEnabledByUs = false;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _guardTimer;

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart);

        AddTimer(1.0f, ApplyInfiniteRound);

        // Периодически подтверждаем настройки — некоторые серверы/плагины
        // могут сбрасывать sv_cheats или mp_ignore_round_win_conditions.
        _guardTimer = AddTimer(5.0f, () =>
        {
            if (_infiniteEnabled)
                ApplyInfiniteRound();
        }, TimerFlags.REPEAT);

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
        // mp_ignore_round_win_conditions защищена флагом FCVAR_CHEAT.
        // ВАЖНО: при выключении sv_cheats обратно в 0 движок игры автоматически
        // сбрасывает ВСЕ cheat-cvar на значения по умолчанию — поэтому sv_cheats
        // должен оставаться включённым всё время, пока активна эта функция.
        if (IsCheatsDisabled())
        {
            Server.ExecuteCommand("sv_cheats 1");
            _cheatsEnabledByUs = true;
        }

        // 60 — максимальное значение для mp_roundtime и связанных с ним cvar
        Server.ExecuteCommand("mp_roundtime 60");
        Server.ExecuteCommand("mp_roundtime_deployment 60");
        Server.ExecuteCommand("mp_roundtime_defuse 60");
        Server.ExecuteCommand("mp_roundtime_hostage 60");

        // Раунд не завершается по условиям победы (убийство всех, взрыв, обезвреживание, истечение времени)
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

        // Выключаем читы обратно, только если мы сами их включали для этой функции.
        if (_cheatsEnabledByUs)
        {
            Server.ExecuteCommand("sv_cheats 0");
            _cheatsEnabledByUs = false;
        }

        Console.WriteLine($"[{ModuleName}] Обычное время раунда восстановлено");
    }

    private bool IsCheatsDisabled()
    {
        var cvar = ConVar.Find("sv_cheats");
        return cvar == null || !cvar.GetPrimitiveValue<bool>();
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
        _guardTimer?.Kill();
        RestoreNormalRound();
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}