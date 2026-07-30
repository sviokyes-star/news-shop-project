using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using CS2MenuManager.API.Class;
using CS2MenuManager.API.Menu;

namespace RtvPlugin;

public class RtvPlugin : BasePlugin
{
    public override string ModuleName => "Rock The Vote";
    public override string ModuleVersion => "2.1.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Голосование за смену карты с выбором следующей карты";

    private const char Orange = ChatColors.Orange;
    private const string Prefix = " \x0e[RTV]\x01";

    // Доля игроков для запуска голосования (0.6 = 60%).
    private const double VotePercent = 0.6;

    // За сколько минут до конца карты автоматически запускать голосование.
    private const double AutoVoteBeforeEndMinutes = 2.0;

    private static readonly string[] DefaultMaps =
    {
        "de_dust2", "de_mirage", "de_inferno", "de_nuke",
        "de_ancient", "de_anubis", "de_vertigo", "de_train"
    };

    private readonly List<string> _maps = new();
    private readonly HashSet<int> _rtvVoters = new();
    private readonly Dictionary<string, string> _nominations = new();
    private bool _voteInProgress = false;

    // Время старта текущей карты (для автозапуска голосования).
    private DateTime _mapStartTime = DateTime.Now;

    private string MapsFilePath => Path.Combine(ModuleDirectory, "maps.txt");

    public override void Load(bool hotReload)
    {
        LoadMaps();

        AddCommand("css_rtv", "Голосовать за смену карты", CmdRtv);
        AddCommand("css_unrtv", "Отозвать голос за смену карты", CmdUnRtv);
        AddCommand("css_nominate", "Номинировать карту", CmdNominate);
        AddCommand("css_maps", "Список доступных карт", CmdMaps);
        AddCommand("css_rtv_status", "Статус голосования", CmdStatus);

        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);

        // Фиксируем старт карты для автозапуска голосования.
        _mapStartTime = DateTime.Now;
        RegisterListener<Listeners.OnMapStart>(OnMapStart);

        // Каждые 15 секунд проверяем, не пора ли автоматически запускать голосование.
        AddTimer(15.0f, CheckAutoVote, TimerFlags.REPEAT);

