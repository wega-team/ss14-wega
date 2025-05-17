using Content.Shared.Buckle.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Surgery.Components;
using Content.Shared.Tools.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.Surgery;

public sealed partial class SurgerySystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        GraphsInitialize();
        UiInitialize();
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