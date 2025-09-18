using Content.Shared.Actions;
using Content.Shared.Instruments;

namespace Content.Server.Instruments;

public sealed partial class InstrumentSystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;

    private void OnMapInit(EntityUid uid, InstrumentComponent component, ref MapInitEvent args)
    {
        component.ActionUid = _action.AddAction(uid, component.Action);
    }

    private void OnShutdown(EntityUid uid, InstrumentComponent component, ref ComponentShutdown args)
    {
        _action.RemoveAction(component.ActionUid);
        component.ActionUid = null;
    }
}
