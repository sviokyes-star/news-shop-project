using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace NoMapMusicPlugin;

public class NoMapMusicPlugin : BasePlugin
{
    public override string ModuleName => "No Map Music";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "poehali.dev";
    public override string ModuleDescription => "Отключает встроенную музыку карт для всех игроков";

    // Клиентские cvar-ы громкости музыки. Задаются каждому игроку,
    // потому что музыка карт проигрывается на стороне клиента.
    private static readonly string[] MuteCommands =
    {
        "snd_musicvolume 0",              // основная громкость музыки
        "snd_mapobjective_volume 0",      // музыка целей карты
        "snd_roundstart_volume 0",        // музыка начала раунда
        "snd_roundend_volume 0",          // музыка конца раунда
        "snd_deathcamera_volume 0",       // музыка камеры смерти
        "snd_mvp_volume 0",               // музыка MVP
        "snd_dzmusic_volume 0",           // музыка Danger Zone
        "snd_menumusic_volume 0"          // музыка меню
    };

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);

        // Если плагин загружен на живой сервер — заглушаем сразу всем.
        if (hotReload)
        {
            foreach (var player in Utilities.GetPlayers())
                MutePlayer(player);
        }

        Console.WriteLine($"[{ModuleName}] Плагин загружен! Музыка карт отключена.");
    }

    private void OnClientPutInServer(int slot)
    {
        var player = Utilities.GetPlayerFromSlot(slot);
        // Небольшая задержка — клиент должен полностью подключиться.
        AddTimer(1.0f, () => MutePlayer(player));
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // Повторно заглушаем на старте раунда — некоторые карты
        // перезапускают музыку по событиям раунда.
        foreach (var player in Utilities.GetPlayers())
            MutePlayer(player);

        return HookResult.Continue;
    }

    private void MutePlayer(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
            return;

        foreach (var cmd in MuteCommands)
            player.ExecuteClientCommandFromServer(cmd);
    }

    public override void Unload(bool hotReload)
    {
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}
