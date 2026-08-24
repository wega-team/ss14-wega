using Content.Server.Chat.Systems;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Content.Shared.Light.Components;
using Content.Shared.Night.Lightning.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Server.Night.Lightning;

public sealed partial class NightLightningSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private ChatSystem _chat = default!;

    private int _nightStartHour = 22;
    private int _nightEndHour = 8;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightLightningComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<NightLightComponent, PointLightToggleEvent>(OnPointLightToggle);

        _cfg.OnValueChanged(WegaCVars.NightStartHour, OnNightStartHourChanged, true);
        _cfg.OnValueChanged(WegaCVars.NightEndHour, OnNightEndHourChanged, true);
    }

    private void OnNightStartHourChanged(int obj) => _nightStartHour = Math.Clamp(obj, 0, 23);
    private void OnNightEndHourChanged(int obj) => _nightEndHour = Math.Clamp(obj, 0, 23);

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
                if (!HasComp<AmbientSoundComponent>(light) || HasComp<NightLightBlockedComponent>(light))
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
        if (_cfg.GetCVar(WegaCVars.PartyEnabled))
            return true;

        var currentHour = DateTime.Now.Hour;
        if (_nightStartHour > _nightEndHour)
        {
            return currentHour >= _nightStartHour || currentHour < _nightEndHour;
        }
        else if (_nightStartHour < _nightEndHour)
        {
            return currentHour >= _nightStartHour && currentHour < _nightEndHour;
        }

        return false;
    }

    private void OnComponentStartup(EntityUid uid, NightLightningComponent component, ComponentStartup ev)
    {
        Dirty(uid, component);
    }
}
