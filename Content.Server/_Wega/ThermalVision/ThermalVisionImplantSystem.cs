using Content.Shared._Wega.ThermalVision;
using Content.Shared.Implants;

namespace Content.Server._Wega.ThermalVision;

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
