using Content.Shared.Actions;
using Content.Shared.Dataset;
using Content.Shared.NameIdentifier;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Ninja.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Random.Helpers;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Handles the chameleon module's disguise toggle for ninja borgs.
/// Toggles between the ninja skin and the service-borg disguise skin,
/// and swaps the entity name/description to prevent metagaming.
/// </summary>
public sealed partial class NinjaBorgChameleonSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private NameModifierSystem _nameModifier = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NinjaBorgChameleonComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NinjaBorgChameleonComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<NinjaBorgChameleonComponent, NinjaBorgToggleChameleonEvent>(OnToggle);
    }

    private void OnInit(EntityUid uid, NinjaBorgChameleonComponent comp, ComponentInit args)
    {
        _actions.AddAction(uid, ref comp.ToggleActionEntity, comp.ToggleAction);
    }

    private void OnShutdown(EntityUid uid, NinjaBorgChameleonComponent comp, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, comp.ToggleActionEntity);

        if (!TerminatingOrDeleted(uid))
        {
            RemComp<NinjaBorgChameleonDisguisedComponent>(uid);
            if (comp.IsDisguised)
            {
                EnsureComp<NinjaBorgVisualsComponent>(uid);
                RestoreName(uid, comp);
            }
        }
    }

    private void OnToggle(EntityUid uid, NinjaBorgChameleonComponent comp, NinjaBorgToggleChameleonEvent args)
    {
        comp.IsDisguised = !comp.IsDisguised;

        if (comp.IsDisguised)
        {
            RemComp<NinjaBorgVisualsComponent>(uid);
            EnsureComp<NinjaBorgChameleonDisguisedComponent>(uid);
            ApplyDisguiseName(uid, comp);
        }
        else
        {
            RemComp<NinjaBorgChameleonDisguisedComponent>(uid);
            EnsureComp<NinjaBorgVisualsComponent>(uid);
            RestoreName(uid, comp);
        }

        args.Handled = true;
    }

    private void ApplyDisguiseName(EntityUid uid, NinjaBorgChameleonComponent comp)
    {
        // Save the BASE name (without synth-prefix modifiers) so restoring doesn't double the prefix.
        comp.OriginalName = _nameModifier.GetBaseName((uid, null));
        comp.OriginalDescription = MetaData(uid).EntityDescription;

        // Save and swap synth-ID to make the disguise complete
        if (TryComp<NameIdentifierComponent>(uid, out var nameId))
        {
            comp.OriginalIdentifier = nameId.FullIdentifier;
            // Generate a new random synth-ID (Silicon range is 1000-9999), wrapped in parentheses like the real system does
            var newId = _random.Next(1000, 10000);
            var synthName = Loc.GetString("name-identifier-format-silicon", ("number", newId));
            nameId.FullIdentifier = $"({synthName})";
            Dirty(uid, nameId);
        }

        var dataset = _prototype.Index<LocalizedDatasetPrototype>(comp.DisguisedNameDataset);
        comp.PickedDisguiseName = _random.Pick(dataset);

        // SetEntityName triggers RefreshNameModifiers via EntityRenamedEvent, which applies the new FullIdentifier.
        _meta.SetEntityName(uid, comp.PickedDisguiseName);
        _meta.SetEntityDescription(uid, Loc.GetString(comp.DisguisedDescription));
    }

    private void RestoreName(EntityUid uid, NinjaBorgChameleonComponent comp)
    {
        // Restore original synth-ID before restoring name so the modifier is correct on rename.
        if (comp.OriginalIdentifier != null && TryComp<NameIdentifierComponent>(uid, out var nameId))
        {
            nameId.FullIdentifier = comp.OriginalIdentifier;
            Dirty(uid, nameId);
        }

        if (comp.OriginalName != null)
            _meta.SetEntityName(uid, comp.OriginalName);
        if (comp.OriginalDescription != null)
            _meta.SetEntityDescription(uid, comp.OriginalDescription);

        comp.OriginalName = null;
        comp.OriginalDescription = null;
        comp.OriginalIdentifier = null;
    }
}
