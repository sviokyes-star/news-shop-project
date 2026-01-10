using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

namespace BhopPlugin;

public class BhopPlugin : BasePlugin
{
    public override string ModuleName => "Bunny Hop";
    public override string ModuleVersion => "1.0.3";
    public override string ModuleAuthor => "poehali.dev";
    public override string ModuleDescription => "Автоматический банихоп для CS2";

    private readonly Dictionary<int, bool> _autobhopEnabled = new();

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnTick>(OnTick);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        
        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
    }

    [ConsoleCommand("css_bhop", "Включить/выключить автобанихоп")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnBhopCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.PlayerPawn.Value == null)
            return;

        int userId = (int)player.UserId!;
        
        if (!_autobhopEnabled.ContainsKey(userId))
            _autobhopEnabled[userId] = false;

        _autobhopEnabled[userId] = !_autobhopEnabled[userId];

        string status = _autobhopEnabled[userId] ? "включен" : "выключен";
        player.PrintToChat($" {ChatColors.Green}[BHOP]{ChatColors.Default} Автобанихоп {status}");
    }

    private void OnTick()
    {
        var players = Utilities.GetPlayers();
        foreach (var player in players)
        {
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null)
                continue;

            int userId = (int)player.UserId!;
            
            if (!_autobhopEnabled.ContainsKey(userId) || !_autobhopEnabled[userId])
                continue;

            var pawn = player.PlayerPawn.Value;
            
            // Проверяем, что игрок нажимает прыжок и находится на земле
            var buttons = player.Buttons;
            bool isJumping = (buttons & PlayerButtons.Jump) != 0;
            bool isOnGround = (pawn.Flags & (uint)PlayerFlags.FL_ONGROUND) != 0;

            if (isJumping && isOnGround)
            {
                // Автоматически выполняем прыжок
                pawn.Teleport(null, null, new Vector(pawn.AbsVelocity!.X, pawn.AbsVelocity.Y, 301.993377f));
            }
        }
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        int userId = (int)player.UserId!;
        
        // Включаем bhop по умолчанию при спавне
        if (!_autobhopEnabled.ContainsKey(userId))
            _autobhopEnabled[userId] = true;

        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null)
            return HookResult.Continue;

        int userId = (int)player.UserId!;
        _autobhopEnabled.Remove(userId);

        return HookResult.Continue;
    }

    public override void Unload(bool hotReload)
    {
        _autobhopEnabled.Clear();
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}