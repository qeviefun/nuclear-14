// #Misfits Add - Screen-space overlay that draws [ALLY] and [ENEMY] tags above in-world entities
// when the local player's faction is engaged in an active war.
// Tags faction members (existing war factions) and individual /warjoin participants.
// Pattern mirrors AdminNameOverlay (Content.Client/Administration/AdminNameOverlay.cs).
// All faction membership checks route through NpcFactionSystem.IsMember to satisfy RA0002
// (NpcFactionMemberComponent.Factions is access-restricted to NpcFactionSystem).

using System.Numerics;
using Content.Shared._Misfits.FactionWar;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Maths;

namespace Content.Client._Misfits.FactionWar;

/// <summary>
/// Draws green <c>[ALLY]</c> or red <c>[ENEMY]</c> tags above entities whose faction is relevant
/// to the current war state. Active only while the local player is involved in at least one war.
/// Handles all faction members and individual /warjoin participants.
/// </summary>
internal sealed class AllyTagOverlay : Overlay
{
    private readonly FactionWarClientSystem _warSystem;
    private readonly IEntityManager         _entityManager;
    private readonly IPlayerManager         _playerManager;
    private readonly NpcFactionSystem        _npcFaction;
    private readonly IEyeManager            _eyeManager;
    private readonly EntityLookupSystem     _entityLookup;
    private readonly Font                   _font;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public AllyTagOverlay(
        FactionWarClientSystem warSystem,
        IEntityManager         entityManager,
        IPlayerManager         playerManager,
        NpcFactionSystem       npcFaction,
        IEyeManager            eyeManager,
        IResourceCache         resourceCache,
        EntityLookupSystem     entityLookup)
    {
        _warSystem     = warSystem;
        _entityManager = entityManager;
        _playerManager = playerManager;
        _npcFaction    = npcFaction;
        _eyeManager    = eyeManager;
        _entityLookup  = entityLookup;

        ZIndex = 195; // just below AdminNameOverlay (200) so admin tags render on top
        _font = new VectorFont(
            resourceCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var localEntity = _playerManager.LocalSession?.AttachedEntity;
        if (localEntity == null)
            return;

        var activeWars = _warSystem.ActiveWars;
        if (activeWars.Count == 0)
            return;

        // The effective faction is the player's NPC war faction or their /warjoin side.
        var effectiveFaction = _warSystem.LocalFactionId ?? _warSystem.LocalWarJoinSide;
        if (effectiveFaction == null)
            return;

        // Build the enemy faction list from active wars involving the effective faction.
        var enemyFactions = new List<string>(2);
        foreach (var war in activeWars)
        {
            if (war.AggressorFaction == effectiveFaction)
                enemyFactions.Add(war.TargetFaction);
            else if (war.TargetFaction == effectiveFaction)
                enemyFactions.Add(war.AggressorFaction);
        }

        // Build sets of all NPC faction IDs that map to our canonical war faction and to each enemy.
        // e.g. if effectiveFaction = "NCR", allyNpcFactions = { "NCR", "Rangers" }.
        var allyNpcFactions = new HashSet<string> { effectiveFaction };
        foreach (var (raw, canonical) in FactionWarConfig.FactionAliases)
        {
            if (canonical == effectiveFaction)
                allyNpcFactions.Add(raw);
        }

        var enemyNpcFactions = new HashSet<string>(enemyFactions);
        foreach (var ef in enemyFactions)
        {
            foreach (var (raw, canonical) in FactionWarConfig.FactionAliases)
            {
                if (canonical == ef)
                    enemyNpcFactions.Add(raw);
            }
        }

        var viewport = args.WorldAABB;
        var participants = _warSystem.WarParticipants;

        // ── Pass 1: NPC faction members ────────────────────────────────────
        var query = _entityManager.AllEntityQueryEnumerator<NpcFactionMemberComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out _))
        {
            if (uid == localEntity.Value)
                continue;

            if (_entityManager.GetComponent<TransformComponent>(uid).MapID != _eyeManager.CurrentMap)
                continue;

            var aabb = _entityLookup.GetWorldAABB(uid);
            if (!aabb.Intersects(viewport))
                continue;

            string tag;
            Color  tagColor;

            // Check NPC faction membership.
            var isAlly = false;
            foreach (var af in allyNpcFactions)
            {
                if (_npcFaction.IsMember(uid, af))
                {
                    isAlly = true;
                    break;
                }
            }

            if (isAlly)
            {
                tag      = "[ALLY]";
                tagColor = Color.LimeGreen;
            }
            else
            {
                var isEnemy = false;
                foreach (var ef in enemyNpcFactions)
                {
                    if (_npcFaction.IsMember(uid, ef))
                    {
                        isEnemy = true;
                        break;
                    }
                }

                if (isEnemy)
                {
                    tag      = "[ENEMY]";
                    tagColor = new Color(1f, 0.3f, 0.3f);
                }
                else
                {
                    // Not a war-faction member — check if they're a warjoin participant.
                    var netEnt = _entityManager.GetNetEntity(uid);
                    if (participants.TryGetValue(netEnt, out var side))
                    {
                        if (side == effectiveFaction)
                        {
                            tag      = "[ALLY]";
                            tagColor = Color.LimeGreen;
                        }
                        else
                        {
                            tag      = "[ENEMY]";
                            tagColor = new Color(1f, 0.3f, 0.3f);
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            var screenCoords = _eyeManager.WorldToScreen(
                aabb.Center + new Angle(-_eyeManager.CurrentEye.Rotation)
                    .RotateVec(aabb.TopRight - aabb.Center)) + new Vector2(1f, 7f);

            args.ScreenHandle.DrawString(_font, screenCoords, tag, tagColor);
        }

        // ── Pass 2: warjoin participants without NpcFactionMemberComponent ─
        // Most players have NpcFactionMemberComponent and are caught by pass 1.
        // This pass catches edge cases (entities without the component).
        foreach (var (netEntity, side) in participants)
        {
            var uid = _entityManager.GetEntity(netEntity);
            if (uid == localEntity.Value || !_entityManager.EntityExists(uid))
                continue;

            // Skip if already handled in pass 1 (has NpcFactionMemberComponent).
            if (_entityManager.HasComponent<NpcFactionMemberComponent>(uid))
                continue;

            if (!_entityManager.TryGetComponent<SpriteComponent>(uid, out _))
                continue;

            if (_entityManager.GetComponent<TransformComponent>(uid).MapID != _eyeManager.CurrentMap)
                continue;

            var aabb = _entityLookup.GetWorldAABB(uid);
            if (!aabb.Intersects(viewport))
                continue;

            string tag;
            Color tagColor;

            if (side == effectiveFaction)
            {
                tag      = "[ALLY]";
                tagColor = Color.LimeGreen;
            }
            else
            {
                tag      = "[ENEMY]";
                tagColor = new Color(1f, 0.3f, 0.3f);
            }

            var screenCoords = _eyeManager.WorldToScreen(
                aabb.Center + new Angle(-_eyeManager.CurrentEye.Rotation)
                    .RotateVec(aabb.TopRight - aabb.Center)) + new Vector2(1f, 7f);

            args.ScreenHandle.DrawString(_font, screenCoords, tag, tagColor);
        }
    }
}
