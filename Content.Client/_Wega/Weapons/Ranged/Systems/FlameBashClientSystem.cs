using System.Numerics;
using Content.Client.Weapons.Ranged.Components;
using Content.Client.Weapons.Ranged.Systems;
using Content.Shared._Wega.Weapons.Components;
using Content.Shared._Wega.Weapons.Ranged.Events;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Wega.Weapons.Systems;

public sealed partial class FlameBashClientSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnMeleeHit(Entity<FlameBashComponent> ent, ref MeleeHitEvent args)
    {
        if (!_timing.IsFirstTimePredicted || !args.IsHit || args.Direction is not { } direction)
            return;

        if (direction == Vector2.Zero)
            return;

        if (!HasComp<BallisticAmmoProviderComponent>(ent.Owner))
            return;

        var count = _gun.GetAmmoCount(ent.Owner);
        var capacity = _gun.GetAmmoCapacity(ent.Owner);

        if (count != capacity || count < ent.Comp.RequiredAmmo)
            return;

        direction = Vector2.Normalize(direction);

        _audio.PlayPredicted(ent.Comp.FlameSound, ent.Owner, args.User);

        var userXform = Transform(args.User);
        var userPosition = _transform.GetWorldPosition(userXform);

        var center = userPosition + direction * (ent.Comp.FlameOffset + ent.Comp.FlameRange / 2f);

        var coordinates = _transform.ToCoordinates(new MapCoordinates(center, userXform.MapID));

        var rotation = direction.ToWorldAngle() - Angle.FromDegrees(180);

        ShowFlame(ent.Comp.FlameEffect, coordinates, rotation);
    }

    [SubscribeNetworkEvent]
    private void OnFlameVisual(FlameBashVisualEvent args)
    {
        ShowFlame(args.Prototype, GetCoordinates(args.Coordinates), args.Rotation);
    }

    private void ShowFlame(EntProtoId prototype, EntityCoordinates coordinates, Angle rotation)
    {
        if (!coordinates.IsValid(EntityManager))
            return;

        var effect = SpawnAtPosition(prototype, coordinates);

        _transform.SetWorldRotationNoLerp(effect, rotation);
    }

    [SubscribeLocalEvent]
    private void OnAppearanceChanged(Entity<FlameBashComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!args.TryGetData<int>(AmmoVisuals.AmmoCount, out _))
            return;

        if (!TryComp<AmmoCounterComponent>(ent.Owner, out var counter) || counter.Control == null)
            return;

        var ev = new GunSystem.UpdateAmmoCounterEvent
        {
            Control = counter.Control
        };

        RaiseLocalEvent(ent.Owner, ev, false);
    }
}