        Console.WriteLine($"[{ModuleName}] Плагин загружен! Карт в пуле: {_maps.Count}");
    }

    private void LoadMaps()
    {
        try
        {
            if (!File.Exists(MapsFilePath))
            {
                File.WriteAllText(MapsFilePath,
                    "# Список карт для RTV-голосования. Одна карта — одна строка.\n" +
                    "# Обычная карта:       de_dust2\n" +
                    "# Карта из Мастерской: workshop:3070463151\n" +
                    "# Со своим названием:  Моя карта|3070463151\n" +
                    string.Join("\n", DefaultMaps) + "\n");
                Console.WriteLine($"[{ModuleName}] Создан maps.txt со списком карт");
            }

            _maps.Clear();
            foreach (var line in File.ReadAllLines(MapsFilePath))
            {
                var s = line.Trim();
                if (s.Length == 0 || s.StartsWith("#"))
                    continue;
                _maps.Add(s);
            }

            if (_maps.Count == 0)
                _maps.AddRange(DefaultMaps);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка чтения maps.txt: {ex.Message}");
            _maps.Clear();
            _maps.AddRange(DefaultMaps);
        }
    }

    private (string display, string command) ParseMap(string entry)
    {
        if (entry.Contains('|'))
        {
            var parts = entry.Split('|', 2);
            return (parts[0].Trim(), $"host_workshop_map {parts[1].Trim()}");
        }
        if (entry.StartsWith("workshop:", StringComparison.OrdinalIgnoreCase))
        {
            string id = entry.Substring("workshop:".Length).Trim();
            return ($"Мастерская {id}", $"host_workshop_map {id}");
        }
        return (entry, $"changelevel {entry}");
    }

    private static int OnlinePlayers()
        => Utilities.GetPlayers().Count(p => p.IsValid && !p.IsBot && !p.IsHLTV);

    private int VotesNeeded()
        => Math.Max(2, (int)Math.Ceiling(OnlinePlayers() * VotePercent));

    private void CmdRtv(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        if (_voteInProgress)
        {
            player.PrintToChat($"{Prefix} Голосование за карту уже идёт!");
            return;
        }

        if (!_rtvVoters.Add(player.Slot))
        {
            player.PrintToChat($"{Prefix} Ты уже проголосовал за смену карты.");
            return;
        }

        int have = _rtvVoters.Count;
        int need = VotesNeeded();
        Server.PrintToChatAll($"{Prefix}{ChatColors.Green} {player.PlayerName}{ChatColors.Default} хочет сменить карту ({ChatColors.Yellow}{have}/{need}{ChatColors.Default})");

        if (have >= need)
            StartVote();
    }

    private void CmdUnRtv(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || _voteInProgress)
            return;

        if (_rtvVoters.Remove(player.Slot))
            Server.PrintToChatAll($"{Prefix}{ChatColors.Green} {player.PlayerName}{ChatColors.Default} отозвал голос ({ChatColors.Yellow}{_rtvVoters.Count}/{VotesNeeded()}{ChatColors.Default})");
    }

    private void CmdNominate(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        string arg = command.ArgString.Trim();
        if (string.IsNullOrEmpty(arg))
        {
            ShowNominateMenu(player);
            return;
        }

        var match = _maps.FirstOrDefault(m =>
            ParseMap(m).display.Contains(arg, StringComparison.OrdinalIgnoreCase) ||
            m.Contains(arg, StringComparison.OrdinalIgnoreCase));

        if (match == null)
        {
            player.PrintToChat($"{Prefix} Карта \"{arg}\" не найдена. Набери css_maps.");
            return;
        }

        Nominate(player, match);
    }

    private void ShowNominateMenu(CCSPlayerController player)
    {
        var menu = new WasdMenu("Номинировать карту", this);
        foreach (var entry in _maps)
        {
            var (display, _) = ParseMap(entry);
            menu.AddItem(display, (controller, option) => Nominate(controller, entry));
        }
        menu.Display(player, 0);
    }

    private void Nominate(CCSPlayerController player, string entry)
    {
        var (display, command) = ParseMap(entry);
        if (_nominations.ContainsKey(display))
        {
            player.PrintToChat($"{Prefix} Карта {display} уже номинирована.");
            return;
        }

        _nominations[display] = command;
        Server.PrintToChatAll($"{Prefix}{ChatColors.Green} {player.PlayerName}{ChatColors.Default} номинировал карту {ChatColors.Gold}{display}");
    }

    private void CmdMaps(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        player.PrintToChat($"{Prefix} Доступные карты:");
        foreach (var entry in _maps)
            player.PrintToChat($" {Orange}> {ChatColors.Default}{ParseMap(entry).display}");
    }

    private void CmdStatus(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        player.PrintToChat($"{Prefix} Голосов за RTV: {ChatColors.Yellow}{_rtvVoters.Count}/{VotesNeeded()}");
    }

    // Новая карта загружена — сбрасываем состояние.
    private void OnMapStart(string mapName)
    {
        _mapStartTime = DateTime.Now;
        _voteInProgress = false;
        _rtvVoters.Clear();
        _nominations.Clear();
    }

    // Автозапуск голосования за N минут до конца карты (по mp_timelimit).
    private void CheckAutoVote()
    {
        if (_voteInProgress)
            return;

        float timelimit = ConVar.Find("mp_timelimit")?.GetPrimitiveValue<float>() ?? 0f;
        if (timelimit <= 0f)
            return; // безлимитная карта — автозапуск не нужен

        double elapsedMinutes = (DateTime.Now - _mapStartTime).TotalMinutes;
        double remaining = timelimit - elapsedMinutes;

        if (remaining <= AutoVoteBeforeEndMinutes && remaining > 0)
        {
            Server.PrintToChatAll($"{Prefix}{ChatColors.Gold} Карта скоро закончится — автоматическое голосование за следующую карту!");
            StartVote();
        }
    }

    // Финальное голосование за следующую карту.
    private void StartVote()
    {
        _voteInProgress = true;
        Server.PrintToChatAll($"{Prefix}{ChatColors.Gold} Голосование за следующую карту началось! У вас 20 секунд.");

        var candidates = new List<string>();
        foreach (var kv in _nominations)
            if (!candidates.Contains(kv.Key))
                candidates.Add(kv.Key);

        var rnd = new Random();
        foreach (var entry in _maps.OrderBy(_ => rnd.Next()))
        {
            if (candidates.Count >= 5) break;
            var (display, _) = ParseMap(entry);
            if (!candidates.Contains(display))
                candidates.Add(display);
        }

        var votes = new Dictionary<string, int>();
        foreach (var c in candidates)
            votes[c] = 0;

        var voted = new HashSet<int>();

        foreach (var p in Utilities.GetPlayers())
        {
            if (!p.IsValid || p.IsBot || p.IsHLTV)
                continue;

            var menu = new WasdMenu("Голосование за карту", this);
            foreach (var c in candidates)
            {
                string display = c;
                menu.AddItem(display, (controller, option) =>
                {
                    if (voted.Add(controller.Slot))
                    {
                        votes[display]++;
                        controller.PrintToChat($"{Prefix} Твой голос за {ChatColors.Gold}{display}{ChatColors.Default} учтён.");
                    }
                });
            }
            menu.Display(p, 20);
        }

        AddTimer(20.0f, () => FinishVote(votes));
    }

    private void FinishVote(Dictionary<string, int> votes)
    {
        var winner = votes.OrderByDescending(v => v.Value).First();

        string command = _nominations.TryGetValue(winner.Key, out var cmd)
            ? cmd
            : _maps.Select(ParseMap).FirstOrDefault(m => m.display == winner.Key).command
              ?? $"changelevel {winner.Key}";

        Server.PrintToChatAll($"{Prefix}{ChatColors.Gold} Победила карта: {winner.Key}{ChatColors.Default} ({winner.Value} голосов)");
        Server.PrintToChatAll($"{Prefix} Смена карты через 5 секунд...");

        AddTimer(5.0f, () =>
        {
            Server.ExecuteCommand(command);
            _voteInProgress = false;
            _rtvVoters.Clear();
            _nominations.Clear();
        });
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player != null && player.IsValid)
            _rtvVoters.Remove(player.Slot);
        return HookResult.Continue;
    }

    public override void Unload(bool hotReload)
    {
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}