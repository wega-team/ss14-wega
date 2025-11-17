using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.NodeContainer;
using Content.Shared.VentCraw.Components;

namespace Content.Server.VentCraw;

public sealed class BeingVentCrawSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeingVentCrawComponent, AtmosExposedGetAirEvent>(OnGetAir);
    }

    private void OnGetAir(EntityUid uid, BeingVentCrawComponent component, ref AtmosExposedGetAirEvent args)
    {
        if (!TryComp<VentCrawHolderComponent>(component.Holder, out var holder) || holder.CurrentTube == null)
            return;

        if (!TryComp(holder.CurrentTube.Value, out NodeContainerComponent? nodeContainer))
            return;

        foreach (var (_, node) in nodeContainer.Nodes)
        {
            if (node is PipeNode pipe)
            {
                args.Gas = pipe.Air;
                args.Handled = true;
                return;
            }
        }
    }
}
