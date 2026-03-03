using Content.Server.Lavaland.Mobs.Components;
using Content.Shared.Achievements;
using Content.Shared.Lavaland.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Lavaland.Mobs;

public sealed partial class BloodDrunkMinerSystem : EntitySystem
{
    [Dependency] private readonly SharedAchievementsSystem _achievement = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodDrunkMinerComponent, MegafaunaKilledEvent>(OnBloodDrunkMinerKilled);
        SubscribeLocalEvent<BloodDrunkMinerComponent, BloodDrunkMinerDashAction>(OnDash);
    }

    private void OnBloodDrunkMinerKilled(EntityUid uid, BloodDrunkMinerComponent component, MegafaunaKilledEvent args)
    {
        if (args.Killer == null)
            return;

        _achievement.QueueAchievement(args.Killer.Value, AchievementsEnum.MinerBoss);
    }

    private void OnDash(Entity<BloodDrunkMinerComponent> ent, ref BloodDrunkMinerDashAction args)
    {
        args.Handled = true;
        _transform.SetCoordinates(ent, args.Target);
        _audio.PlayPvs(args.DashSound, ent);
    }
}
