// #Misfits Add - Shared PipBoy Network System base.
// Ensures PipBoyNetworkComponent is attached to NanoChatCard entities on init.
using Content.Shared.DeltaV.NanoChat;
using Content.Shared.Examine;

namespace Content.Shared._Misfits.PipBoy;

public abstract class SharedPipBoyNetworkSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        // Automatically attach PipBoyNetworkComponent to any NanoChatCard on startup.
        // Uses ComponentStartup instead of MapInitEvent to avoid duplicate subscription
        // with NanoChatSystem which already subscribes to (NanoChatCardComponent, MapInitEvent).
        SubscribeLocalEvent<NanoChatCardComponent, ComponentStartup>(OnNanoChatCardStartup);
        SubscribeLocalEvent<PipBoyNetworkComponent, ExaminedEvent>(OnExamined);
    }

    private void OnNanoChatCardStartup(Entity<NanoChatCardComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<PipBoyNetworkComponent>(ent);
    }

    private void OnExamined(Entity<PipBoyNetworkComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.IsLocked)
            args.PushMarkup(Loc.GetString("pipboy-hub-examine-locked"));
    }
}
