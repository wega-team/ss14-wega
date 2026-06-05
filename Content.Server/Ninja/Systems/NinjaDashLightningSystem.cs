using System.Numerics;
using Content.Server.Beam;
using Content.Shared.Ninja.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Spawns the katana dash visuals: a green lightning beam (ninja_blink sprite) along the dash path,
/// plus phase effects at both ends oriented to the dash direction.
/// </summary>
public sealed partial class NinjaDashLightningSystem : EntitySystem
{
    private const string LightningProto = "LightningNinjaDash"; // green

    // The ninja_blink sprite state used for the beam body (instead of the default random lightning_N).
    private const string BlinkState = "ninja_blink";

    [Dependency] private BeamSystem       _beam       = default!;
    [Dependency] private TransformSystem  _transform  = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EnergyKatanaComponent, AfterDashEvent>(OnAfterDash);
    }

    private void OnAfterDash(Entity<EnergyKatanaComponent> ent, ref AfterDashEvent args)
    {
        var destMap = _transform.ToMapCoordinates(args.Destination);
        if (destMap.MapId != args.Origin.MapId)
            return;

        // Lightning beam from where the ninja vanished to where they reappeared, drawn with the
        // ninja_blink sprite (passed as the beam body state so it isn't overridden by lightning_N).
        var anchor = Spawn(null, new MapCoordinates(args.Origin.Position, args.Origin.MapId));
        _beam.TryCreateBeam(anchor, args.User, LightningProto, BlinkState);
        QueueDel(anchor);

        // Face the phase effects the way the ninja dashed (= the way they were looking).
        var delta = destMap.Position - args.Origin.Position;
        var rot = delta.LengthSquared() > 0.0001f ? delta.ToWorldAngle().GetCardinalDir().ToAngle() : Angle.Zero;

        // Phase effects: where the ninja vanished from, and where they reappeared.
        var phaseOut = Spawn("NinjaPhaseOutEffect", args.Origin);
        _transform.SetWorldRotation(phaseOut, rot);

        var phaseIn = Spawn("NinjaPhaseInEffect", args.Destination);
        _transform.SetWorldRotation(phaseIn, rot);
    }
}
