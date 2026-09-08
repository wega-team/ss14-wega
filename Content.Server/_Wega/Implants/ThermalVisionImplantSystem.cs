using Content.Shared._Wega.Implants.Components;
using Content.Shared.Implants;
using Content.Shared.Overlays;

namespace Content.Server._Wega.Implants;

/// <summary>
/// Grants <see cref="ThermalVisionComponent"/> to whoever carries a
/// <see cref="ThermalVisionImplantComponent"/> implant, and removes it when the implant is taken out.
/// </summary>
public sealed class ThermalVisionImplantSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalVisionImplantComponent, ImplantImplantedEvent>(OnImplanted);
        SubscribeLocalEvent<ThermalVisionImplantComponent, ImplantRemovedEvent>(OnRemoved);
    }

    private void OnImplanted(Entity<ThermalVisionImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        EnsureComp<ThermalVisionComponent>(args.Implanted);
    }

    private void OnRemoved(Entity<ThermalVisionImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        RemCompDeferred<ThermalVisionComponent>(args.Implanted);
    }
}
