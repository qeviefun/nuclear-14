// #Misfits Change /Add:/ Passive drug ambience text effect
using Content.Server._Misfits.DrugEffects;
using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Misfits.EntityEffects.Effects.DrugAmbience;

/// <summary>
///     Sends a private, nameless ambient message to a player while a reagent is metabolizing.
///     The message appears in local chat with no source entity attached.
/// </summary>
[UsedImplicitly]
public sealed partial class AmbientDrugFlavorText : EntityEffect
{
    [DataField(required: true)]
    public List<string> MessageKeys = new();

    [DataField]
    public string CooldownId = string.Empty;

    [DataField]
    public float Cooldown = 15f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (MessageKeys.Count == 0)
            return;

        var actorSystem = args.EntityManager.EntitySysManager.GetEntitySystem<ActorSystem>();
        if (!actorSystem.TryGetSession(args.TargetEntity, out var session))
            return;

        if (session is null)
            return;

        var timing = IoCManager.Resolve<IGameTiming>();
        var cooldowns = args.EntityManager.EnsureComponent<DrugAmbienceCooldownComponent>(args.TargetEntity);
        var cooldownId = string.IsNullOrWhiteSpace(CooldownId)
            ? $"{GetType().FullName}:{string.Join("|", MessageKeys)}"
            : CooldownId;

        if (cooldowns.NextAllowedById.TryGetValue(cooldownId, out var nextAllowed)
            && nextAllowed > timing.CurTime)
            return;

        cooldowns.NextAllowedById[cooldownId] = timing.CurTime + TimeSpan.FromSeconds(Cooldown);

        var random = IoCManager.Resolve<IRobustRandom>();
        var chatManager = IoCManager.Resolve<IChatManager>();
        var text = Loc.GetString(random.Pick(MessageKeys));

        chatManager.ChatMessageToOne(
            ChatChannel.Local,
            text,
            text,
            EntityUid.Invalid,
            false,
            session.Channel);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return null;
    }
}