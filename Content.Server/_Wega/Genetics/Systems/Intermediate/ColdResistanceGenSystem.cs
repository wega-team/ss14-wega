using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Genetics;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Temperature.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Genetics.System;

public sealed partial class ColdResistanceGenSystem : EntitySystem
{
    private static readonly EntProtoId Effect = "StatusEffectPressureImmunity";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ColdResistanceGenComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ColdResistanceGenComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(Entity<ColdResistanceGenComponent> ent, ref ComponentInit args)
    {
        if (TryComp<TemperatureDamageComponent>(ent, out var temperature))
        {
            ent.Comp.OldColdResistance = temperature.ColdDamageThreshold;
            temperature.ColdDamageThreshold = Atmospherics.TCMB;
        }

        if (HasComp<BarotraumaComponent>(ent))
        {
            var status = EnsureComp<PermanentStatusEffectsComponent>(ent);
            status.StatusEffects.Add(Effect);
        }
    }

    private void OnShutdown(Entity<ColdResistanceGenComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<TemperatureDamageComponent>(ent, out var temperature))
            temperature.ColdDamageThreshold = ent.Comp.OldColdResistance;

        if (HasComp<BarotraumaComponent>(ent) && TryComp<PermanentStatusEffectsComponent>(ent, out var status))
        {
            if (status.StatusEffects.Contains(Effect))
                status.StatusEffects.Remove(Effect);

            if (status.StatusEffects.Count == 0)
            {
                RemComp<PermanentStatusEffectsComponent>(ent);
            }
        }
    }
}

