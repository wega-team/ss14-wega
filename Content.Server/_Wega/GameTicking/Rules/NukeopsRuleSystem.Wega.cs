using Content.Server.AlertLevel;
using Content.Server.Station.Systems;
using Content.Server.Nuke;
using Content.Server.NukeOps;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.NukeOps;
using Robust.Shared.Timing;
using static System.Collections.Specialized.BitVector32;

namespace Content.Server.GameTicking.Rules;

public sealed partial class NukeopsRuleSystem : GameRuleSystem<NukeopsRuleComponent>
{
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private void ChangeAlert()
    {
        var query = EntityQueryEnumerator<NukeopsRuleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.CanChangeAlertLevel)
            {
                if (comp.SetAlertlevel == null || comp.TargetStation == null)
                    continue;
                _alertLevelSystem.SetLevel(comp.TargetStation.Value, comp.SetAlertlevel, true, true, true, true);
                comp.CanChangeAlertLevel = false;
            }
        }
    }
}
