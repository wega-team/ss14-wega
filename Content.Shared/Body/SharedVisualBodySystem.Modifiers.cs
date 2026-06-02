using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Body;

public abstract partial class SharedVisualBodySystem
{
    [Dependency] private ISharedAdminManager _admin = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;

    private void InitializeModifiers()
    {
        SubscribeLocalEvent<VisualBodyComponent, GetVerbsEvent<Verb>>(OnGetVerbs);

        Subs.BuiEvents<VisualBodyComponent>(HumanoidMarkingModifierKey.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnModifiersOpened);
                subs.Event<HumanoidMarkingModifierMarkingSetMessage>(OnSetModifiers);
            });
    }

    private void OnGetVerbs(Entity<VisualBodyComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!_admin.HasAdminFlag(args.User, AdminFlags.Fun))
            return;

        var user = args.User;
        args.Verbs.Add(new Verb
        {
            Text = "Modify markings",
            Category = VerbCategory.Tricks,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Mobs/Customization/reptilian_parts.rsi"), "tail_smooth"),
            Act = () =>
            {
                _userInterface.OpenUi(ent.Owner, HumanoidMarkingModifierKey.Key, user);
            }
        });
    }

    /// <summary>
    /// Copies the appearance of organs from one body to another
    /// </summary>
    /// <param name="source">The body whose organs to copy the appearance from</param>
    /// <param name="target">The body whose organs to copy the appearance to</param>
    [PublicAPI]
    public void CopyAppearanceFrom(Entity<BodyComponent?> source, Entity<BodyComponent?> target)
    {
        if (!Resolve(source, ref source.Comp) || !Resolve(target, ref target.Comp))
            return;

        var sourceOrgans = _container.EnsureContainer<Container>(source, BodyComponent.ContainerID);

        foreach (var sourceOrgan in sourceOrgans.ContainedEntities)
        {
            var evt = new OrganCopyAppearanceEvent(sourceOrgan);
            RaiseLocalEvent(target, ref evt);
        }
    }

    /// <summary>
    /// Gathers all the markings-relevant data from this entity
    /// </summary>
    /// <param name="ent">The entity to sample</param>
    /// <param name="filter">If set, only returns data concerning the given layers</param>
    /// <param name="profiles">The profiles for the various organs</param>
    /// <param name="markings">The marking parameters for the various organs</param>
    /// <param name="applied">The markings that are applied to the entity</param>
    public bool TryGatherMarkingsData(Entity<VisualBodyComponent?> ent,
        HashSet<HumanoidVisualLayers>? filter,
        [NotNullWhen(true)] out Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData>? profiles,
        [NotNullWhen(true)] out Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData>? markings,
        [NotNullWhen(true)] out Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>? applied)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false))
        {
            profiles = null;
            markings = null;
            applied = null;
            return false;
        }

        profiles = new();
        markings = new();
        applied = new();

        var organContainer = _container.EnsureContainer<Container>(ent, BodyComponent.ContainerID);

        foreach (var organ in organContainer.ContainedEntities)
        {
            if (!TryComp<OrganComponent>(organ, out var organComp) || organComp.Category is not { } category)
                continue;

            if (TryComp<VisualOrganComponent>(organ, out var visualOrgan))
            {
                profiles.TryAdd(category, visualOrgan.Profile);
            }

            if (TryComp<VisualOrganMarkingsComponent>(organ, out var visualOrganMarkings))
            {
                markings.TryAdd(category, visualOrganMarkings.MarkingData);
                if (filter is not null)
                    applied.TryAdd(category, visualOrganMarkings.Markings.Where(kvp => filter.Contains(kvp.Key)).ToDictionary());
                else
                    applied.TryAdd(category, visualOrganMarkings.Markings);
            }
        }

        return true;
    }

    private void OnModifiersOpened(Entity<VisualBodyComponent> ent, ref BoundUIOpenedEvent args)
    {
        TryGatherMarkingsData(ent.AsNullable(), null, out var profiles, out var markings, out var applied);

        _userInterface.SetUiState(ent.Owner, HumanoidMarkingModifierKey.Key, new HumanoidMarkingModifierState(applied!, markings!, profiles!));
    }

    private void OnSetModifiers(Entity<VisualBodyComponent> ent, ref HumanoidMarkingModifierMarkingSetMessage args)
    {
        var markingsEvt = new ApplyOrganMarkingsEvent(args.Markings);
        RaiseLocalEvent(ent, ref markingsEvt);
    }

    /// <summary>
    /// Applies the given set of markings to the body.
    /// </summary>
    /// <param name="ent">The body whose organs to apply markings to.</param>
    /// <param name="markings">A dictionary of organ categories to markings information. Organs not included in this dictionary will remain unaffected.</param>
    [PublicAPI]
    public void ApplyMarkings(EntityUid ent, Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> markings)
    {
        var markingsEvt = new ApplyOrganMarkingsEvent(markings);
        RaiseLocalEvent(ent, ref markingsEvt);
    }

    private void ApplyAppearanceTo(Entity<VisualBodyComponent?> ent, HumanoidCharacterAppearance appearance, Sex sex)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ApplyProfile(ent,
            new()
        {
            Sex = sex,
            SkinColor = appearance.SkinColor,
            EyeColor = appearance.EyeColor,
        });

        var markingsEvt = new ApplyOrganMarkingsEvent(appearance.Markings);
        RaiseLocalEvent(ent, ref markingsEvt);
    }

    /// <summary>
    /// Applies the information contained with a <see cref="HumanoidCharacterProfile"/> to a visual body's appearance.
    /// This sets the profile data and markings of all organs contained within the profile.
    /// </summary>
    /// <param name="ent">The body to apply the profile to</param>
    /// <param name="profile">The profile to apply</param>
    [PublicAPI]
    public void ApplyProfileTo(Entity<VisualBodyComponent?> ent, HumanoidCharacterProfile profile)
    {
        ApplyAppearanceTo(ent, profile.Appearance, profile.Sex);
    }

    /// <summary>
    /// Applies profile data to all visual organs within the body.
    /// </summary>
    /// <param name="ent">The body to apply the organ profile to</param>
    /// <param name="profile">The profile to apply</param>
    [PublicAPI]
    public void ApplyProfile(EntityUid ent, OrganProfileData profile)
    {
        var profileEvt = new ApplyOrganProfileDataEvent(profile, null);
        RaiseLocalEvent(ent, ref profileEvt);
    }

    /// <summary>
    /// Applies profile data to the specified visual organs within the body.
    /// Organs not specified are left unchanged.
    /// </summary>
    /// <param name="ent">The body to apply the organ profiles to.</param>
    /// <param name="profiles">The profiles to apply.</param>
    [PublicAPI]
    public void ApplyProfiles(EntityUid ent, Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> profiles)
    {
        var profileEvt = new ApplyOrganProfileDataEvent(null, profiles);
        RaiseLocalEvent(ent, ref profileEvt);
    }

    /// <summary>
    /// Clears all markings from every <see cref="VisualOrganMarkingsComponent"/> organ in the body.
    /// Call this before <see cref="ApplyProfileTo"/> when restoring a body to its original appearance
    /// so that species-specific markings from a previous disguise don't bleed through.
    /// </summary>
    [PublicAPI]
    public void ClearAllMarkings(Entity<VisualBodyComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var organContainer = _container.EnsureContainer<Container>(ent, BodyComponent.ContainerID);
        foreach (var organ in organContainer.ContainedEntities)
        {
            if (TryComp<VisualOrganMarkingsComponent>(organ, out var markingsComp))
                SetOrganMarkings((organ, markingsComp), new Dictionary<HumanoidVisualLayers, List<Marking>>());
        }
    }

    /// <summary>
    /// Captures the current <see cref="PrototypeLayerData"/> reference for every
    /// <see cref="VisualOrganComponent"/> organ in the body.
    /// Store the result and pass it to <see cref="RestoreOrganAppearances"/> to undo any
    /// RSI / state changes that <c>CopyAppearanceFrom</c> applies during a disguise.
    /// </summary>
    [PublicAPI]
    public Dictionary<EntityUid, PrototypeLayerData> SaveOrganAppearances(Entity<VisualBodyComponent?> ent)
    {
        var saved = new Dictionary<EntityUid, PrototypeLayerData>();
        if (!Resolve(ent, ref ent.Comp, false))
            return saved;

        var organContainer = _container.EnsureContainer<Container>(ent, BodyComponent.ContainerID);
        foreach (var organ in organContainer.ContainedEntities)
        {
            if (TryComp<VisualOrganComponent>(organ, out var visualOrgan))
                saved[organ] = visualOrgan.Data;
        }

        return saved;
    }

    /// <summary>
    /// Re-applies the <see cref="PrototypeLayerData"/> references saved by
    /// <see cref="SaveOrganAppearances"/>, restoring each organ's RSI path and state.
    /// Call this before <see cref="ApplyProfileTo"/> on revert so that
    /// <c>SexStateOverrides</c> are resolved against the correct species RSI.
    /// </summary>
    [PublicAPI]
    public void RestoreOrganAppearances(Dictionary<EntityUid, PrototypeLayerData> saved)
    {
        foreach (var (organ, data) in saved)
        {
            if (TryComp<VisualOrganComponent>(organ, out var visualOrgan))
                SetOrganAppearance((organ, visualOrgan), data);
        }
    }

    /// <summary>
    /// Captures the current markings for every <see cref="VisualOrganMarkingsComponent"/> organ.
    /// Pass the result to <see cref="RestoreOrganMarkings"/> to undo markings copied by a disguise.
    /// </summary>
    [PublicAPI]
    public Dictionary<EntityUid, Dictionary<HumanoidVisualLayers, List<Marking>>> SaveOrganMarkings(Entity<VisualBodyComponent?> ent)
    {
        var saved = new Dictionary<EntityUid, Dictionary<HumanoidVisualLayers, List<Marking>>>();
        if (!Resolve(ent, ref ent.Comp, false))
            return saved;

        var organContainer = _container.EnsureContainer<Container>(ent, BodyComponent.ContainerID);
        foreach (var organ in organContainer.ContainedEntities)
        {
            if (!TryComp<VisualOrganMarkingsComponent>(organ, out var markingsComp))
                continue;

            var copy = new Dictionary<HumanoidVisualLayers, List<Marking>>();
            foreach (var (layer, marks) in markingsComp.Markings)
                copy[layer] = new List<Marking>(marks);
            saved[organ] = copy;
        }
        return saved;
    }

    /// <summary>
    /// Restores organ markings previously captured by <see cref="SaveOrganMarkings"/>.
    /// Organs not present in <paramref name="saved"/> are cleared.
    /// </summary>
    [PublicAPI]
    public void RestoreOrganMarkings(Entity<VisualBodyComponent?> ent, Dictionary<EntityUid, Dictionary<HumanoidVisualLayers, List<Marking>>> saved)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var organContainer = _container.EnsureContainer<Container>(ent, BodyComponent.ContainerID);
        foreach (var organ in organContainer.ContainedEntities)
        {
            if (!TryComp<VisualOrganMarkingsComponent>(organ, out var markingsComp))
                continue;

            var markings = saved.TryGetValue(organ, out var data)
                ? data
                : new Dictionary<HumanoidVisualLayers, List<Marking>>();
            SetOrganMarkings((organ, markingsComp), markings);
        }
    }

    /// <summary>
    /// Captures each organ's <see cref="OrganProfileData"/> (skin/eye colour, sex) keyed by organ.
    /// Pass to <see cref="RestoreOrganProfiles"/> to undo a disguise's colour changes.
    /// </summary>
    [PublicAPI]
    public Dictionary<EntityUid, OrganProfileData> SaveOrganProfiles(Entity<VisualBodyComponent?> ent)
    {
        var saved = new Dictionary<EntityUid, OrganProfileData>();
        if (!Resolve(ent, ref ent.Comp, false))
            return saved;

        var organContainer = _container.EnsureContainer<Container>(ent, BodyComponent.ContainerID);
        foreach (var organ in organContainer.ContainedEntities)
        {
            if (TryComp<VisualOrganComponent>(organ, out var visualOrgan))
                saved[organ] = visualOrgan.Profile;
        }
        return saved;
    }

    /// <summary>
    /// Restores organ profiles previously captured by <see cref="SaveOrganProfiles"/>.
    /// </summary>
    [PublicAPI]
    public void RestoreOrganProfiles(Dictionary<EntityUid, OrganProfileData> saved)
    {
        foreach (var (organ, profile) in saved)
        {
            if (TryComp<VisualOrganComponent>(organ, out var visualOrgan))
                SetOrganProfile((organ, visualOrgan), profile);
        }
    }

    /// <summary>
    /// Copies each matching organ's <see cref="OrganProfileData"/> (skin/eye colour, sex) from
    /// <paramref name="source"/> to <paramref name="target"/>, matched by organ layer.
    /// </summary>
    [PublicAPI]
    public void CopyOrganProfilesFrom(Entity<BodyComponent?> source, Entity<BodyComponent?> target)
    {
        if (!Resolve(source, ref source.Comp) || !Resolve(target, ref target.Comp))
            return;

        var sourceContainer = _container.EnsureContainer<Container>(source, BodyComponent.ContainerID);
        var targetContainer = _container.EnsureContainer<Container>(target, BodyComponent.ContainerID);

        foreach (var targetOrgan in targetContainer.ContainedEntities)
        {
            if (!TryComp<VisualOrganComponent>(targetOrgan, out var targetVisual))
                continue;

            foreach (var sourceOrgan in sourceContainer.ContainedEntities)
            {
                if (!TryComp<VisualOrganComponent>(sourceOrgan, out var sourceVisual))
                    continue;
                if (!sourceVisual.Layer.Equals(targetVisual.Layer))
                    continue;

                SetOrganProfile((targetOrgan, targetVisual), sourceVisual.Profile);
                break;
            }
        }
    }

    /// <summary>
    /// Sets an organ's <see cref="OrganProfileData"/> and re-applies its colour, mirroring
    /// the logic in OnVisualOrganApplyProfile but for a single explicit organ.
    /// </summary>
    private void SetOrganProfile(Entity<VisualOrganComponent> ent, OrganProfileData profile)
    {
        ent.Comp.Profile = profile;
        Dirty(ent);

        if (ent.Comp.Layer.Equals(HumanoidVisualLayers.Eyes))
            SetOrganColor(ent, profile.EyeColor);
        else
            SetOrganColor(ent, profile.SkinColor);
    }
}
