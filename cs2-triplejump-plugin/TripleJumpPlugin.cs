using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace TripleJumpPlugin;

public class TripleJumpConfig : BasePluginConfig
{
    // Сколько ДОПОЛНИТЕЛЬНЫХ прыжков в воздухе доступно (не считая взлётный).
    // 2 = тройной прыжок (взлёт + 2), 1 = двойной, 3 = четверной и т.д.
    [JsonPropertyName("AirJumps")]
    public int AirJumps { get; set; } = 2;

    // Сила прыжка — вертикальная скорость, которая задаётся при доп. прыжке.
    [JsonPropertyName("JumpForce")]
    public float JumpForce { get; set; } = 301.993377f;

    // Порог скорости отскока вверх для детекта банихопа (для карт без флага земли).
    // Больше значение — реже ложные срабатывания, но нужен более резкий прыжок.
    [JsonPropertyName("BhopTakeoffVelocity")]
    public float BhopTakeoffVelocity { get; set; } = 200f;
}

public class TripleJumpPlugin : BasePlugin, IPluginConfig<TripleJumpConfig>
{
    public override string ModuleName => "Triple Jump";
    public override string ModuleVersion => "2.3.4";
    public override string ModuleAuthor => "poehali.dev";
    public override string ModuleDescription => "Тройной прыжок для CS2";

    public TripleJumpConfig Config { get; set; } = new();

    public void OnConfigParsed(TripleJumpConfig config)
    {
        Config = config;
    }

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

            // === Точная модель рабочего DoubleJumpCS2 (декомпилировано из .dll) ===
            // Флаги и кнопки текущего тика + сохранённые за прошлый тик.
            uint curFlags = pawn.Flags;
            uint prevFlags = (uint)_groundTicks[userId];         // хранит PrevFlags
            ulong curButtons = (ulong)player.Buttons;
            ulong prevButtons = _lastJumpButton[userId];

            const uint FL_ONGROUND = (uint)PlayerFlags.FL_ONGROUND;
            int maxAirJumps = Config.AirJumps;   // из конфига (2 = тройной прыжок)

            // Земля определяется по ОКНУ из двух тиков: текущий ИЛИ предыдущий флаг.
            bool onGroundNow = (curFlags & FL_ONGROUND) != 0;
            bool onGroundPrev = (prevFlags & FL_ONGROUND) != 0;
            bool onGround = onGroundNow || onGroundPrev;

            // Z-скорость сейчас и на прошлом тике.
            float zVel = pawn.AbsVelocity?.Z ?? 0f;
            float prevZVel = _lastZVel[userId];

            // BHOP-ОТТАЛКИВАНИЕ: игрок падал вниз (Z < 0), а на этом тике резко полетел
            // вверх (Z заметно > 0). На mg-картах флаг земли при bhop не выставляется,
            // поэтому это единственный надёжный признак нового прыжка с земли.
            bool bhopTakeoff = prevZVel < 0f && zVel > Config.BhopTakeoffVelocity;

            // Касание земли ИЛИ bhop-отталкивание обнуляет счётчик доп. прыжков —
            // значит серия доп. прыжков доступна на КАЖДОМ прыжке банихопа.
            if (onGround || bhopTakeoff)
                _jumpCount[userId] = 0;

            // Фронт нажатия прыжка (бит IN_JUMP): нажат сейчас, не был нажат в прошлом.
            bool jumpNow = (curButtons & (ulong)PlayerButtons.Jump) != 0;
            bool jumpPrev = (prevButtons & (ulong)PlayerButtons.Jump) != 0;
            bool justPressedJump = jumpNow && !jumpPrev;

            // Доп. прыжок: в воздухе и в пределах лимита.
            if (justPressedJump && !onGround && _jumpCount[userId] < maxAirJumps)
            {
                _jumpCount[userId]++;

                if (pawn.AbsVelocity != null)
                {
                    pawn.Teleport(null, null, new Vector(
                        pawn.AbsVelocity.X,
                        pawn.AbsVelocity.Y,
                        Config.JumpForce
                    ));
                }
            }

            // Сохраняем состояние для следующего тика.
            // Z берём АКТУАЛЬНУЮ (после возможного нашего прыжка Z=302), чтобы наш
            // же прыжок не распознался как bhop-отталкивание на следующем тике.
            _lastJumpButton[userId] = curButtons;
            _groundTicks[userId] = (int)curFlags;   // станет PrevFlags
            _lastZVel[userId] = pawn.AbsVelocity?.Z ?? zVel;
            _wasOnGround[userId] = onGroundNow;
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

        int total = Config.AirJumps + 1;
        player.PrintToChat($" {ChatColors.Green}[TRIPLE JUMP]{ChatColors.Default} Мультипрыжок активен!");
        player.PrintToChat($" {ChatColors.Yellow}Доступно прыжков подряд: {total}");
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