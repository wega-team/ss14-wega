using Content.Server.Silicons.Laws;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Silicons.Borgs;

public sealed partial class BorgSwitchableTypeSystem
{
    [Dependency] private readonly SiliconLawSystem _law = default!;

    private void ChangeLaw(EntityUid uid, ProtoId<SiliconLawsetPrototype> id)
    {
        var laws = _law.GetLawset(id);

        _law.SetLaws(laws.Laws, uid);

        if (TryComp<EmagSiliconLawComponent>(uid, out var emag))
        {
			if (emag.OwnerName != null)
			{
				var ev = new SiliconEmaggedEvent(uid);
				RaiseLocalEvent(uid, ref ev);
			}
        }
    }
}