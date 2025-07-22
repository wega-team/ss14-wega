using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Martial.Arts;
using Content.Shared.Martial.Arts.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Martial.Arts;

/// Boxe
public sealed partial class MartialArtsSystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    private void InitializeBoxing()
    {
        SubscribeLocalEvent<MartialArtsComponent, BoxingJabActionEvent>(OnJab);
        SubscribeLocalEvent<MartialArtsComponent, BoxingHookActionEvent>(OnHook);
        SubscribeLocalEvent<MartialArtsComponent, BoxingDodgeActionEvent>(OnDodge);
    }

    #region Processing
    private void OnJab(Entity<MartialArtsComponent> ent, ref BoxingJabActionEvent args)
    {
        args.Handled = true;
        string type = "Jab";
        if (TryPrepared(ent, type))
            return;

        AddPrepared(ent, type);
    }

    private void OnHook(Entity<MartialArtsComponent> ent, ref BoxingHookActionEvent args)
    {
        args.Handled = true;
        string type = "Hook";
        if (TryPrepared(ent, type))
            return;

        AddPrepared(ent, type);
    }

    private void OnDodge(Entity<MartialArtsComponent> ent, ref BoxingDodgeActionEvent args)
    {
        var transformSystem = EntityManager.System<SharedTransformSystem>();

        var transform = Transform(ent);
        var entPosition = transformSystem.GetMapCoordinates(transform);
        var lookDirection = transformSystem.GetWorldRotation(transform).ToWorldVec().Normalized();
        var dodgeDirection = -lookDirection;

        var force = 1500f;
        if (TryComp(ent, out PhysicsComponent? physics))
        {
            if (_mapMan.TryFindGridAt(entPosition, out _, out var grid) && grid != null)
            {
                if (_map.TryGetTileRef(ent, grid, transform.Coordinates, out _))
                {
                    if (physics.Mass < 80f)
                        force *= 2;

                    _physics.ApplyLinearImpulse(ent, dodgeDirection * force, body: physics);
                }
            }
        }

        args.Handled = true;
    }
    #endregion

    #region Handles
    private void HandleJab(EntityUid target)
    {
        if (!TryComp<StaminaComponent>(target, out var stamina))
            return;

        _stamina.TryTakeStamina(target, 8f, stamina);
    }

    private void HandleHook(EntityUid target)
    {
        if (!TryComp<StaminaComponent>(target, out var stamina))
            return;

        _stamina.TryTakeStamina(target, 25f, stamina);

        if (TryComp<DamageableComponent>(target, out var damage))
        {
            var totalDamage = damage.TotalDamage;
            if (totalDamage >= 125)
                return;

            float paralyzeChance = Math.Clamp((float)totalDamage / 100f, 0f, 1f);
            float randomValue = _random.NextFloat();

            if (randomValue <= paralyzeChance)
            {
                _stun.TryParalyze(target, TimeSpan.FromSeconds(4), true);
            }
        }
    }
    #endregion
}
