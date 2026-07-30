using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace TripleJumpPlugin;

public class TripleJumpPlugin : BasePlugin
{
    public override string ModuleName => "Triple Jump";
    public override string ModuleVersion => "1.9.1";
    public override string ModuleAuthor => "poehali.dev";
    public override string ModuleDescription => "Тройной прыжок для CS2";

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

            // Детект фронта нажатия прыжка: сравниваем текущее состояние кнопок
            // с сохранённым за прошлый тик (_lastJumpButton).
            ulong curButtons = (ulong)player.Buttons;
            ulong oldButtons = _lastJumpButton[userId];

            bool isJumping = (curButtons & (ulong)PlayerButtons.Jump) != 0;
            bool wasJumping = (oldButtons & (ulong)PlayerButtons.Jump) != 0;
            bool justPressedJump = isJumping && !wasJumping;

            // _jumpCount здесь = число СДЕЛАННЫХ воздушных доп. прыжков в текущем
            // полёте. Взлётный прыжок НЕ считаем — иначе доступных остаётся мало
            // ("двойной вместо тройного"). Лимит доп. прыжков в воздухе = 3.
            const int MaxAirJumps = 3;

            // Отрыв от земли или нахождение на земле — новый полёт: обнуляем счётчик.
            bool tookOff = wasOnGround && !isOnGround;
            if (tookOff || isOnGround)
                _jumpCount[userId] = 0;

            // ДИАГНОСТИКА в чат (смена состояния кнопки), чтобы видеть касания земли.
            if (isJumping != wasJumping)
                player.PrintToChat($" {ChatColors.Grey}[TJ] jump={(isJumping ? "DOWN" : "up")} ground={isOnGround} wasGround={wasOnGround} tookOff={tookOff} air={_jumpCount[userId]}");

            if (justPressedJump && !isOnGround)
            {
                if (_jumpCount[userId] < MaxAirJumps)
                {
                    // Воздушный доп. прыжок.
                    _jumpCount[userId]++;

                    if (pawn.AbsVelocity != null)
                    {
                        pawn.Teleport(null, null, new Vector(
                            pawn.AbsVelocity.X,
                            pawn.AbsVelocity.Y,
                            301.993377f
                        ));
                    }

                    player.PrintToChat($" {ChatColors.Green}[TJ] AIR JUMP -> {_jumpCount[userId]}/{MaxAirJumps}");
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