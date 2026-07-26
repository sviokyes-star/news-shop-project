using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;

namespace UnlimitedTeamPlugin;

public class UnlimitedTeamPlugin : BasePlugin
{
    public override string ModuleName => "Unlimited Team [Okyes]";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Снимает лимит на количество игроков в одной команде";

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart);

        ApplyCvars();

        // Периодически подтверждаем настройки — на случай, если карта или другой
        // плагин пытается вернуть баланс команд обратно.
        AddTimer(15.0f, ApplyCvars, TimerFlags.REPEAT);

        Console.WriteLine($"[{ModuleName}] Плагин загружен! Лимит команд снят.");
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        ApplyCvars();
        return HookResult.Continue;
    }

    private void ApplyCvars()
    {
        // Разница в размере команд не ограничена (0 = без лимита).
        Server.ExecuteCommand("mp_limitteams 0");
        // Автобаланс выключен — сервер не перекидывает игроков в другую команду.
        Server.ExecuteCommand("mp_autoteambalance 0");
    }

    public override void Unload(bool hotReload)
    {
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}