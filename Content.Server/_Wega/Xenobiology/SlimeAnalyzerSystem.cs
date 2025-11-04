
using System.Linq;
using Content.Shared.Xenobiology.Components.Tools;
using Content.Shared.Xenobiology.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Xenobiology.UI;
using Content.Shared.Xenobiology.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Server.PowerCell;
using Robust.Shared.Timing;
using Content.Shared.Interaction.Events;
using Robust.Shared.Containers;

namespace Content.Server.Medical;

public sealed class SlimeAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SlimeAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SlimeAnalyzerComponent, SlimeAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<SlimeAnalyzerComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<SlimeAnalyzerComponent, DroppedEvent>(OnDropped);
    }

    public override void Update(float frameTime)
    {
        var analyzerQuery = EntityQueryEnumerator<SlimeAnalyzerComponent, TransformComponent>();
        while (analyzerQuery.MoveNext(out var uid, out var component, out var transform))
        {
            if (component.NextUpdate > _gameTiming.CurTime)
                continue;

            if (component.ScannedEntity is not { } target)
                continue;

            if (Deleted(target))
            {
                StopAnalyzingEntity((uid, component));
                continue;
            }

            component.NextUpdate = _gameTiming.CurTime + component.UpdateInterval;

            var targetCoordinates = Transform(target).Coordinates;
            if (!_transform.InRange(targetCoordinates, transform.Coordinates, component.MaxScanRange))
            {
                StopAnalyzingEntity((uid, component));
                continue;
            }

            UpdateScannedUser(uid, target);
        }
    }

    private void OnAfterInteract(Entity<SlimeAnalyzerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<SlimeHungerComponent>(args.Target) || !_cell.HasDrawCharge(ent, user: args.User))
            return;

        _audio.PlayPvs(ent.Comp.ScanningBeginSound, ent);

        var doAfterCancelled = !_doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.ScanDelay, new SlimeAnalyzerDoAfterEvent(), ent, target: args.Target, used: ent)
        {
            NeedHand = true,
            BreakOnMove = true,
        });

        if (args.Target == args.User || doAfterCancelled)
            return;

        var msg = Loc.GetString("slime-analyzer-popup-scan-target", ("user", Identity.Entity(args.User, EntityManager)));
        _popup.PopupEntity(msg, args.Target.Value, args.Target.Value);
    }

    private void OnDoAfter(Entity<SlimeAnalyzerComponent> ent, ref SlimeAnalyzerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null || !_cell.HasDrawCharge(ent, user: args.User))
            return;

        _audio.PlayPvs(ent.Comp.ScanningEndSound, ent);

        OpenUserInterface(args.User, ent);
        BeginAnalyzingEntity(ent, args.Target.Value);
        args.Handled = true;
    }

    private void OnInsertedIntoContainer(Entity<SlimeAnalyzerComponent> analyzer, ref EntGotInsertedIntoContainerMessage args)
    {
        if (analyzer.Comp.ScannedEntity is { } _)
            _toggle.TryDeactivate(analyzer.Owner);
    }

    private void OnDropped(Entity<SlimeAnalyzerComponent> analyzer, ref DroppedEvent args)
    {
        if (analyzer.Comp.ScannedEntity is { } _)
            _toggle.TryDeactivate(analyzer.Owner);
    }

    private void OpenUserInterface(EntityUid user, EntityUid analyzer)
    {
        if (!_uiSystem.HasUi(analyzer, SlimeAnalyzerUiKey.Key))
            return;

        _uiSystem.OpenUi(analyzer, SlimeAnalyzerUiKey.Key, user);
    }

    private void BeginAnalyzingEntity(Entity<SlimeAnalyzerComponent> analyzer, EntityUid target)
    {
        analyzer.Comp.ScannedEntity = target;

        _toggle.TryActivate(analyzer.Owner);

        UpdateScannedUser(analyzer, target);
    }

    private void StopAnalyzingEntity(Entity<SlimeAnalyzerComponent> analyzer)
    {
        analyzer.Comp.ScannedEntity = null;

        _toggle.TryDeactivate(analyzer.Owner);

        if (_uiSystem.HasUi(analyzer.Owner, SlimeAnalyzerUiKey.Key))
        {
            var actors = _uiSystem.GetActors(analyzer.Owner, SlimeAnalyzerUiKey.Key);
            foreach (var actor in actors)
            {
                _uiSystem.CloseUi(analyzer.Owner, SlimeAnalyzerUiKey.Key, actor);
            }
        }
    }

    public void UpdateScannedUser(EntityUid analyzer, EntityUid target)
    {
        if (!_uiSystem.HasUi(analyzer, SlimeAnalyzerUiKey.Key))
            return;

        if (!TryComp<SlimeHungerComponent>(target, out var hunger) || !TryComp<SlimeGrowthComponent>(target, out var growth))
            return;

        var state = new SlimeAnalyzerScannedUserMessage(
            GetNetEntity(target),
            hunger.Hunger,
            hunger.MaxHunger,
            hunger.CurrentState,
            growth.CurrentStage,
            growth.SlimeType,
            growth.MutationChance,
            growth.RainbowChance,
            SharedSlimeGrowthSystem.MutationTable.TryGetValue(growth.SlimeType, out var mutations)
                ? mutations.Select(m => (m.type, m.weight)).ToList()
                : null
        );

        _uiSystem.ServerSendUiMessage(analyzer, SlimeAnalyzerUiKey.Key, state);
    }
}
