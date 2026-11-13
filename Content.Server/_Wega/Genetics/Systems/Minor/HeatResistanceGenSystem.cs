using Content.Shared.Atmos.Components;
using Content.Shared.Genetics;
using Content.Shared.Temperature.Components;

namespace Content.Server.Genetics.System;

public sealed partial class HeatResistanceGenSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnInit(Entity<HeatResistanceGenComponent> ent, ref ComponentInit args)
    {
        if (HasComp<FlammableComponent>(ent))
        {
            RemComp<FlammableComponent>(ent);
            ent.Comp.RemFlammable = true;
        }

        if (TryComp<TemperatureDamageComponent>(ent, out var temperature))
        {
            temperature.HeatDamageThreshold = temperature.HeatDamageThreshold * ent.Comp.ResistanceRatio;
        }
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<HeatResistanceGenComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.RemFlammable) AddComp<FlammableComponent>(ent);

        if (TryComp<TemperatureDamageComponent>(ent, out var temperature))
        {
            temperature.HeatDamageThreshold = temperature.HeatDamageThreshold / ent.Comp.ResistanceRatio;
        }
    }
}

