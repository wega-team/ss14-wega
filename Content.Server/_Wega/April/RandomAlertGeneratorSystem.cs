using Content.Server.Chat.Systems;
using Content.Server.Power.EntitySystems;
using Content.Shared.April.Fools.Components;
using Content.Shared.Humanoid;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.April.Fools;

public sealed class RandomAlertGeneratorSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var randomAlertQuery = EntityQueryEnumerator<RandomAlertGeneratorComponent>();
        while (randomAlertQuery.MoveNext(out var uid, out var comp))
        {
            comp.NextTimeTick -= frameTime;

            if (comp.NextTimeTick <= 0)
            {
                comp.NextTimeTick = _random.Next(120, 600);
                if (!this.IsPowered(uid, EntityManager))
                    return;

                SendAlert(comp);
            }
        }
    }

    private void SendAlert(RandomAlertGeneratorComponent comp)
    {
        if (comp.Messages.Count == 0)
            return;

        var humanoids = new List<(string Name, EntityUid Id)>();
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, ActorComponent>();
        while (query.MoveNext(out var id, out _, out _))
        {
            if (TryComp(id, out MetaDataComponent? meta))
                humanoids.Add((meta.EntityName, id));
        }

        string name1 = "Сотрудник #1";
        string name2 = "Сотрудник #2";
        if (humanoids.Count >= 2)
        {
            var picked = _random.Pick(humanoids);
            name1 = picked.Name;
            humanoids.Remove(picked);

            picked = _random.Pick(humanoids);
            name2 = picked.Name;
        }
        else if (humanoids.Count == 1)
        {
            name1 = humanoids[0].Name;
        }

        var message = Loc.GetString(
            _random.Pick(comp.Messages),
            ("name1", name1),
            ("name2", name2)
        );

        _chat.DispatchGlobalAnnouncement(message, playSound: true);
    }
}
