using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;

namespace RtvPlugin;

public class RtvPlugin : BasePlugin
{
    public override string ModuleName => "Rock The Vote";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Голосование за смену карты в CS2";

    private readonly HashSet<ulong> _votedPlayers = new();
    private int _requiredVotes = 0;
    private float _votePercentage = 0.6f;
    private bool _voteInProgress = false;
    private int _mapChangeCountdown = 0;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _countdownTimer;

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        
        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
        Console.WriteLine($"[{ModuleName}] Требуется {(_votePercentage * 100)}% голосов для смены карты");
    }

    [ConsoleCommand("css_rtv", "Голосовать за смену карты")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnRtvCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        if (_voteInProgress)
        {
            player.PrintToChat($" {ChatColors.Green}[RTV]{ChatColors.Default} Голосование уже идёт!");
            return;
        }

        ulong steamId = player.SteamID;

        if (_votedPlayers.Contains(steamId))
        {
            player.PrintToChat($" {ChatColors.Green}[RTV]{ChatColors.Default} Вы уже проголосовали!");
            return;
        }

        _votedPlayers.Add(steamId);
        UpdateRequiredVotes();

        int currentVotes = _votedPlayers.Count;
        
        Server.PrintToChatAll($" {ChatColors.Green}[RTV]{ChatColors.Default} {player.PlayerName} хочет сменить карту ({ChatColors.Yellow}{currentVotes}/{_requiredVotes}{ChatColors.Default})");

        if (currentVotes >= _requiredVotes)
        {
            StartVote();
        }
    }

    [ConsoleCommand("css_rtv_status", "Статус голосования")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnRtvStatusCommand(CCSPlayerController? caller, CommandInfo command)
    {
        int currentVotes = _votedPlayers.Count;
        UpdateRequiredVotes();

        if (caller != null)
        {
            caller.PrintToChat($" {ChatColors.Green}[RTV]{ChatColors.Default} Голосов: {ChatColors.Yellow}{currentVotes}/{_requiredVotes}");
            if (_votedPlayers.Contains(caller.SteamID))
            {
                caller.PrintToChat($" {ChatColors.Green}[RTV]{ChatColors.Default} Вы уже проголосовали");
            }
        }
        else
        {
            Console.WriteLine($"[RTV] Голосов: {currentVotes}/{_requiredVotes}");
        }
    }

    [ConsoleCommand("css_rtv_cancel", "Отменить голосование")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnRtvCancelCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ulong steamId = player.SteamID;

        if (!_votedPlayers.Contains(steamId))
        {
            player.PrintToChat($" {ChatColors.Green}[RTV]{ChatColors.Default} Вы не голосовали!");
            return;
        }

        _votedPlayers.Remove(steamId);
        UpdateRequiredVotes();

        int currentVotes = _votedPlayers.Count;
        
        player.PrintToChat($" {ChatColors.Green}[RTV]{ChatColors.Default} Голос отменён");
        Server.PrintToChatAll($" {ChatColors.Green}[RTV]{ChatColors.Default} {player.PlayerName} отменил голос ({ChatColors.Yellow}{currentVotes}/{_requiredVotes}{ChatColors.Default})");
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        _votedPlayers.Remove(player.SteamID);
        UpdateRequiredVotes();

        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (!_voteInProgress)
        {
            _votedPlayers.Clear();
        }

        return HookResult.Continue;
    }

    private void UpdateRequiredVotes()
    {
        var players = Utilities.GetPlayers().Where(p => p?.IsValid == true && !p.IsBot).ToList();
        int totalPlayers = players.Count;
        
        _requiredVotes = (int)Math.Ceiling(totalPlayers * _votePercentage);
        
        if (_requiredVotes < 2)
            _requiredVotes = 2;
    }

    private void StartVote()
    {
        _voteInProgress = true;
        _mapChangeCountdown = 10;

        Server.PrintToChatAll($" {ChatColors.Green}[RTV]{ChatColors.Default} {ChatColors.Red}Голосование принято! Смена карты через 10 секунд...");
        Console.WriteLine("[RTV] Голосование принято, смена карты");

        _countdownTimer = AddTimer(1.0f, () =>
        {
            if (_mapChangeCountdown > 0)
            {
                if (_mapChangeCountdown <= 5)
                {
                    Server.PrintToChatAll($" {ChatColors.Green}[RTV]{ChatColors.Default} {ChatColors.Red}{_mapChangeCountdown}...");
                }
                _mapChangeCountdown--;
            }
            else
            {
                _countdownTimer?.Kill();
                ChangeMap();
            }
        }, TimerFlags.REPEAT);
    }

    private void ChangeMap()
    {
        Server.PrintToChatAll($" {ChatColors.Green}[RTV]{ChatColors.Default} {ChatColors.Red}Смена карты...");
        Console.WriteLine("[RTV] Принудительная смена карты");
        
        AddTimer(1.0f, () =>
        {
            Server.ExecuteCommand("mp_timelimit 0.1");
        });
    }

    public override void Unload(bool hotReload)
    {
        _countdownTimer?.Kill();
        _votedPlayers.Clear();
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}
