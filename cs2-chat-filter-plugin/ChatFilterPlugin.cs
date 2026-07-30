using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.UserMessages;

namespace ChatFilterPlugin;

public class ChatFilterPlugin : BasePlugin
{
    public override string ModuleName => "Chat Filter [Okyes]";
    public override string ModuleVersion => "1.1.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Убирает служебные сообщения карт из чата";

    // ID сообщений, через которые в чат/HUD попадает текст.
    // Разные карты используют разные каналы, поэтому хукаем несколько.
    private static readonly int[] TextMessageIds = { 117, 118, 124 };

    // Режим диагностики — печатает ID и текст сообщений в консоль сервера.
    private bool _debug = false;

    // Подстроки, при наличии которых сообщение из чата убирается.
    // Регистр не важен. Настраивается в файле filters.txt.
    private static readonly string[] DefaultFilters =
    {
        "access granted",
        "access denied",
        "football",
        "Console:",
        "[MG]",
        "trap activated",
        "trap deactivated"
    };

    private readonly List<string> _filters = new();

    private string FiltersFilePath => Path.Combine(ModuleDirectory, "filters.txt");

    public override void Load(bool hotReload)
    {
        LoadFilters();

        foreach (var id in TextMessageIds)
            HookUserMessage(id, OnSayText, HookMode.Pre);

        Console.WriteLine($"[{ModuleName}] Плагин загружен! Фильтров: {_filters.Count}");
    }

    private void LoadFilters()
    {
        try
        {
            if (!File.Exists(FiltersFilePath))
            {
                File.WriteAllText(FiltersFilePath,
                    "# Список подстрок для скрытия сообщений из чата.\n" +
                    "# Одна подстрока — одна строка. Регистр не важен.\n" +
                    "# Если строка сообщения содержит любую из подстрок — оно не показывается.\n" +
                    string.Join("\n", DefaultFilters) + "\n");
                Console.WriteLine($"[{ModuleName}] Создан filters.txt");
            }

            _filters.Clear();
            foreach (var line in File.ReadAllLines(FiltersFilePath))
            {
                var s = line.Trim();
                if (s.Length == 0 || s.StartsWith("#"))
                    continue;
                _filters.Add(s.ToLowerInvariant());
            }

            if (_filters.Count == 0)
                foreach (var f in DefaultFilters)
                    _filters.Add(f.ToLowerInvariant());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Ошибка чтения filters.txt: {ex.Message}");
            _filters.Clear();
            foreach (var f in DefaultFilters)
                _filters.Add(f.ToLowerInvariant());
        }
    }

    // Перехват чат-сообщения: если текст подходит под фильтр — блокируем.
    private HookResult OnSayText(UserMessage um)
    {
        try
        {
            string text = ExtractText(um);

            if (_debug && text.Trim().Length > 0)
                Console.WriteLine($"[{ModuleName}] TEXT: \"{text}\"");

            string lower = text.ToLowerInvariant();
            if (lower.Length == 0)
                return HookResult.Continue;

            foreach (var f in _filters)
            {
                if (lower.Contains(f))
                    return HookResult.Stop; // сообщение не покажется
            }
        }
        catch
        {
            // При ошибке чтения не мешаем чату.
        }

        return HookResult.Continue;
    }

    // Возможные строковые поля в разных типах текстовых сообщений.
    private static readonly string[] TextFields =
    {
        "messagename", "text", "message", "param1", "param2", "param3", "param4"
    };

    // Собирает весь текст сообщения из всех известных полей.
    private string ExtractText(UserMessage um)
    {
        var parts = new List<string>();

        foreach (var field in TextFields)
        {
            if (um.HasField(field))
            {
                var val = um.ReadString(field);
                if (!string.IsNullOrEmpty(val))
                    parts.Add(val);
            }
        }

        // params — повторяемое поле с подстановками текста.
        if (um.HasField("params"))
        {
            int count = um.GetRepeatedFieldCount("params");
            for (int i = 0; i < count; i++)
            {
                var val = um.ReadString("params", i);
                if (!string.IsNullOrEmpty(val))
                    parts.Add(val);
            }
        }

        return string.Join(" ", parts);
    }

    // Перезагрузить список фильтров из файла без рестарта.
    [CounterStrikeSharp.API.Core.Attributes.Registration.ConsoleCommand("css_chatfilter_reload", "Перезагрузить фильтры чата")]
    [CounterStrikeSharp.API.Modules.Commands.CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnReload(CCSPlayerController? caller, CommandInfo command)
    {
        LoadFilters();
        command.ReplyToCommand($"[Chat Filter] Перезагружено фильтров: {_filters.Count}");
    }

    // Включить/выключить вывод текста всех сообщений в консоль сервера.
    [CounterStrikeSharp.API.Core.Attributes.Registration.ConsoleCommand("css_chatfilter_debug", "Показывать текст сообщений в консоли")]
    [CounterStrikeSharp.API.Modules.Commands.CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnDebug(CCSPlayerController? caller, CommandInfo command)
    {
        _debug = !_debug;
        string state = _debug ? "ВКЛючена" : "ВЫКЛючена";
        Console.WriteLine($"[{ModuleName}] Диагностика {state}. Смотри консоль — найди строку с нужным текстом.");
        command.ReplyToCommand($"[Chat Filter] Диагностика {state}. Смотри консоль сервера.");
    }

    public override void Unload(bool hotReload)
    {
        foreach (var id in TextMessageIds)
            UnhookUserMessage(id, OnSayText, HookMode.Pre);
        Console.WriteLine($"[{ModuleName}] Плагин выгружен!");
    }
}