using Content.Shared.Atmos;
using Content.Shared.Genetics;
using Content.Shared.Temperature.Components;

namespace Content.Server.Genetics.System;

public sealed partial class ColdResistanceGenSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnInit(Entity<ColdResistanceGenComponent> ent, ref ComponentInit args)
    {
        if (TryComp<TemperatureDamageComponent>(ent, out var temperature))
        {
            ent.Comp.OldColdResistance = temperature.ColdDamageThreshold;
            temperature.ColdDamageThreshold = Atmospherics.TCMB;
        }
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<ColdResistanceGenComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<TemperatureDamageComponent>(ent, out var temperature))
            temperature.ColdDamageThreshold = ent.Comp.OldColdResistance;
    }
}

