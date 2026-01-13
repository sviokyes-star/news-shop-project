using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Timers;

namespace MapTimeLimitPlugin;

public class MapTimeLimitPlugin : BasePlugin
{
    public override string ModuleName => "Map Time Limit";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Устанавливает время карты на 30 минут в CS2";

    private int _timeLimitMinutes = 30;
    private float _mapStartTime = 0f;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _checkTimer;
    private bool _warningShown = false;

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventCsIntermission>(OnMapEnd);
        
        AddTimer(5.0f, () =>
        {
            _mapStartTime = Server.CurrentTime;
            SetMapTimeLimit(_timeLimitMinutes);
            _checkTimer = AddTimer(10.0f, CheckTimeRemaining, TimerFlags.REPEAT);
        });
        
        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
        Console.WriteLine($"[{ModuleName}] Время карты: {_timeLimitMinutes} минут");
    }

    [ConsoleCommand("css_timelimit", "Установить время карты")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 1, usage: "<минуты>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnTimeLimitCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (!int.TryParse(command.GetArg(1), out int minutes) || minutes < 0)
        {
            if (caller != null)
                caller.PrintToChat($" {ChatColors.Green}[Map Time]{ChatColors.Default} Неверное значение (0 и больше)");
            else
                Console.WriteLine("[Map Time] Неверное значение времени");
            return;
        }

        _timeLimitMinutes = minutes;
        SetMapTimeLimit(minutes);
        _warningShown = false;

        string message = minutes == 0 
            ? "Лимит времени карты отключен" 
            : $"Лимит времени карты: {minutes} минут";

        if (caller != null)
            caller.PrintToChat($" {ChatColors.Green}[Map Time]{ChatColors.Default} {message}");
        
        Console.WriteLine($"[Map Time] {message}");
        Server.PrintToChatAll($" {ChatColors.Green}[Map Time]{ChatColors.Default} {message}");
    }

    [ConsoleCommand("css_timeleft", "Узнать оставшееся время карты")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnTimeLeftCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (_timeLimitMinutes == 0)
        {
            if (caller != null)
                caller.PrintToChat($" {ChatColors.Green}[Map Time]{ChatColors.Default} Лимит времени отключен");
            else
                Console.WriteLine("[Map Time] Лимит времени отключен");
            return;
        }

        float elapsedSeconds = Server.CurrentTime - _mapStartTime;
        float totalSeconds = _timeLimitMinutes * 60f;
        float remainingSeconds = totalSeconds - elapsedSeconds;

        if (remainingSeconds < 0)
            remainingSeconds = 0;

        int minutes = (int)(remainingSeconds / 60);
        int seconds = (int)(remainingSeconds % 60);

        if (caller != null)
        {
            caller.PrintToChat($" {ChatColors.Green}[Map Time]{ChatColors.Default} Осталось времени: {ChatColors.Yellow}{minutes:D2}:{seconds:D2}");
        }
        else
        {
            Console.WriteLine($"[Map Time] Осталось времени: {minutes:D2}:{seconds:D2}");
        }
    }

    [ConsoleCommand("css_extendmap", "Продлить время карты")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 1, usage: "<минуты>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnExtendMapCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (!int.TryParse(command.GetArg(1), out int minutes) || minutes <= 0)
        {
            if (caller != null)
                caller.PrintToChat($" {ChatColors.Green}[Map Time]{ChatColors.Default} Неверное значение (больше 0)");
            return;
        }

        _timeLimitMinutes += minutes;
        SetMapTimeLimit(_timeLimitMinutes);
        _warningShown = false;

        if (caller != null)
            caller.PrintToChat($" {ChatColors.Green}[Map Time]{ChatColors.Default} Карта продлена на {minutes} минут");
        
        Console.WriteLine($"[Map Time] Карта продлена на {minutes} минут (всего: {_timeLimitMinutes})");
        Server.PrintToChatAll($" {ChatColors.Green}[Map Time]{ChatColors.Default} Карта продлена на {ChatColors.Yellow}{minutes} минут");
    }

    [ConsoleCommand("css_nextmap", "Сменить карту")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 0, usage: "[название карты]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnNextMapCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (command.ArgCount > 1)
        {
            string mapName = command.GetArg(1);
            Server.ExecuteCommand($"changelevel {mapName}");
            
            Console.WriteLine($"[Map Time] Смена карты на {mapName}");
        }
        else
        {
            Server.ExecuteCommand("mp_timelimit 0.1");
            
            if (caller != null)
                caller.PrintToChat($" {ChatColors.Green}[Map Time]{ChatColors.Default} Смена карты через 5 секунд...");
            
            Console.WriteLine("[Map Time] Принудительная смена карты");
            Server.PrintToChatAll($" {ChatColors.Green}[Map Time]{ChatColors.Default} Смена карты через 5 секунд...");
        }
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (_mapStartTime == 0f)
        {
            _mapStartTime = Server.CurrentTime;
            _warningShown = false;
        }

        return HookResult.Continue;
    }

    private HookResult OnMapEnd(EventCsIntermission @event, GameEventInfo info)
    {
        return HookResult.Continue;
    }

    private void CheckTimeRemaining()
    {
        if (_timeLimitMinutes == 0 || _mapStartTime == 0f)
            return;

        try
        {
            float elapsedSeconds = Server.CurrentTime - _mapStartTime;
            float totalSeconds = _timeLimitMinutes * 60f;
            float remainingSeconds = totalSeconds - elapsedSeconds;

            if (remainingSeconds <= 60 && !_warningShown)
            {
                _warningShown = true;
                Server.PrintToChatAll($" {ChatColors.Green}[Map Time]{ChatColors.Default} {ChatColors.Red}Осталась 1 минута до смены карты!");
            }
            else if (remainingSeconds <= 300 && remainingSeconds > 240 && !_warningShown)
            {
                Server.PrintToChatAll($" {ChatColors.Green}[Map Time]{ChatColors.Default} {ChatColors.Yellow}Осталось 5 минут до смены карты!");
            }

            if (remainingSeconds <= 0)
            {
                Server.PrintToChatAll($" {ChatColors.Green}[Map Time]{ChatColors.Default} {ChatColors.Red}Время истекло! Смена карты...");
                Console.WriteLine("[Map Time] Лимит времени достигнут, смена карты");
                
                AddTimer(3.0f, () =>
                {
                    Server.ExecuteCommand("mp_timelimit 0.1");
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Map Time] Ошибка в CheckTimeRemaining: {ex.Message}");
        }
    }

    private void SetMapTimeLimit(int minutes)
    {
        try
        {
            Server.ExecuteCommand($"mp_timelimit {minutes}");
            Console.WriteLine($"[Map Time] mp_timelimit установлен на {minutes} минут");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Map Time] Ошибка установки mp_timelimit: {ex.Message}");
        }
    }

    public override void Unload(bool hotReload)
    {
        _checkTimer?.Kill();
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}