using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.FactionLock;

public sealed partial class FactionLockSystem : EntitySystem
{
    private static readonly TimeSpan DenyCooldown = TimeSpan.FromSeconds(1);

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private NpcFactionSystem _npcFaction = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FactionLockComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<FactionLockComponent, AttemptMeleeEvent>(OnMeleeAttempt);
        SubscribeLocalEvent<FactionLockComponent, ExaminedEvent>(OnExamined);
    }

    public bool IsAllowed(Entity<FactionLockComponent?> ent, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return true;

        return _npcFaction.IsMemberOfAny(user, ent.Comp.Factions);
    }

    private void OnShotAttempted(Entity<FactionLockComponent> ent, ref ShotAttemptedEvent args)
    {
        if (args.Cancelled || !ent.Comp.BlockShooting || args.User == ent.Owner)
            return;

        if (IsAllowed(ent.AsNullable(), args.User))
            return;

        args.Cancel();

        var curTime = _timing.CurTime;
        if (ent.Comp.NextDeny > curTime)
            return;

        ent.Comp.NextDeny = curTime + DenyCooldown;
        _popup.PopupEntity(Loc.GetString(ent.Comp.DenyMessage), ent.Owner, args.User);
        _audio.PlayPredicted(ent.Comp.DenySound, ent.Owner, args.User);
    }

    private void OnMeleeAttempt(Entity<FactionLockComponent> ent, ref AttemptMeleeEvent args)
    {
        if (args.Cancelled || !ent.Comp.BlockMelee)
            return;

        var user = args.User;
        if (!user.IsValid())
        {
            if (!_hands.IsHeld(ent.Owner, out var holder))
                return;

            user = holder.Value;
        }

        if (IsAllowed(ent.AsNullable(), user))
            return;

        args.Cancelled = true;
        args.Message = Loc.GetString(ent.Comp.DenyMessage);
        _audio.PlayPredicted(ent.Comp.DenySound, ent.Owner, user);
    }

    private void OnExamined(Entity<FactionLockComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExamineText is not { } text || !args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString(text));
    }
}
