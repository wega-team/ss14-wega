using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.Components;
using Content.Shared.Mining.Components;
using Content.Shared.Power;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Examine;

namespace Content.Server.Mining;

public sealed class MiningServerSystem : EntitySystem
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiningServerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MiningServerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MiningServerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<MiningServerComponent, ExaminedEvent>(OnExamined);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<MiningServerComponent, PowerConsumerComponent>();

        while (query.MoveNext(out var uid, out var server, out var consumer))
        {
            if (server.IsBroken || consumer.ReceivedPower < server.ActualPowerConsumption)
            {
                server.CurrentTemperature = Math.Max(server.CurrentTemperature - 0.145f * frameTime, 293f);
                continue;
            }

            float ambientTemperature = 293f;
            if (_atmosphereSystem.GetContainingMixture(uid, excite: true) is { } atmosphere)
                ambientTemperature = atmosphere.Temperature;

            var baseHeatGeneration = server.ActualHeatGeneration * frameTime;

            float ambientHeatMultiplier = 1f;
            if (ambientTemperature > server.BreakdownTemperature * 0.7f)
            {
                ambientHeatMultiplier = 1f + (ambientTemperature - server.BreakdownTemperature * 0.7f) / (server.BreakdownTemperature * 0.3f);
            }

            var heatGeneration = baseHeatGeneration * ambientHeatMultiplier;
            if (consumer.DrawRate != server.ActualPowerConsumption)
                consumer.DrawRate = server.ActualPowerConsumption;

            if (server.IsActive)
            {
                server.CurrentTemperature += heatGeneration;
                HeatSurroundingAtmosphere(uid, heatGeneration);

                if (TryGetAccount(out var account))
                {
                    var efficiency = GetEfficiency(server.Mode, server.MiningStage);
                    if (server.Mode == MiningMode.Credits)
                    {
                        account.Credits += efficiency * frameTime;
                    }
                    else
                    {
                        account.ResearchPoints += efficiency * frameTime;
                    }
                }
            }
            else
            {
                server.CurrentTemperature += heatGeneration * 0.2f;
            }

            server.CurrentTemperature -= 0.145f * frameTime;
            server.CurrentTemperature = Math.Max(server.CurrentTemperature, 293f);

            if (server.CurrentTemperature >= server.BreakdownTemperature && !server.IsBroken)
            {
                UpdateAppearance(uid, server);
                _ambient.SetAmbience(uid, false);
                server.IsBroken = true;
            }
        }
    }

    private void OnInit(Entity<MiningServerComponent> ent, ref ComponentInit args)
    {
        var account = EntityQuery<MiningAccountComponent>().FirstOrDefault();
        if (account == default)
            return;

        ent.Comp.Mode = account.GlobalMode;
    }

    private void OnMapInit(Entity<MiningServerComponent> ent, ref MapInitEvent args)
        => ent.Comp.MiningStage = 1;

    private void OnPowerChanged(Entity<MiningServerComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered == false)
        {
            ent.Comp.IsActive = args.Powered;
            UpdateAppearance(ent.Owner, ent.Comp);
            _ambient.SetAmbience(ent, args.Powered);
        }
    }

    private void OnExamined(Entity<MiningServerComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !TryComp<PowerConsumerComponent>(entity, out var consumer))
            return;

        float chargePercent = 0f;
        if (entity.Comp.ActualPowerConsumption > 0f)
        {
            chargePercent = (consumer.ReceivedPower / entity.Comp.ActualPowerConsumption) * 100f;
            chargePercent = Math.Clamp(chargePercent, 0f, 100f);
        }

        args.PushMarkup(Loc.GetString("mining-server-examined", ("percent", chargePercent.ToString("F0"))));
    }

    private bool TryGetAccount([NotNullWhen(true)] out MiningAccountComponent? account)
    {
        account = null;
        var station = EntityQuery<MiningAccountComponent>().FirstOrDefault();
        if (station == default)
            return false;

        account = station;
        return true;
    }

    private float GetEfficiency(MiningMode mode, int stage)
    {
        return mode switch
        {
            MiningMode.Credits => stage * 0.35f, // ~500к за 2 часа для 50 серверов
            MiningMode.Research => stage * 0.17f, // ~250к за 2 часа
            _ => 0f
        };
    }

    private void HeatSurroundingAtmosphere(EntityUid uid, float heatEnergy)
    {
        if (_atmosphereSystem.GetContainingMixture(uid, excite: true) is { } atmosphere)
            _atmosphereSystem.AddHeat(atmosphere, heatEnergy * 500f);
    }

    private void UpdateAppearance(EntityUid uid, MiningServerComponent? server = null)
    {
        if (!Resolve(uid, ref server))
            return;

        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            _appearance.SetData(uid, MiningServerVisuals.MiningStage, server.MiningStage, appearance);
            _appearance.SetData(uid, MiningServerVisuals.IsActive, server.IsActive, appearance);
        }
    }
}
