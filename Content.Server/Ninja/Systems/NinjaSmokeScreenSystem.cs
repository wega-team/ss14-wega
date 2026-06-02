using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Ninja.Components;
using Content.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Ninja.Systems;

public sealed partial class NinjaSmokeScreenSystem : EntitySystem
{
    private const string SuitDisableDelayId = "suit_powers";
    private const float SmokeScreenCharge = 95f;
    private const float SmokeDuration = 6f;
    private const int SmokeSpreadAmount = 7;

    private static readonly string[] SuitSmokeReagents =
    {
        "NinjaSmokeRed",
        "NinjaSmokeBlue",
        "NinjaSmokeGreen",
    };

    [Dependency] private NinjaCloakSystem _cloak = default!;
    [Dependency] private SpaceNinjaSystem _ninja = default!;
    [Dependency] private SmokeSystem _smoke = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NinjaSuitComponent, NinjaSmokeScreenEvent>(OnSmokeScreen);
    }

    private void OnSmokeScreen(Entity<NinjaSuitComponent> ent, ref NinjaSmokeScreenEvent args)
    {
        var user = args.Performer;

        if (_cloak.TryRevealCloak(user))
            return;

        args.Handled = true;

        if (TryComp<UseDelayComponent>(ent, out var delay) &&
            _useDelay.IsDelayed((ent, delay), SuitDisableDelayId))
            return;

        if (!_ninja.TryUseCharge(user, SmokeScreenCharge))
            return;

        var solution = new Solution();
        if (TryComp<SpiderOSComponent>(ent, out var spiderOS))
        {
            var idx = Math.Clamp(spiderOS.SuitColor, 0, SuitSmokeReagents.Length - 1);
            solution.AddReagent(SuitSmokeReagents[idx], FixedPoint2.New(1));
        }

        var smokeEnt = Spawn("NinjaSmoke", Transform(user).Coordinates);
        _smoke.StartSmoke(smokeEnt, solution, SmokeDuration, SmokeSpreadAmount);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/smoke_grenade_smoke.ogg"), user);
    }
}
