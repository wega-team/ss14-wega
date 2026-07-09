using System.Linq;
using System.Numerics;
using Content.Server.Lavaland.Components;
using Content.Shared.Achievements;
using Content.Shared.Destructible;
using Content.Shared.Ghost;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Systems;

public sealed partial class NecropolisTendrilSystem : EntitySystem
{
    [Dependency] private SharedAchievementsSystem _achievement = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NecropolisTendrilComponent, DestructionEventArgs>(OnDestruction);
    }

    private void OnDestruction(Entity<NecropolisTendrilComponent> ent, ref DestructionEventArgs args)
    {
        var coordinates = Transform(ent).Coordinates;
        var chasmPrototype = ent.Comp.ChasmPrototype;

        Timer.Spawn(TimeSpan.FromSeconds(ent.Comp.ChasmDelay), () =>
        {
            _audio.PlayPredicted(ent.Comp.ChasmSound, coordinates, null);
            CreateChasms(coordinates, chasmPrototype);
        });
    }

    private void CreateChasms(EntityCoordinates coordinates, EntProtoId chasmProto)
    {
        if (!coordinates.IsValid(EntityManager))
            return;

        var actorsNearby = _lookup.GetEntitiesInRange<ActorComponent>(coordinates, 6f, flags: LookupFlags.Uncontained)
            .Where(a => !HasComp<GhostComponent>(a)).ToList();

        foreach (var player in actorsNearby)
        {
            _achievement.QueueAchievement(player, AchievementsEnum.NecropolisTendril);
        }

        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                var offset = new Vector2(x, y);
                var chasmPos = coordinates.Offset(offset);

                if (chasmPos.IsValid(EntityManager))
                    SpawnAtPosition(chasmProto, chasmPos);
            }
        }

        var extraChasms = _random.Next(3, 8);
        var expansionDirections = new List<Vector2>
        {
            new Vector2(-2, -2), new Vector2(-2, 2), new Vector2(2, -2), new Vector2(2, 2),
            new Vector2(-2, -1), new Vector2(-2, 0), new Vector2(-2, 1),
            new Vector2(2, -1), new Vector2(2, 0), new Vector2(2, 1),
            new Vector2(-1, -2), new Vector2(0, -2), new Vector2(1, -2),
            new Vector2(-1, 2), new Vector2(0, 2), new Vector2(1, 2)
        };

        _random.Shuffle(expansionDirections);

        for (var i = 0; i < extraChasms && i < expansionDirections.Count; i++)
        {
            var chasmPos = coordinates.Offset(expansionDirections[i]);
            if (chasmPos.IsValid(EntityManager))
                SpawnAtPosition(chasmProto, chasmPos);
        }
    }
}
