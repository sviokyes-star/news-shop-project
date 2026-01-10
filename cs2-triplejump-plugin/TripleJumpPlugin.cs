using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace TripleJumpPlugin;

public class TripleJumpPlugin : BasePlugin
{
    public override string ModuleName => "Triple Jump";
    public override string ModuleVersion => "1.0.4";
    public override string ModuleAuthor => "poehali.dev";
    public override string ModuleDescription => "Тройной прыжок для CS2";

    private readonly Dictionary<int, int> _jumpCount = new();
    private readonly Dictionary<int, bool> _wasOnGround = new();
    private readonly Dictionary<int, ulong> _lastJumpButton = new();

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnTick>(OnTick);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        
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
            
            bool isOnGround = (pawn.Flags & (uint)PlayerFlags.FL_ONGROUND) != 0;
            bool wasOnGround = _wasOnGround.ContainsKey(userId) && _wasOnGround[userId];
            
            // Сброс счетчика при приземлении или если игрок на земле
            if (isOnGround)
            {
                if (!wasOnGround || _jumpCount[userId] > 0)
                {
                    _jumpCount[userId] = 0;
                }
            }
            
            // Проверяем нажатие прыжка
            var buttons = player.Buttons;
            ulong currentButtons = (ulong)buttons;
            bool isJumping = (buttons & PlayerButtons.Jump) != 0;
            
            if (!_jumpCount.ContainsKey(userId))
                _jumpCount[userId] = 0;
            
            if (!_lastJumpButton.ContainsKey(userId))
                _lastJumpButton[userId] = 0;
            
            ulong lastButtons = _lastJumpButton[userId];
            bool wasJumpPressed = (lastButtons & (ulong)PlayerButtons.Jump) != 0;
            
            // Детектируем момент нажатия (переход от не нажата к нажата)
            bool justPressedJump = isJumping && !wasJumpPressed;
            
            if (justPressedJump)
            {
                // Первый прыжок - с земли
                if (isOnGround)
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
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}