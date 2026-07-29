using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;

namespace NoMapMusicPlugin;

public class NoMapMusicPlugin : BasePlugin
{
    public override string ModuleName => "No Map Music";
    public override string ModuleVersion => "2.1.0";
    public override string ModuleAuthor => "poehali.dev";
    public override string ModuleDescription => "Отключает встроенную музыку карт (в т.ч. воркшоп) для всех игроков";

    // ID пользовательского сообщения о запуске звукового события (soundevent).
    // Через него воркшоп-карты проигрывают музыку/эмбиент.
    private const int SosStartSoundEvent = 208;

    // Ключевые слова в названиях музыкальных звуковых событий.
    // Блокируем только их — звуки ловушек/триггеров остаются.
    private static readonly string[] MusicKeywords =
    {
        "music", "song", "theme", "soundtrack", "ost",
        "bgm", "ambient_music", "background_music", "menu_music"
    };

    // Предвычисленные хеши музыкальных событий (по ключевым словам).
    private readonly HashSet<uint> _blockedHashes = new();

    // Клиентские cvar-ы громкости музыки — для стандартной музыки CS2.
    private static readonly string[] MuteCommands =
    {
        "snd_musicvolume 0",
        "snd_mapobjective_volume 0",
        "snd_roundstart_volume 0",
        "snd_roundend_volume 0",
        "snd_deathcamera_volume 0",
        "snd_mvp_volume 0",
        "snd_dzmusic_volume 0",
        "snd_menumusic_volume 0"
    };

    public override void Load(bool hotReload)
    {
        BuildBlockedHashes();
        LoadBlockedHashesFromFile();

        // Перехватываем звуковые события карт (музыка/эмбиент воркшоп-карт).
        HookUserMessage(SosStartSoundEvent, OnSoundEvent, HookMode.Pre);

        RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);

        if (hotReload)
            foreach (var player in Utilities.GetPlayers())
                MutePlayer(player);

        Console.WriteLine($"[{ModuleName}] Плагин загружен! Музыка карт отключена.");
    }

    // Режим диагностики — печатает хеши всех звуковых событий в консоль.
    private bool _debug = false;

    // Возможные названия поля с хешем звукового события в разных версиях CS2.
    private static readonly string[] HashFields =
    {
        "soundevent_hash", "soundevent_guid", "sound_event_hash", "hash"
    };

    // Блокирует запуск звукового события, если это музыка.
    private HookResult OnSoundEvent(UserMessage um)
    {
        try
        {
            uint hash = ReadHash(um);
            if (hash == 0)
                return HookResult.Continue;

            if (_debug)
                Console.WriteLine($"[{ModuleName}] SoundEvent hash = {hash}");

            if (_blockedHashes.Contains(hash))
                return HookResult.Stop; // заблокировано — музыка
        }
        catch
        {
            // Не мешаем игре при ошибке чтения.
        }

        return HookResult.Continue;
    }

    // Читает хеш события из первого доступного поля.
    private uint ReadHash(UserMessage um)
    {
        foreach (var field in HashFields)
        {
            if (um.HasField(field))
                return um.ReadUInt(field);
        }
        return 0;
    }

    private string BlockedHashesFile => Path.Combine(ModuleDirectory, "blocked_hashes.txt");

    private void LoadBlockedHashesFromFile()
    {
        if (!File.Exists(BlockedHashesFile))
            return;

        foreach (var line in File.ReadAllLines(BlockedHashesFile))
        {
            var s = line.Trim();
            if (uint.TryParse(s, out var h))
                _blockedHashes.Add(h);
        }
    }

    private void SaveBlockedHash(uint hash)
    {
        File.AppendAllText(BlockedHashesFile, hash + "\n");
    }

    // Собираем хеши музыкальных событий из типовых музыкальных имён.
    private void BuildBlockedHashes()
    {
        _blockedHashes.Clear();
        foreach (var kw in MusicKeywords)
        {
            _blockedHashes.Add(Fnv1a(kw));
            _blockedHashes.Add(Fnv1a(kw.ToLowerInvariant()));
        }
    }

    // FNV-1a 32-bit — тот же алгоритм хеширования имён звуковых событий в Source 2.
    private static uint Fnv1a(string text)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;
        uint hash = offset;
        foreach (char c in text)
        {
            hash ^= (byte)c;
            hash *= prime;
        }
        return hash;
    }

    private void OnClientPutInServer(int slot)
    {
        var player = Utilities.GetPlayerFromSlot(slot);
        AddTimer(1.0f, () => MutePlayer(player));
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
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

    // Включить/выключить вывод хешей звуковых событий в консоль сервера.
    [ConsoleCommand("css_music_debug", "Показывать хеши звуковых событий в консоли")]
    [RequiresPermissions("@css/root")]
    public void OnMusicDebug(CCSPlayerController? caller, CommandInfo command)
    {
        _debug = !_debug;
        string state = _debug ? "ВКЛючена" : "ВЫКЛючена";
        Console.WriteLine($"[{ModuleName}] Диагностика {state}. Найди хеш, который повторяется в такт музыке.");
        command.ReplyToCommand($"[No Map Music] Диагностика {state}. Смотри консоль сервера.");
    }

    // Заблокировать конкретный хеш музыки (узнать его можно через css_music_debug).
    [ConsoleCommand("css_music_block", "Заблокировать звуковое событие по хешу")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 1, usage: "<хеш>")]
    public void OnMusicBlock(CCSPlayerController? caller, CommandInfo command)
    {
        if (!uint.TryParse(command.GetArg(1), out var hash))
        {
            command.ReplyToCommand("[No Map Music] Неверный хеш");
            return;
        }

        if (_blockedHashes.Add(hash))
            SaveBlockedHash(hash);

        command.ReplyToCommand($"[No Map Music] Хеш {hash} заблокирован. Сменит карту — правило сохранится.");
        Console.WriteLine($"[{ModuleName}] Заблокирован хеш {hash}");
    }

    public override void Unload(bool hotReload)
    {
        UnhookUserMessage(SosStartSoundEvent, OnSoundEvent, HookMode.Pre);
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}