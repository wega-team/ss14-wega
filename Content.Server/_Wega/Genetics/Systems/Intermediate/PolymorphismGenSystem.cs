using Content.Shared.Actions;
using Content.Shared.Genetics;
using Content.Shared.Humanoid;

namespace Content.Server.Genetics.System;

public sealed class PolymorphismGenSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly DnaModifierSystem _dnaModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PolymorphismGenComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PolymorphismGenComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<PolymorphismGenComponent, PolymorphismActionEvent>(OnPolymorphism);
    }

    private void OnInit(Entity<PolymorphismGenComponent> ent, ref ComponentInit args)
        => ent.Comp.PolymorphismActionEntity = _action.AddAction(ent, ent.Comp.PolymorphismAction);

    private void OnShutdown(Entity<PolymorphismGenComponent> ent, ref ComponentShutdown args)
        => _action.RemoveAction(ent.Comp.PolymorphismActionEntity);

    private void OnPolymorphism(Entity<PolymorphismGenComponent> ent, ref PolymorphismActionEvent args)
    {
        args.Handled = true;
        if (!TryComp<DnaModifierComponent>(ent, out var dna) || !HasComp<HumanoidAppearanceComponent>(ent))
            return;

        if (!TryComp<DnaModifierComponent>(args.Target, out var targetDna) || !HasComp<HumanoidAppearanceComponent>(args.Target))
            return;

        _dnaModifier.TryCloneHumanoid((ent, dna), (args.Target, targetDna));
    }
}

