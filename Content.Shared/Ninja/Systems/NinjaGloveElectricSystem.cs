using Content.Shared.CombatMode;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Ninja.Components;

namespace Content.Shared.Ninja.Systems;

/// <summary>
/// Deals stamina damage and plays a zap sound when the ninja performs a disarm while gloves are active.
/// DisarmedEvent is directed at the target; we filter via MobStateComponent (present on all mobs)
/// since StaminaComponent is already taken by SharedStaminaSystem for the same event.
/// </summary>
public sealed partial class NinjaGloveElectricSystem : EntitySystem
{
    [Dependency] private SharedStaminaSystem _stamina = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobStateComponent, DisarmedEvent>(OnDisarmed);
    }

    private void OnDisarmed(Entity<MobStateComponent> target, ref DisarmedEvent args)
    {
        if (!TryComp<NinjaGloveElectricComponent>(args.Source, out var comp))
            return;

        _stamina.TakeStaminaDamage(target, comp.DisarmStaminaDamage, source: args.Source, sound: comp.ZapSound);
    }
}
