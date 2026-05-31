// Developed by Nox project.
// Author: KloopRe

using Content.Server._Modifications.Disease.Components;
using Content.Shared._Modifications.Disease;
using Content.Shared._Modifications.Disease.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Modifications.Disease.Systems;

public sealed partial class DiseaseAnalyzerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private PowerCellSystem _cell = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private UserInterfaceSystem _uiSystem = default!;
    [Dependency] private TransformSystem _transformSystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DiseaseAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DiseaseAnalyzerComponent, DiseaseAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<DiseaseAnalyzerComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<DiseaseAnalyzerComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<DiseaseAnalyzerComponent, DroppedEvent>(OnDropped);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<DiseaseAnalyzerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var transform))
        {
            if (comp.NextUpdate > _timing.CurTime)
                continue;

            if (comp.ScannedEntity is not {} patient)
                continue;

            if (Deleted(patient))
            {
                StopAnalyzingEntity((uid, comp), patient);
                continue;
            }

            comp.NextUpdate = _timing.CurTime + comp.UpdateInterval;

            var patientCoordinates = Transform(patient).Coordinates;
            if (comp.MaxScanRange != null && !_transformSystem.InRange(patientCoordinates, transform.Coordinates, comp.MaxScanRange.Value))
            {
                PauseAnalyzingEntity((uid, comp), patient);
                continue;
            }

            comp.IsAnalyzerActive = true;
            UpdateScannedUser(uid, patient);
        }
    }

    private void OnAfterInteract(Entity<DiseaseAnalyzerComponent> uid, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<MobStateComponent>(args.Target) || !_cell.HasDrawCharge(uid.Owner, user: args.User))
            return;

        _audio.PlayPvs(uid.Comp.ScanningBeginSound, uid);

        var doAfterCancelled = !_doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, uid.Comp.ScanDelay, new DiseaseAnalyzerDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            NeedHand = true,
            BreakOnMove = true,
        });

        if (args.Target == args.User || doAfterCancelled || uid.Comp.Silent)
            return;

        var msg = Loc.GetString("disease-analyzer-popup-scan-target", ("user", Identity.Entity(args.User, EntityManager)));
        _popupSystem.PopupEntity(msg, args.Target.Value, args.Target.Value, PopupType.Medium);
    }

    private void OnDoAfter(Entity<DiseaseAnalyzerComponent> uid, ref DiseaseAnalyzerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null || !_cell.HasDrawCharge(uid.Owner, user: args.User))
            return;

        if (!uid.Comp.Silent)
            _audio.PlayPvs(uid.Comp.ScanningEndSound, uid);

        OpenUserInterface(args.User, uid);
        BeginAnalyzingEntity(uid, args.Target.Value);
        args.Handled = true;
    }

    private void OnInsertedIntoContainer(Entity<DiseaseAnalyzerComponent> uid, ref EntGotInsertedIntoContainerMessage args)
    {
        if (uid.Comp.ScannedEntity is { })
            _toggle.TryDeactivate(uid.Owner);
    }

    private void OnToggled(Entity<DiseaseAnalyzerComponent> ent, ref ItemToggledEvent args)
    {
        if (!args.Activated && ent.Comp.ScannedEntity is { } patient)
            StopAnalyzingEntity(ent, patient);
    }

    private void OnDropped(Entity<DiseaseAnalyzerComponent> uid, ref DroppedEvent args)
    {
        if (uid.Comp.ScannedEntity is { })
            _toggle.TryDeactivate(uid.Owner);
    }

    private void OpenUserInterface(EntityUid user, EntityUid analyzer)
    {
        if (!_uiSystem.HasUi(analyzer, DiseaseAnalyzerUiKey.Key))
            return;

        _uiSystem.OpenUi(analyzer, DiseaseAnalyzerUiKey.Key, user);
    }

    private void BeginAnalyzingEntity(Entity<DiseaseAnalyzerComponent> analyzer, EntityUid target)
    {
        analyzer.Comp.ScannedEntity = target;
        _toggle.TryActivate(analyzer.Owner);
        UpdateScannedUser(analyzer, target);
    }

    private void StopAnalyzingEntity(Entity<DiseaseAnalyzerComponent> analyzer, EntityUid target)
    {
        analyzer.Comp.ScannedEntity = null;
        _toggle.TryDeactivate(analyzer.Owner);
        UpdateScannedUser(analyzer, target);
    }

    private void PauseAnalyzingEntity(Entity<DiseaseAnalyzerComponent> analyzer, EntityUid target)
    {
        if (!analyzer.Comp.IsAnalyzerActive)
            return;

        UpdateScannedUser(analyzer, target);
        analyzer.Comp.IsAnalyzerActive = false;
    }

    public void UpdateScannedUser(EntityUid analyzer, EntityUid target)
    {
        if (!_uiSystem.HasUi(analyzer, DiseaseAnalyzerUiKey.Key))
            return;

        var state = GetDiseaseAnalyzerUiState(target);

        _uiSystem.ServerSendUiMessage(
            analyzer,
            DiseaseAnalyzerUiKey.Key,
            new DiseaseAnalyzerScannedMessage(state)
        );
    }

    private DiseaseAnalyzerUiState GetDiseaseAnalyzerUiState(EntityUid target)
    {
        if (!TryComp<DiseaseComponent>(target, out var disease))
        {
            return new DiseaseAnalyzerUiState(
                GetNetEntity(target),
                false,
                null,
                0f,
                0
            );
        }

        var progress = disease.Data.MaxThreshold > 0f
            ? disease.Data.Threshold / disease.Data.MaxThreshold
            : 0f;

        return new DiseaseAnalyzerUiState(
            GetNetEntity(target),
            true,
            disease.Data.StrainId,
            1f - progress,
            disease.Data.ActiveSymptom.Count
        );
    }
}
