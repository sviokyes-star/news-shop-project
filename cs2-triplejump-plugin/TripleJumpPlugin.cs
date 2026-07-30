using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace TripleJumpPlugin;

public class TripleJumpPlugin : BasePlugin
{
    public override string ModuleName => "Triple Jump";
    public override string ModuleVersion => "1.3.0";
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
            bool wasOnGround = _wasOnGround.ContainsKey(userId) && _wasOnGround[userId];

            // Считаем тики, прошедшие с последнего реального касания земли.
            // При касании — сбрасываем в 0, иначе увеличиваем.
            // При банихопе FL_ONGROUND держится всего 1 тик, поэтому опираемся
            // не на мгновенный флаг, а на окно из нескольких последних тиков.
            if (isOnGround)
                _groundTicks[userId] = 0;
            else
                _groundTicks[userId]++;

            // "Недавно на земле" — реальный флаг был в пределах окна тиков.
            // Это переживает мелькание флага при bhop, но в чистом воздухе
            // (серия прыжков без касания земли) окно закрывается — бесконечных нет.
            const int GroundWindowTicks = 4;
            bool touchedGround = isOnGround || wasOnGround || _groundTicks[userId] <= GroundWindowTicks;

            // Любое касание земли сбрасывает серию прыжков.
            if (touchedGround && _jumpCount[userId] != 0)
            {
                _jumpCount[userId] = 0;
                _lastJumpButton[userId] = 0;
            }

            // Проверяем нажатие прыжка
            var buttons = player.Buttons;
            ulong currentButtons = (ulong)buttons;
            bool isJumping = (buttons & PlayerButtons.Jump) != 0;

            ulong lastButtons = _lastJumpButton[userId];
            bool wasJumpPressed = (lastButtons & (ulong)PlayerButtons.Jump) != 0;

            // Детектируем момент нажатия (переход от не нажата к нажата)
            bool justPressedJump = isJumping && !wasJumpPressed;

            if (justPressedJump)
            {
                if (_debug)
                    Console.WriteLine($"[TJ] {player.PlayerName} JUMP: onGround={isOnGround} wasGround={wasOnGround} groundTicks={_groundTicks[userId]} touched={touchedGround} count(before)={_jumpCount[userId]}");

                // Прыжок с земли (или из тика касания земли) — начинаем новую серию.
                // Это надёжно работает при банихопе, где FL_ONGROUND держится 1 тик.
                if (touchedGround)
                {
                    _jumpCount[userId] = 1;
                }
                // Второй и третий прыжок - в воздухе
                else if (_jumpCount[userId] >= 1 && _jumpCount[userId] < 3)
                {
                    _jumpCount[userId]++;

                    // Выполняем прыжок
                    if (pawn.AbsVelocity != null)
                    {
                        pawn.Teleport(null, null, new Vector(
                            pawn.AbsVelocity.X,
                            pawn.AbsVelocity.Y,
                            301.993377f
                        ));
                    }
                }
                // Лимит исчерпан (3 прыжка) — больше не прыгаем в воздухе.
                // Сброс произойдёт только при следующем касании земли.
            }

            _lastJumpButton[userId] = currentButtons;
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
        Console.WriteLine($"[TJ] Диагностика {( _debug ? "ВКЛ" : "ВЫКЛ")}");
        command.ReplyToCommand($"[TJ] Диагностика {( _debug ? "ВКЛ" : "ВЫКЛ")}");
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