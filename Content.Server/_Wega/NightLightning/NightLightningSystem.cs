using Content.Server.Chat.Systems;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Content.Shared.Light.Components;
using Content.Shared.Night.Lightning.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Server.Night.Lightning;

public sealed class NightLightningSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightLightningComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<NightLightComponent, PointLightToggleEvent>(OnPointLightToggle);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var nightLightningQuery = EntityQueryEnumerator<NightLightningComponent>();
        while (nightLightningQuery.MoveNext(out var uid, out var nightLightningComponent))
        {
            nightLightningComponent.NextTimeTick -= frameTime;

            if (nightLightningComponent.NextTimeTick <= 0)
            {
                nightLightningComponent.NextTimeTick = 600f;
                UpdateNightLights(uid, nightLightningComponent);
            }
        }
    }

    private void UpdateNightLights(EntityUid uid, NightLightningComponent comp)
    {
        if (!_cfg.GetCVar(WegaCVars.NightLightEnabled))
            return;

        var station = Name(uid);
        var transform = Transform(uid);
        if (IsNightTime() && !comp.IsNight)
        {
            var lightEntities = _lookup.GetEntitiesInRange<PointLightComponent>(transform.Coordinates, 500f);
            foreach (var lightEntity in lightEntities)
            {
                var light = lightEntity.Owner;
                if (!TryComp<AmbientSoundComponent>(light, out var sound) || HasComp<NightLightBlockedComponent>(light))
                    continue;

                if (_light.TryGetLight(light, out var pointLight))
                {
                    var newEnergy = pointLight.Energy * 0.8f;
                    var newColor = new Color(173, 216, 230, 255);
                    _light.SetEnergy(light, newEnergy, pointLight);
                    _light.SetColor(light, newColor, pointLight);
                    EnsureComp<NightLightComponent>(light);

                    // Праздничный режим
                    if (_cfg.GetCVar(WegaCVars.PartyEnabled))
                        EnsureComp<RgbLightControllerComponent>(light);
                }
            }

            if (_cfg.GetCVar(WegaCVars.PartyEnabled))
            {
                _chat.DispatchGlobalAnnouncement(Loc.GetString("auto-announcements-holiday-mode", ("station", station)), Loc.GetString("auto-announcements-title"), true, colorOverride: Color.Turquoise);
                comp.IsNight = true;
                return;
            }

            _chat.DispatchGlobalAnnouncement(Loc.GetString("auto-announcements-night-enabled", ("station", station)), Loc.GetString("auto-announcements-title"), true, colorOverride: Color.Turquoise);
            comp.IsNight = true;
        }
        else if (!IsNightTime() && comp.IsNight)
        {
            var lightEntities = _lookup.GetEntitiesInRange<PointLightComponent>(transform.Coordinates, 500f);
            foreach (var lightEntity in lightEntities)
            {
                RemComp<NightLightComponent>(lightEntity);
            }

            _chat.DispatchGlobalAnnouncement(Loc.GetString("auto-announcements-night-disabled", ("station", station)), Loc.GetString("auto-announcements-title"), true, colorOverride: Color.Turquoise);
            comp.IsNight = false;
        }
    }

    private void OnPointLightToggle(EntityUid uid, NightLightComponent comp, PointLightToggleEvent ev)
    {
        if (HasComp<NightLightComponent>(uid))
        {
            if (!TryComp<AmbientSoundComponent>(uid, out var sound))
                return;

            if (_light.TryGetLight(uid, out var pointLight))
            {
                Timer.Spawn(500, () =>
                {
                    var newEnergy = pointLight.Energy * 0.8f;
                    var newColor = new Color(173, 216, 230, 255);
                    _light.SetEnergy(uid, newEnergy, pointLight);
                    _light.SetColor(uid, newColor, pointLight);
                });
            }
        }
    }

    private bool IsNightTime()
    {
        DateTime currentTime = DateTime.Now;
        if (_cfg.GetCVar(WegaCVars.PartyEnabled))
            return currentTime.Hour >= 0;
        return currentTime.Hour >= 19 || currentTime.Hour < 5;
    }

    private void OnComponentStartup(EntityUid uid, NightLightningComponent component, ComponentStartup ev)
    {
        Dirty(uid, component);
    }
}
