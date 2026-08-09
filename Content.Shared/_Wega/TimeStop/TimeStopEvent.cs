using Content.Shared.Coordinates;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Administration;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio;
using System.Linq;
using Content.Shared.Magic;
using Content.Shared.Magic.Events;
using Content.Shared.CombatMode.Pacification;
using Robust.Shared.Network;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Wega.Shared.Magic;

public sealed partial class TimeStopSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
		
        SubscribeLocalEvent<TimeStopActionEvent>(OnStopTime);
    }

    private bool PassesSpellPrerequisites(EntityUid spell, EntityUid performer)
    {
        var ev = new BeforeCastSpellEvent(performer);
        RaiseLocalEvent(spell, ref ev);
        return !ev.Cancelled;
    }

    private void OnStopTime(TimeStopActionEvent args)
    {
        if (args.Handled || !PassesSpellPrerequisites(args.Action, args.Performer))
            return;
		
        if (_net.IsClient)
            return;

		args.Handled = true;

        EnsureComp<PacifiedComponent>(args.Performer);
        Timer.Spawn(args.TimePacified, () => RemComp<PacifiedComponent>(args.Performer));

        Spawn("Chronofield", Transform(args.Performer).Coordinates);
        var nearbyTargets = _entityLookup.GetEntitiesInRange<MobStateComponent>(Transform(args.Performer).Coordinates, 2.5f)
           .Where(target => target.Owner != args.Performer)
           .ToList();

        foreach (var target in nearbyTargets)
        {
			EnsureComp<AdminFrozenComponent>(target);
			Timer.Spawn(args.Time, () => RemComp<AdminFrozenComponent>(target));
        }
    }
}