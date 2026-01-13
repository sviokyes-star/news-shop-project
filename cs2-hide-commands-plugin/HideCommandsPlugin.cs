using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace HideCommandsPlugin;

public class HideCommandsPlugin : BasePlugin
{
    public override string ModuleName => "Hide Commands";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Okyes";
    public override string ModuleDescription => "Скрывает консольные команды из чата CS2";

    public override void Load(bool hotReload)
    {
        AddCommandListener("say", OnSayCommand);
        AddCommandListener("say_team", OnSayTeamCommand);
        
        Console.WriteLine($"[{ModuleName}] Плагин загружен!");
    }

    private HookResult OnSayCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        string message = commandInfo.GetArg(1).Trim();
        
        if (message.StartsWith("/") || message.StartsWith("!") || message.StartsWith("."))
        {
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }

    private HookResult OnSayTeamCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        string message = commandInfo.GetArg(1).Trim();
        
        if (message.StartsWith("/") || message.StartsWith("!") || message.StartsWith("."))
        {
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }
}
