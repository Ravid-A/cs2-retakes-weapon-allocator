using CounterStrikeSharp.API.Core.Capabilities;

using RetakesPluginShared;
using RetakesPluginShared.Events;

using static RetakesAllocator.Modules.Core;
using static RetakesAllocator.Modules.Utils;

namespace RetakesAllocator.Modules;

public class RetakeCapability
{

    private static IRetakesPluginEventSender? RetakesPluginEventSender { get; set; }

    public static void RetakeCapability_OnLoad()
    {
        Plugin.AddTimer(0.1f, () => { GetRetakesPluginEventSender().RetakesPluginEventHandlers += RetakesEventHandler; });
    }

    public static void RetakeCapability_OnUnload()
    {
        GetRetakesPluginEventSender().RetakesPluginEventHandlers -= RetakesEventHandler;
    }

    private static IRetakesPluginEventSender GetRetakesPluginEventSender()
    {
        if (RetakesPluginEventSender is not null)
        {
            return RetakesPluginEventSender;
        }

        var sender = new PluginCapability<IRetakesPluginEventSender>("retakes_plugin:event_sender").Get();
        if (sender is null)
        {
            throw new Exception("Couldn't load retakes plugin event sender capability");
        }

        RetakesPluginEventSender = sender;
        return sender;
    }

    private static void RetakesEventHandler(object? _, IRetakesPluginEvent @event)
    {
        Action? handler = @event switch
        {
            AnnounceBombsiteEvent => HandleAnnounceBombsiteEvent,
            _ => null
        };
        handler?.Invoke();
    }

    private static void HandleAnnounceBombsiteEvent()
    {
        if(GetGameRules().WarmupPeriod)
        {
            return;
        }

        string mode = "normal mode";

        if(currentVote != null!)
        {
            mode = currentVote.vote.Description + " mode";
        }

        if(RoundsCounter < Core.Config.PistolRound.RoundAmount)
        {
            mode = $"pistol rounds, {Core.Config.PistolRound.RoundAmount - RoundsCounter} rounds left";
        }

        PrintToChatAll($"{PREFIX} Retake {mode}.");
    }
}