using Content.Server.Body;
using Content.Shared._Wega.Android;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Humanoid;
using Content.Shared.PowerCell.Components;
using Content.Shared.Preferences;
using Robust.Server.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Enums;
using Content.Server.Popups;
using Robust.Server.Audio;
using Content.Shared.Corvax.TTS;

namespace Content.Server._Wega.Android;

public sealed partial class AndroidFrameSystem : SharedAndroidFrameSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly HumanoidProfileSystem _humanoid = default!;
    [Dependency] private readonly VisualBodySystem _visualBody = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly NamingSystem _naming = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AndroidFrameComponent, ComponentInit>(OnInit);
        Subs.BuiEvents<AndroidFrameComponent>(AndroidConstructUiKey.Key, subs =>
        {
            subs.Event<AndroidConstructEditMessage>(OnEditUiMessage);
            subs.Event<AndroidConstructAssembleMessage>(OnAssembleUiMessage);
        });
    }

    private void OnInit(EntityUid uid, AndroidFrameComponent component, ComponentInit args)
    {
        if (!_prototypeManager.Resolve(component.Species, out var species))
            return;

        var profile = new HumanoidCharacterProfile();
        profile.Species = component.Species;
        profile.Appearance.SkinColor = species.DefaultSkinTone;
        profile.Name = _naming.GetName(component.Species, profile.Gender);

        component.Profile = profile;
        Dirty(uid, component);
    }

    private void OnAssembleUiMessage(EntityUid uid, AndroidFrameComponent component, AndroidConstructAssembleMessage message)
    {
        if (!TryAssemble(uid, component))
            _popup.PopupEntity(Loc.GetString("android-construct-assemble-error-popup"), uid, message.Actor);
    }

    private void OnEditUiMessage(EntityUid uid, AndroidFrameComponent component, AndroidConstructEditMessage message)
    {
        if (message.NewProfile.Species != component.Species)
            return;

        component.Profile = message.NewProfile;
        Dirty(uid, component);
    }

    private bool TryAssemble(EntityUid uid, AndroidFrameComponent component)
    {
        if (!_prototypeManager.Resolve(component.Species, out var species) || component.Profile == null)
            return false;

        if (!TryGetFromSlot(uid, component.BatterySlot, out var battery) || !TryGetFromSlot(uid, component.BrainSlot, out var brain))
            return false;

        var profile = component.Profile;

        switch (profile.Sex)
        {
            case Sex.Male: profile = profile.WithGender(Gender.Male); break;
            case Sex.Female: profile = profile.WithGender(Gender.Female); break;
            default: profile = profile.WithGender(Gender.Neuter); break;
        }

        var speciesEntity = Spawn(species.Prototype, Transform(uid).Coordinates);
        _humanoid.ApplyProfileTo(speciesEntity, profile);
        _visualBody.ApplyProfileTo(speciesEntity, profile);
        _metaData.SetEntityName(speciesEntity, profile.Name);

        var tts = EnsureComp<TTSComponent>(speciesEntity);
        string voice = profile.Sex switch
        {
            Sex.Female => "Alyx_Alyx",
            _ => "Wheatley"
        };
        tts.VoicePrototypeId = voice;

        // Replace Battery
        if (TryComp<PowerCellSlotComponent>(speciesEntity, out var cellComp) && battery.HasValue &&
            _container.TryGetContainer(speciesEntity, cellComp.CellSlotId, out var cellSlot))
        {
            foreach (var toDelete in cellSlot.ContainedEntities)
                Del(toDelete);
            _container.Insert(battery.Value, cellSlot, force: true);
        }

        // Replace Brain
        if (TryComp<BodyComponent>(speciesEntity, out var bodyComp) && brain.HasValue && bodyComp.Organs != null)
        {
            foreach (var organ in bodyComp.Organs.ContainedEntities)
            {
                if (HasComp<BrainComponent>(organ))
                {
                    Del(organ);
                    _container.Insert(brain.Value, bodyComp.Organs, force: true);
                    break;
                }
            }
        }

        _audio.PlayPvs(component.AssembleSound, speciesEntity);
        Del(uid);

        return true;
    }
}
