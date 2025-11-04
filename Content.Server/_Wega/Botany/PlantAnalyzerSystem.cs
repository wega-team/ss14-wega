using System.Linq;
using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.PlantAnalyzer;
using Content.Shared.Botany.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Paper;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Containers;
using Content.Server.PowerCell;
using Content.Shared.Item.ItemToggle;

namespace Content.Server.Botany.Systems;

public sealed class PlantAnalyzerSystem : SharedPlantAnalyzerSystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly LabelSystem _labelSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlantAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<PlantAnalyzerComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerPrintMessage>(OnPrint);
        SubscribeLocalEvent<PlantAnalyzerComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<PlantAnalyzerComponent, DroppedEvent>(OnDropped);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var analyzerQuery = EntityQueryEnumerator<PlantAnalyzerComponent, TransformComponent>();
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

            if (!_cell.HasDrawCharge(uid))
            {
                StopAnalyzingEntity((uid, component));
                continue;
            }

            var targetCoordinates = Transform(target).Coordinates;
            if (!_transform.InRange(targetCoordinates, transform.Coordinates, component.MaxScanRange))
            {
                StopAnalyzingEntity((uid, component));
                continue;
            }

            component.NextUpdate = _gameTiming.CurTime + component.UpdateInterval;
            UpdateScannedUser(uid, target, true);
        }
    }

    private void OnAfterInteract(Entity<PlantAnalyzerComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<PlantHolderComponent>(args.Target) || !_cell.HasDrawCharge(entity, user: args.User))
            return;

        StartScan(entity, args.User, args.Target.Value);
        args.Handled = true;
    }

    private void OnUseInHand(Entity<PlantAnalyzerComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!_cell.HasDrawCharge(entity, user: args.User))
        {
            _popupSystem.PopupEntity(Loc.GetString("plant-analyzer-no-power"), entity, args.User);
            return;
        }

        OpenUserInterface(args.User, entity.Owner);

        if (entity.Comp.ScannedEntity != null)
        {
            UpdateScannedUser(entity.Owner, entity.Comp.ScannedEntity.Value, true);
        }

        args.Handled = true;
    }

    private void OnDoAfter(Entity<PlantAnalyzerComponent> entity, ref PlantAnalyzerDoAfterEvent args)
    {
        var target = args.Target;
        if (args.Cancelled || args.Handled || target == null || !_cell.HasDrawCharge(entity, user: args.User))
            return;

        if (!entity.Comp.Silent)
            _audioSystem.PlayPvs(entity.Comp.ScanningEndSound, entity);

        OpenUserInterface(args.User, entity.Owner);
        BeginAnalyzingEntity(entity, target.Value);
        args.Handled = true;
    }

    private void OnInsertedIntoContainer(Entity<PlantAnalyzerComponent> analyzer, ref EntGotInsertedIntoContainerMessage args)
    {
        if (analyzer.Comp.ScannedEntity is { } _)
            _toggle.TryDeactivate(analyzer.Owner);
    }

    private void OnDropped(Entity<PlantAnalyzerComponent> analyzer, ref DroppedEvent args)
    {
        if (analyzer.Comp.ScannedEntity is { } _)
            _toggle.TryDeactivate(analyzer.Owner);
    }

    private void StartScan(Entity<PlantAnalyzerComponent> analyzer, EntityUid user, EntityUid target)
    {
        _audioSystem.PlayPvs(analyzer.Comp.ScanningBeginSound, analyzer);

        var doAfterArgs = new DoAfterArgs(EntityManager, user, analyzer.Comp.ScanDelay, new PlantAnalyzerDoAfterEvent(), analyzer, target, analyzer)
        {
            NeedHand = true,
            BreakOnMove = true,
        };

        if (!_doAfterSystem.TryStartDoAfter(doAfterArgs))
            return;

        if (target == user || analyzer.Comp.Silent)
            return;

        var msg = Loc.GetString("plant-analyzer-popup-scan-target", ("target", target));
        _popupSystem.PopupEntity(msg, user, user);
    }

    private void OpenUserInterface(EntityUid user, EntityUid analyzer)
    {
        if (!_uiSystem.HasUi(analyzer, PlantAnalyzerUiKey.Key))
            return;

        _uiSystem.OpenUi(analyzer, PlantAnalyzerUiKey.Key, user);
    }

    private void BeginAnalyzingEntity(Entity<PlantAnalyzerComponent> analyzer, EntityUid target)
    {
        analyzer.Comp.ScannedEntity = target;
        analyzer.Comp.NextUpdate = _gameTiming.CurTime;

        _toggle.TryActivate(analyzer.Owner);

        Timer.Spawn(100, () =>
        {
            UpdateScannedUser(analyzer.Owner, target, true);
        });
    }

    private void StopAnalyzingEntity(Entity<PlantAnalyzerComponent> analyzer)
    {
        analyzer.Comp.ScannedEntity = null;

        _toggle.TryDeactivate(analyzer.Owner);

        if (_uiSystem.HasUi(analyzer.Owner, PlantAnalyzerUiKey.Key))
        {
            var actors = _uiSystem.GetActors(analyzer.Owner, PlantAnalyzerUiKey.Key);
            foreach (var actor in actors)
            {
                _uiSystem.CloseUi(analyzer.Owner, PlantAnalyzerUiKey.Key, actor);
            }
        }
    }

    public void UpdateScannedUser(EntityUid analyzer, EntityUid target, bool scanMode)
    {
        if (!_uiSystem.HasUi(analyzer, PlantAnalyzerUiKey.Key))
            return;

        if (!HasComp<PlantHolderComponent>(target))
            return;

        if (!TryComp<PlantAnalyzerComponent>(analyzer, out var analyzerComponent))
            return;

        _uiSystem.ServerSendUiMessage(analyzer, PlantAnalyzerUiKey.Key, GatherData(analyzerComponent, scanMode, target));
    }

    private PlantAnalyzerScannedUserMessage GatherData(PlantAnalyzerComponent analyzer, bool? scanMode = null, EntityUid? target = null)
    {
        PlantAnalyzerPlantData? plantData = null;
        PlantAnalyzerTrayData? trayData = null;
        PlantAnalyzerTolerancesData? tolerancesData = null;
        PlantAnalyzerProduceData? produceData = null;

        if (target != null && TryComp<PlantHolderComponent>(target, out var plantHolder))
        {
            if (plantHolder.Seed is not null)
            {
                plantData = new PlantAnalyzerPlantData(
                    seedDisplayName: plantHolder.Seed.DisplayName,
                    health: plantHolder.Health,
                    endurance: plantHolder.Seed.Endurance,
                    age: plantHolder.Age,
                    lifespan: plantHolder.Seed.Lifespan,
                    maturation: plantHolder.Seed.Maturation,
                    dead: plantHolder.Dead,
                    viable: plantHolder.Seed.Viable,
                    mutating: plantHolder.MutationLevel > 0f,
                    kudzu: plantHolder.Seed.TurnIntoKudzu
                );
                tolerancesData = new PlantAnalyzerTolerancesData(
                    waterConsumption: plantHolder.Seed.WaterConsumption,
                    nutrientConsumption: plantHolder.Seed.NutrientConsumption,
                    toxinsTolerance: plantHolder.Seed.ToxinsTolerance,
                    pestTolerance: plantHolder.Seed.PestTolerance,
                    weedTolerance: plantHolder.Seed.WeedTolerance,
                    lowPressureTolerance: plantHolder.Seed.LowPressureTolerance,
                    highPressureTolerance: plantHolder.Seed.HighPressureTolerance,
                    idealHeat: plantHolder.Seed.IdealHeat,
                    heatTolerance: plantHolder.Seed.HeatTolerance,
                    idealLight: plantHolder.Seed.IdealLight,
                    lightTolerance: plantHolder.Seed.LightTolerance,
                    consumeGasses: [.. plantHolder.Seed.ConsumeGasses.Keys]
                );
                produceData = new PlantAnalyzerProduceData(
                    yield: plantHolder.Seed.ProductPrototypes.Count == 0 ? 0 : _botany.CalculateTotalYield(plantHolder.Seed.Yield, plantHolder.YieldMod),
                    potency: plantHolder.Seed.Potency,
                    chemicals: [.. plantHolder.Seed.Chemicals.Keys],
                    produce: plantHolder.Seed.ProductPrototypes,
                    exudeGasses: [.. plantHolder.Seed.ExudeGasses.Keys],
                    seedless: plantHolder.Seed.Seedless
                );
            }
            trayData = new PlantAnalyzerTrayData(
                waterLevel: plantHolder.WaterLevel,
                nutritionLevel: plantHolder.NutritionLevel,
                toxins: plantHolder.Toxins,
                pestLevel: plantHolder.PestLevel,
                weedLevel: plantHolder.WeedLevel,
                chemicals: plantHolder.SoilSolution?.Comp.Solution.Contents.Select(r => r.Reagent.Prototype).ToList()
            );
        }

        return new PlantAnalyzerScannedUserMessage(
            GetNetEntity(target),
            scanMode,
            plantData,
            trayData,
            tolerancesData,
            produceData,
            analyzer.PrintReadyAt
        );
    }

    private void OnPrint(EntityUid uid, PlantAnalyzerComponent component, PlantAnalyzerPrintMessage args)
    {
        var user = args.Actor;

        if (!_cell.HasDrawCharge(uid, user: user))
        {
            _popupSystem.PopupEntity(Loc.GetString("plant-analyzer-no-power"), uid, user);
            return;
        }

        if (_gameTiming.CurTime < component.PrintReadyAt)
        {
            _popupSystem.PopupEntity(Loc.GetString("plant-analyzer-printer-not-ready"), uid, user);
            return;
        }

        // Spawn a piece of paper.
        var printed = EntityManager.SpawnEntity(component.MachineOutput, Transform(uid).Coordinates);
        _handsSystem.PickupOrDrop(args.Actor, printed, checkActionBlocker: false);

        if (!TryComp<PaperComponent>(printed, out var paperComp))
        {
            Log.Error("Printed paper did not have PaperComponent.");
            return;
        }

        var target = component.ScannedEntity;
        var data = GatherData(component, true, target);

        var missingData = Loc.GetString("plant-analyzer-printout-missing");

        var seedName = data.PlantData is not null ? Loc.GetString(data.PlantData.SeedDisplayName) : null;
        (string, object)[] parameters = [
            ("seedName", seedName ?? missingData),
            ("produce", data.ProduceData is not null ? ProduceToLocalizedStrings(data.ProduceData.Produce).Plural : missingData),
            ("water", data.TolerancesData?.WaterConsumption.ToString("0.00") ?? missingData),
            ("nutrients", data.TolerancesData?.NutrientConsumption.ToString("0.00") ?? missingData),
            ("toxins", data.TolerancesData?.ToxinsTolerance.ToString("0.00") ?? missingData),
            ("pests", data.TolerancesData?.PestTolerance.ToString("0.00") ?? missingData),
            ("weeds", data.TolerancesData?.WeedTolerance.ToString("0.00") ?? missingData),
            ("gasesIn", data.TolerancesData is not null ? GasesToLocalizedStrings(data.TolerancesData.ConsumeGasses) : missingData),
            ("kpa", data.TolerancesData?.IdealPressure.ToString("0.00") ?? missingData),
            ("kpaTolerance", data.TolerancesData?.PressureTolerance.ToString("0.00") ?? missingData),
            ("temp", data.TolerancesData?.IdealHeat.ToString("0.00") ?? missingData),
            ("tempTolerance", data.TolerancesData?.HeatTolerance.ToString("0.00") ?? missingData),
            ("lightLevel", data.TolerancesData?.IdealLight.ToString("0.00") ?? missingData),
            ("lightTolerance", data.TolerancesData?.LightTolerance.ToString("0.00") ?? missingData),
            ("yield", data.ProduceData?.Yield ?? -1),
            ("potency", data.ProduceData is not null ? Loc.GetString(data.ProduceData.Potency) : missingData),
            ("chemicals", data.ProduceData is not null ? ChemicalsToLocalizedStrings(data.ProduceData.Chemicals) : missingData),
            ("gasesOut", data.ProduceData is not null ? GasesToLocalizedStrings(data.ProduceData.ExudeGasses) : missingData),
            ("endurance", data.PlantData?.Endurance.ToString("0.00") ?? missingData),
            ("lifespan", data.PlantData?.Lifespan.ToString("0.00") ?? missingData),
            ("seeds", data.ProduceData is not null ? (data.ProduceData.Seedless ? "no" : "yes") : "other"),
            ("viable", data.PlantData is not null ? (data.PlantData.Viable ? "yes" : "no") : "other"),
            ("kudzu", data.PlantData is not null ? (data.PlantData.Kudzu ? "yes" : "no") : "other")
        ];

        _paperSystem.SetContent((printed, paperComp), Loc.GetString($"plant-analyzer-printout", [.. parameters]));
        _labelSystem.Label(printed, seedName);
        _audioSystem.PlayPvs(component.SoundPrint, uid,
            AudioParams.Default
            .WithVariation(0.25f)
            .WithVolume(3f)
            .WithRolloffFactor(2.8f)
            .WithMaxDistance(4.5f));

        component.PrintReadyAt = _gameTiming.CurTime + component.PrintCooldown;
    }
}
