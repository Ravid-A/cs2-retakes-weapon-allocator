using CounterStrikeSharp.API.Core.Capabilities;
using Microsoft.Extensions.Logging;

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
        Plugin.AddTimer(0.1f, () =>
        {
            var sender = GetRetakesPluginEventSender();

            if (sender is null)
            {
                Plugin.Logger.LogError("Couldn't load the retakes plugin event sender capability; round announcements are disabled");
                return;
            }

            sender.RetakesPluginEventHandlers += RetakesEventHandler;
        });
    }

    public static void RetakeCapability_OnUnload()
    {
        // Previously this threw during unload whenever the capability was unavailable,
        // which aborted the rest of the teardown.
        var sender = GetRetakesPluginEventSender();

        if (sender is null)
        {
            return;
        }

        sender.RetakesPluginEventHandlers -= RetakesEventHandler;
        RetakesPluginEventSender = null;
    }

    private static IRetakesPluginEventSender? GetRetakesPluginEventSender()
    {
        if (RetakesPluginEventSender is not null)
        {
            return RetakesPluginEventSender;
        }

        RetakesPluginEventSender = new PluginCapability<IRetakesPluginEventSender>("retakes_plugin:event_sender").Get();
        return RetakesPluginEventSender;
    }

    private static void RetakesEventHandler(object? _, IRetakesPluginEvent @event)
    {
        if (@event is AnnounceBombsiteEvent)
        {
            HandleAnnounceBombsiteEvent();
        }
    }

    private static void HandleAnnounceBombsiteEvent()
    {
        if (IsWarmup)
        {
            return;
        }

        var mode = "normal mode";

        if (CurrentVote != null!)
        {
            mode = CurrentVote.Vote.Description + " mode";
        }

        if (RoundsCounter < Core.Config.PistolRound.RoundAmount)
        {
            mode = $"pistol rounds, {Core.Config.PistolRound.RoundAmount - RoundsCounter} rounds left";
        }

        PrintToChatAll($"{Prefix} Retake {mode}.");
    }
}
