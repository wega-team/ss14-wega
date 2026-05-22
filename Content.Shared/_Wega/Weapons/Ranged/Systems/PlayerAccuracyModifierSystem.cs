using Content.Shared.Weapons.Ranged.Events;
using Content.Shared._Wega.Weapons.Ranged.Components;

namespace Content.Shared._Wega.Weapons.Ranged.Systems;

public sealed partial class PlayerAccuracyModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerAccuracyModifierComponent, GunRefreshModifiersEvent>(OnGunRefresh);
    }

    private void OnGunRefresh(EntityUid uid, PlayerAccuracyModifierComponent comp, ref GunRefreshModifiersEvent args)
    {
        var multiplier = Math.Max(0.1f, comp.SpreadMultiplier);

        args.MinAngle *= multiplier;
        args.MaxAngle *= multiplier;

        // Ограничиваем максимальный разброс
        var maxRadians = MathHelper.DegreesToRadians(comp.MaxSpreadAngle);
        args.MinAngle = Math.Clamp(args.MinAngle, -maxRadians, maxRadians);
        args.MaxAngle = Math.Clamp(args.MaxAngle, -maxRadians, maxRadians);
    }
}
