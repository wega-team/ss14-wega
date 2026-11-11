using System.Numerics;
using Content.Shared._Wega.Weapons.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared._Wega.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server._Wega.Weapons.Systems;

public sealed partial class FlameBashSystem : EntitySystem
{
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnMeleeHit(Entity<FlameBashComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.Direction is not { } direction)
            return;

        if (direction == Vector2.Zero)
            return;

        if (!TryComp<BallisticAmmoProviderComponent>(ent, out _))
            return;

        var count = _gun.GetAmmoCount(ent);
        var capacity = _gun.GetAmmoCapacity(ent);

        if (count != capacity || count < ent.Comp.RequiredAmmo)
            return;

        direction = Vector2.Normalize(direction);

        var take = new TakeAmmoEvent(ent.Comp.RequiredAmmo, [], Transform(args.User).Coordinates, args.User);

        RaiseLocalEvent(ent.Owner, take);

        foreach (var (ammoEntity, _) in take.Ammo)
        {
            if (ammoEntity is { } ammo)
                QueueDel(ammo);
        }

        args.Handled = true;

        _audio.PlayPredicted(ent.Comp.FlameSound, ent.Owner, args.User);
        SendFlameEffect(ent, args.User, direction);
        ApplyFlame(ent, args.User, direction);
    }

    private void ApplyFlame(Entity<FlameBashComponent> ent, EntityUid user, Vector2 direction)
    {
        var userXform = Transform(user);
        var userPosition = _transform.GetWorldPosition(userXform);

        var flameOrigin = userPosition + direction * ent.Comp.FlameOffset;
        var perpendicular = new Vector2(-direction.Y, direction.X);

        var maxForward = ent.Comp.FlameOffset + ent.Comp.FlameRange;
        var halfWidth = ent.Comp.FlameWidth / 2f;

        var searchRadius = MathF.Sqrt(maxForward * maxForward + halfWidth * halfWidth);

        foreach (var target in _lookup.GetEntitiesInRange<DamageableComponent>(userXform.Coordinates, searchRadius))
        {
            if (target.Owner == user)
                continue;

            var targetXform = Transform(target.Owner);

            if (targetXform.MapID != userXform.MapID)
                continue;

            var targetPosition = _transform.GetWorldPosition(targetXform);
            var offset = targetPosition - flameOrigin;

            var forward = Vector2.Dot(offset, direction);
            var sideways = MathF.Abs(Vector2.Dot(offset, perpendicular));

            if (forward < 0f || forward > ent.Comp.FlameRange)
                continue;

            if (sideways > ent.Comp.FlameWidth / 2f)
                continue;

            var originCoords = new MapCoordinates(userPosition, userXform.MapID);

            var targetCoords = new MapCoordinates(targetPosition, userXform.MapID);

            if (!_interaction.InRangeUnobstructed(originCoords, targetCoords, searchRadius, predicate: uid => uid == user || uid == target.Owner))
                continue;

            var secondRow = forward >= ent.Comp.FlameRange / 2f;

            var damageAmount = ent.Comp.FlameDamage * (secondRow ? ent.Comp.DamageFalloff : 1f);

            var damage = new DamageSpecifier
            {
                DamageDict =
                {
                    { "Heat", FixedPoint2.New(damageAmount) }
                }
            };

            _damage.TryChangeDamage(target.Owner, damage, origin: user);

            if (TryComp<FlammableComponent>(target.Owner, out var flammable))
            {
                var stacks = secondRow ? ent.Comp.FireStacks / 2f : ent.Comp.FireStacks;

                _flammable.AdjustFireStacks(target.Owner, stacks, flammable);

                _flammable.Ignite(target.Owner, ent.Owner, flammable, user);
            }
        }
    }

    private void SendFlameEffect(Entity<FlameBashComponent> ent, EntityUid user, Vector2 direction)
    {
        var userXform = Transform(user);
        var userPosition = _transform.GetWorldPosition(userXform);

        var center = userPosition + direction * (ent.Comp.FlameOffset + ent.Comp.FlameRange / 2f);

        var coordinates = _transform.ToCoordinates(new MapCoordinates(center, userXform.MapID));

        var rotation = direction.ToWorldAngle() - Angle.FromDegrees(180);

        var ev = new FlameBashVisualEvent(ent.Comp.FlameEffect, GetNetCoordinates(coordinates), rotation);

        RaiseNetworkEvent(ev, Filter.PvsExcept(user, entityManager: EntityManager));
    }
}