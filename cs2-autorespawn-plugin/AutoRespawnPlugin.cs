using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;

namespace AutoRespawnPlugin;

public class AutoRespawnPlugin : BasePlugin
{
    public override string ModuleName => "Okyes - Auto Respawn";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Автоматически оживляет игрока при заходе за команду из спектаторов/после перезахода";

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
    }

    private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
            return HookResult.Continue;

        int newTeam = @event.Team;
        if (newTeam != (int)CsTeam.Terrorist && newTeam != (int)CsTeam.CounterTerrorist)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            if (player == null || !player.IsValid)
                return;

            var pawn = player.PlayerPawn.Value;
            bool isAlive = pawn != null && pawn.IsValid && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE;
            if (!isAlive)
                player.Respawn();
        });

        return HookResult.Continue;
    }
}
