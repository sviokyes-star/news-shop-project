using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace TripleJumpPlugin;

public class TripleJumpPlugin : BasePlugin
{
    public override string ModuleName => "Triple Jump";
    public override string ModuleVersion => "1.6.2";
    public override string ModuleAuthor => "poehali.dev";
    public override string ModuleDescription => "Тройной прыжок для CS2";

    private bool _debug = false;
    private readonly Dictionary<int, int> _jumpCount = new();
    private readonly Dictionary<int, bool> _wasOnGround = new();
    private readonly Dictionary<int, ulong> _lastJumpButton = new();
    private readonly Dictionary<int, int> _groundTicks = new();
    private readonly Dictionary<int, float> _lastZVel = new();

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnTick>(OnTick);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        
        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
    }

    private void OnTick()
    {
        var players = Utilities.GetPlayers();
        foreach (var player in players)
        {
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null)
                continue;

            int userId = (int)player.UserId!;
            var pawn = player.PlayerPawn.Value;
            
            if (!_jumpCount.ContainsKey(userId))
                _jumpCount[userId] = 0;
            if (!_lastJumpButton.ContainsKey(userId))
                _lastJumpButton[userId] = 0;
            if (!_groundTicks.ContainsKey(userId))
                _groundTicks[userId] = 0;
            if (!_lastZVel.ContainsKey(userId))
                _lastZVel[userId] = 0f;

            bool isOnGround = (pawn.Flags & (uint)PlayerFlags.FL_ONGROUND) != 0;

            // Тики с момента последнего воздушного доп. прыжка (растёт каждый тик).
            _groundTicks[userId]++;

            // Детект фронта нажатия прыжка: сравниваем текущее состояние кнопок
            // с сохранённым за прошлый тик (_lastJumpButton).
            ulong curButtons = (ulong)player.Buttons;
            ulong oldButtons = _lastJumpButton[userId];

            bool isJumping = (curButtons & (ulong)PlayerButtons.Jump) != 0;
            bool wasJumping = (oldButtons & (ulong)PlayerButtons.Jump) != 0;
            bool justPressedJump = isJumping && !wasJumping;

            // Диагностика сырого состояния кнопки прыжка (только смена состояния),
            // чтобы понять, ловится ли отдельный клик в воздухе. Пишем В ЧАТ игроку.
            if (_debug && isJumping != wasJumping)
                player.PrintToChat($" {ChatColors.Grey}[TJ-BTN] jump={(isJumping ? "DOWN" : "up")} ground={isOnGround} count={_jumpCount[userId]} sinceAir={_groundTicks[userId]}");

            // Приземление сбрасывает серию — НО не сразу после доп. прыжка.
            // После воздушного прыжка игрок ещё летит вверх и FL_ONGROUND может
            // мелькнуть у самой земли; ждём небольшую паузу, чтобы такое мелькание
            // не сбило текущую серию. При bhop реальные приземления дальше по времени.
            const int CooldownAfterAirJump = 8;
            bool cooldownPassed = _groundTicks[userId] > CooldownAfterAirJump;
            if (isOnGround && cooldownPassed)
                _jumpCount[userId] = 0;

            if (_debug && justPressedJump)
                player.PrintToChat($" {ChatColors.Yellow}[TJ] JUMP ground={isOnGround} sinceAir={_groundTicks[userId]} count(before)={_jumpCount[userId]}");

            if (justPressedJump)
            {
                // Начало новой серии — только если серия ещё не идёт (count==0).
                // Это ловит и обычный прыжок с земли, и взлёт через bhop.
                // Как только count>=1, любой клик — воздушный доп. прыжок,
                // даже если FL_ONGROUND мелькнул (bhop у самой земли).
                if (_jumpCount[userId] == 0)
                {
                    _jumpCount[userId] = 1;
                }
                else if (_jumpCount[userId] < 3)
                {
                    // Воздушный доп. прыжок (2-й и 3-й).
                    _jumpCount[userId]++;
                    // Обнуляем счётчик тиков — запускаем cooldown, чтобы мелькание
                    // земли сразу после этого прыжка не сбросило серию.
                    _groundTicks[userId] = 0;

                    if (pawn.AbsVelocity != null)
                    {
                        pawn.Teleport(null, null, new Vector(
                            pawn.AbsVelocity.X,
                            pawn.AbsVelocity.Y,
                            301.993377f
                        ));
                    }

                    if (_debug)
                        player.PrintToChat($" {ChatColors.Green}[TJ] AIR JUMP -> count={_jumpCount[userId]}");
                }
            }

            _lastJumpButton[userId] = curButtons;
            _wasOnGround[userId] = isOnGround;
        }
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        int userId = (int)player.UserId!;
        _jumpCount[userId] = 0;
        _wasOnGround[userId] = true;
        _lastJumpButton[userId] = 0;
        _groundTicks[userId] = 0;
        _lastZVel[userId] = 0f;

        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null)
            return HookResult.Continue;

        int userId = (int)player.UserId!;
        _jumpCount.Remove(userId);
        _wasOnGround.Remove(userId);
        _lastJumpButton.Remove(userId);
        _groundTicks.Remove(userId);
        _lastZVel.Remove(userId);

        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // Сбрасываем все счетчики при старте раунда
        Console.WriteLine($"[TRIPLE JUMP] Round start - resetting all counters");
        foreach (var userId in _jumpCount.Keys.ToList())
        {
            _jumpCount[userId] = 0;
            _lastJumpButton[userId] = 0;
        }

        return HookResult.Continue;
    }

    [ConsoleCommand("css_tj_debug", "Диагностика тройного прыжка")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnTjDebug(CCSPlayerController? player, CommandInfo command)
    {
        _debug = !_debug;
        string state = _debug ? "ВКЛ" : "ВЫКЛ";
        command.ReplyToCommand($"[TJ] Диагностика {state}");
        if (player != null && player.IsValid)
            player.PrintToChat($" {ChatColors.Yellow}[TJ] Диагностика {state} — логи будут в чате");
    }

    [ConsoleCommand("css_triplejump", "Показать информацию о тройном прыжке")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnTripleJumpCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        player.PrintToChat($" {ChatColors.Green}[TRIPLE JUMP]{ChatColors.Default} Тройной прыжок активен!");
        player.PrintToChat($" {ChatColors.Yellow}Прыгайте до 3 раз подряд в воздухе");
    }

    public override void Unload(bool hotReload)
    {
        _jumpCount.Clear();
        _wasOnGround.Clear();
        _lastJumpButton.Clear();
        _groundTicks.Clear();
        _lastZVel.Clear();
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}