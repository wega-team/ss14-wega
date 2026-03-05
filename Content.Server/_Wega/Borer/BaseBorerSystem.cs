using Content.Shared.Borer.BorerInfectedEr;
using Content.Shared.Borer.Wormer;
using Robust.Shared.Containers;
using Robust.Server.Containers;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Server.Popups;
using Content.Shared.Borer.WormEvent;
using Content.Shared.IdentityManagement.Components;

namespace Content.Server.Borer.BorerInfectedSystem;

public sealed class BaseBorerSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BorerComponent, InfectionBorerEvent>(OnInfectionUse);
        SubscribeLocalEvent<BorerComponent, InfectEvent>(Infection)
    }

    private void OnInfectionUse(Entity<BorerComponent> ent, ref ComponentInit args)
    {
        if (!HasComp<IdentityComponent>(args.Target))
            return;
        if (HasComp<SSDIndicator>(args.Target) && (args.Target.SSDIndicator.InSSD == True))
            return;
        if (args.Handled || _whitelistSystem.IsWhitelistFailOrNull(ent.Comp.Whitelist, args.Target))
            return;

        var target = args.Target

        if (ent.Comp.Owner is not null)
        {
            _popup.PopupEntity(Loc.GetString("Владелец уже есть"), uid, uid, PopupType.Medium);
            return;
        }
        if (HasComp<WormInfectedComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("Он уже заражён"), uid, uid, PopupType.Medium);
            return;
        }
        
        var doAfterEventsArgs = new DoAfterArgs(EntityManager, ent.Owner, Entity.Comp.TimeToInfect, new InfectEvent(), Entity.Owner, target: target , used: Entity.Owner)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = false,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;
    }
    private void InfectEvent(Entity<BorerComponent> end, ref InfectEvent args)
    {
    }
}