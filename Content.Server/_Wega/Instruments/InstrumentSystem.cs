using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Instruments;
using Content.Shared.Item.ItemToggle;

namespace Content.Server.Instruments;

public sealed partial class InstrumentSystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    private void OnMapInit(EntityUid uid, InstrumentComponent component, ref MapInitEvent args)
    {
        component.ActionUid = _action.AddAction(uid, component.Action);
    }

    private void OnShutdown(EntityUid uid, InstrumentComponent component, ref ComponentShutdown args)
    {
        _action.RemoveAction(component.ActionUid);
        component.ActionUid = null;
    }

    public EntityUid? GetInstrumentListener(EntityUid instrumentUid, SharedInstrumentComponent? component = null)
    {
        if (!ResolveInstrument(instrumentUid, ref component))
            return null;

        if (TryComp<PrivateListeningComponent>(instrumentUid, out var privateListeting) && privateListeting.PrivateListening)
        {
            if (_toggle.IsActivated(instrumentUid) && HasComp<ClothingComponent>(instrumentUid))
                return Transform(instrumentUid).ParentUid;
        }

        return null;
    }
}
