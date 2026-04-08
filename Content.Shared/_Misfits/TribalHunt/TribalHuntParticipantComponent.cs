using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared._Misfits.TribalHunt;

/// <summary>
/// Grants the offering action used to progress active tribal hunts.
/// </summary>
[RegisterComponent]
public sealed partial class TribalHuntParticipantComponent : Component
{
    [DataField]
    public EntProtoId<InstantActionComponent> OfferAction = "ActionTribalOfferTrophy";

    [DataField]
    public EntityUid? OfferActionEntity;

    [DataField]
    public string TargetDepartment = "Tribe";
}
