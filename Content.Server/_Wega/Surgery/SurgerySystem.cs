using Content.Shared.Body.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Surgery.Components;
using Content.Shared.Tools.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Surgery;

public sealed partial class SurgerySystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        GraphsInitialize();
        InternalDamageInitialize();
        UiInitialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<OperatedComponent>();
        while (query.MoveNext(out var uid, out var operated))
        {
            if (_mobState.IsDead(uid) || operated.OperatedPart)
                continue;

            if (operated.NextUpdateTick <= 0)
            {
                if (operated.InternalDamages.Count != 0)
                {
                    ProcessInternalDamages(uid, operated);
                }
                operated.NextUpdateTick = 5f;
            }
            operated.NextUpdateTick -= frameTime;
        }
    }

    private bool TryGetOperatingTable(EntityUid patient, out float tableModifier)
    {
        tableModifier = 1f;
        if (!TryComp<BuckleComponent>(patient, out var buckle) || buckle.BuckledTo == null)
            return false;

        return TryComp<OperatingTableComponent>(buckle.BuckledTo.Value, out var operating) &&
            (tableModifier = operating.Modifier) > 0;
    }
}
