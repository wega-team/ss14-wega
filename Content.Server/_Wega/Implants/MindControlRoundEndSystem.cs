using System.Linq;
using Content.Server._Wega.Implants.Components;
using Content.Server.Objectives;
using Content.Shared.GameTicking;
using Content.Shared.Mind;

namespace Content.Server._Wega.Implants;

public sealed partial class MindControlRoundEndSystem : EntitySystem
{
    [Dependency] private ObjectivesSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextAppend);
    }

    private void OnRoundEndTextAppend(ref RoundEndTextAppendEvent args)
    {
        var controlled = EntityQueryEnumerator<MindControlRoundInfoComponent>();
        var groups = new Dictionary<string, List<string>>();

        while (controlled.MoveNext(out var controlledMindUid, out var info))
        {
            if (string.IsNullOrWhiteSpace(info.ControlledName) ||
                string.IsNullOrWhiteSpace(info.MasterName))
                continue;

            var masterTitle = info.MasterName;

            if (TryComp<MindComponent>(info.MasterMind, out var masterMind))
            {
                masterTitle = _objectives.GetTitle(
                    (info.MasterMind, masterMind),
                    info.MasterName);
            }

            var controlledTitle = info.ControlledName;

            if (TryComp<MindComponent>(controlledMindUid, out var controlledMind))
            {
                controlledTitle = _objectives.GetTitle(
                    (controlledMindUid, controlledMind),
                    info.ControlledName);
            }

            if (!groups.TryGetValue(masterTitle, out var names))
            {
                names = new List<string>();
                groups.Add(masterTitle, names);
            }

            names.Add(controlledTitle);
        }

        if (groups.Count == 0)
            return;

        var total = groups.Values.Sum(names => names.Count);

        args.AddLine(Loc.GetString(
            "mind-control-round-end-header",
            ("count", total)));

        args.AddLine(string.Empty);

        var firstGroup = true;

        foreach (var group in groups.OrderBy(g => g.Key))
        {
            if (!firstGroup)
                args.AddLine(string.Empty);

            firstGroup = false;

            args.AddLine(Loc.GetString(
                "mind-control-round-end-master",
                ("master", group.Key),
                ("count", group.Value.Count)));

            foreach (var name in group.Value.OrderBy(n => n))
            {
                args.AddLine(Loc.GetString(
                    "mind-control-round-end-entry",
                    ("controlled", name)));
            }
        }
    }
}