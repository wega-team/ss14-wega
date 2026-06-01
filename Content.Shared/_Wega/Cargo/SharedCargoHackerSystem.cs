using Content.Shared.Cargo.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Ninja.Systems;

namespace Content.Shared.Cargo;

public abstract class SharedCargoHackerSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedNinjaGlovesSystem _gloves = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CargoHackerComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);
    }

    private void OnBeforeInteractHand(Entity<CargoHackerComponent> ent, ref BeforeInteractHandEvent args)
    {
        if (args.Handled || !_gloves.AbilityCheck(ent, args, out var target))
            return;

        if (!HasComp<CargoOrderConsoleComponent>(target))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, ent, ent.Comp.Delay,
            new CargoHackDoAfterEvent(), target: target, used: ent, eventTarget: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = 0.5f,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }
}
