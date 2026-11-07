using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Genetics;
using Content.Shared.Humanoid;

namespace Content.Server.Genetics.System;

public sealed partial class PolymorphismGenSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private DnaModifierSystem _dnaModifier = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<PolymorphismGenComponent> ent, ref ComponentInit args)
        => ent.Comp.PolymorphismActionEntity = _action.AddAction(ent, ent.Comp.PolymorphismAction);

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<PolymorphismGenComponent> ent, ref ComponentShutdown args)
        => _action.RemoveAction(ent.Comp.PolymorphismActionEntity);

    [SubscribeLocalEvent]
    private void OnPolymorphism(Entity<PolymorphismGenComponent> ent, ref PolymorphismActionEvent args)
    {
        args.Handled = true;
        if (!HasComp<DnaModifierComponent>(ent) || !HasComp<HumanoidProfileComponent>(ent))
            return;

        if (!HasComp<DnaModifierComponent>(args.Target) || !HasComp<HumanoidProfileComponent>(args.Target))
            return;

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            ent,
            8f,
            new PolymorphismDoAfterEvent(),
            ent,
            args.Target
        )
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<PolymorphismGenComponent> ent, ref PolymorphismDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        if (!TryComp<DnaModifierComponent>(ent, out var dna))
            return;

        if (!TryComp<DnaModifierComponent>(args.Target, out var targetDna))
            return;

        _dnaModifier.TryCloneHumanoid((ent, dna), (args.Target.Value, targetDna));
    }
}

