using Content.Server.Chat.Systems;
using Content.Server.Objectives.Systems;
using Content.Shared._Wega.Ninja;
using Content.Shared.Chat;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Forensics.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Content.Shared.Vampire.Components;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.Ninja.Systems;

public sealed partial class NinjaBloodScanMachineSystem : EntitySystem
{
    private const int SlotCount = 3;

    private static readonly HashSet<string> BloodReagentIds = new()
    {
        "Blood",        // human, dwarf, reptilian, felinid, harpy, skrell, etc.
        "InsectBlood",  // moth
        "CopperBlood",  // arachnid
        "AmmoniaBlood", // vox
        "SulfurBlood",  // vulpkanin
        "Sap",          // diona
        "AriralBlood",  // ariral
        "ResomiBlood",  // resomi
    };

    [Dependency] private AppearanceSystem        _appearance       = default!;
    [Dependency] private ChatSystem              _chat             = default!;
    [Dependency] private TransformSystem         _transform        = default!;
    [Dependency] private CodeConditionSystem     _codeCondition    = default!;
    [Dependency] private ContainerSystem         _container        = default!;
    [Dependency] private SharedHandsSystem       _hands            = default!;
    [Dependency] private IGameTiming             _timing           = default!;
    [Dependency] private SharedMindSystem        _mind             = default!;
    [Dependency] private SharedPopupSystem       _popup            = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private UserInterfaceSystem     _ui               = default!;

    // Timings that match SS13 original (see ninja_bloodscan_machine.dm)
    private static readonly TimeSpan ActivationDelay  = TimeSpan.FromSeconds(3); // ACTIVATION → LOADING
    private static readonly TimeSpan ScanSlotDelay    = TimeSpan.FromSeconds(5); // between slots
    private static readonly TimeSpan ResultDelay      = TimeSpan.FromSeconds(3); // CORRECT/WRONG → DEACTIVATION
    private static readonly TimeSpan DeactWrongDelay  = TimeSpan.FromSeconds(3); // DEACTIVATION → idle (wrong path)
    private static readonly TimeSpan DeactCorrectDelay = TimeSpan.FromSeconds(6); // DEACTIVATION → idle (correct path)

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaBloodScanMachineComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NinjaBloodScanMachineComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<NinjaBloodScanMachineComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<NinjaBloodScanMachineComponent, InteractUsingEvent>(OnInteractUsing);

        Subs.BuiEvents<NinjaBloodScanMachineComponent>(NinjaBloodScanUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
            subs.Event<NinjaBloodScanSlotActionMessage>(OnSlotAction);
            subs.Event<NinjaBloodScanScanMessage>(OnScanMessage);
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<NinjaBloodScanMachineComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ScanStage == 0 || comp.NextStageTime == null)
                continue;
            if (_timing.CurTime < comp.NextStageTime.Value)
                continue;
            AdvanceScanStage((uid, comp));
        }
    }

    // ── Init / Shutdown ──────────────────────────────────────────────────────

    private void OnInit(Entity<NinjaBloodScanMachineComponent> ent, ref ComponentInit args)
    {
        for (var i = 0; i < SlotCount; i++)
            ent.Comp.VialSlots[i] = _container.EnsureContainer<ContainerSlot>(
                ent, NinjaBloodScanMachineComponent.SlotIds[i]);
    }

    private void OnShutdown(Entity<NinjaBloodScanMachineComponent> ent, ref ComponentShutdown args)
    {
        EjectAll(ent, ent.Comp);
    }

    // ── World interaction ────────────────────────────────────────────────────

    private void OnActivate(Entity<NinjaBloodScanMachineComponent> ent, ref ActivateInWorldEvent args)
    {
        args.Handled = true;

        if (!HasComp<SpaceNinjaComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("ninja-blood-scan-need-ninja"), ent, args.User);
            return;
        }

        TryRegisterNinja(ent, args.User);
        _ui.OpenUi(ent.Owner, NinjaBloodScanUiKey.Key, args.User);
    }

    private void OnInteractUsing(Entity<NinjaBloodScanMachineComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<SolutionManagerComponent>(args.Used))
            return;

        args.Handled = true;

        if (!HasComp<SpaceNinjaComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("ninja-blood-scan-need-ninja"), ent, args.User);
            return;
        }

        if (ent.Comp.RegisteredNinja == null)
        {
            _popup.PopupEntity(Loc.GetString("ninja-blood-scan-need-register"), ent, args.User);
            return;
        }

        if (ent.Comp.IsScanning)
        {
            _popup.PopupEntity(Loc.GetString("ninja-blood-scan-busy"), ent, args.User);
            return;
        }

        TryInsertItem(ent, ent.Comp, args.Used, args.User);
    }

    // ── BUI ──────────────────────────────────────────────────────────────────

    private void OnUiOpened(Entity<NinjaBloodScanMachineComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent, ent.Comp);
    }

    private void OnSlotAction(Entity<NinjaBloodScanMachineComponent> ent, ref NinjaBloodScanSlotActionMessage args)
    {
        if (ent.Comp.IsScanning)
            return;

        var idx = args.SlotIndex;
        if (idx < 0 || idx >= SlotCount)
            return;

        if (ent.Comp.VialSlots[idx].ContainedEntity != null)
        {
            // Eject
            EjectSlot(ent, ent.Comp, idx);
        }
        else
        {
            // Try insert held item (mirrors SS13 "vial_out" on empty slot)
            var actor = args.Actor;

            if (!_hands.TryGetActiveItem((actor, null), out var held))
                return;

            if (!HasComp<SolutionManagerComponent>(held.Value))
                return;

            TryInsertItem(ent, ent.Comp, held.Value, actor);
        }

        UpdateUi(ent, ent.Comp);
    }

    private void OnScanMessage(Entity<NinjaBloodScanMachineComponent> ent, ref NinjaBloodScanScanMessage args)
    {
        StartScan(ent, ent.Comp);
    }

    // ── Registration ─────────────────────────────────────────────────────────

    private void TryRegisterNinja(Entity<NinjaBloodScanMachineComponent> ent, EntityUid user)
    {
        if (ent.Comp.RegisteredNinja == user)
            return;

        if (!TryGetBloodObjective(user, out _))
        {
            _popup.PopupEntity(Loc.GetString("ninja-blood-scan-no-objective"), ent, user);
            return;
        }

        if (IsObjectiveComplete(user))
        {
            _popup.PopupEntity(Loc.GetString("ninja-blood-scan-already-complete"), ent, user);
            return;
        }

        ent.Comp.RegisteredNinja = user;
        _popup.PopupEntity(
            Loc.GetString("ninja-blood-scan-register-success", ("name", MetaData(user).EntityName)),
            ent, user);
    }

    // ── Container insertion / ejection ────────────────────────────────────────

    private void TryInsertItem(Entity<NinjaBloodScanMachineComponent> ent, NinjaBloodScanMachineComponent comp,
        EntityUid item, EntityUid user)
    {
        if (!TryExtractBloodDna(item, out var dna, out var donorName))
        {
            _popup.PopupEntity(Loc.GetString("ninja-blood-scan-not-blood"), ent, user);
            return;
        }

        for (var i = 0; i < SlotCount; i++)
        {
            if (comp.VialSlots[i].ContainedEntity != null)
                continue;

            if (!_container.Insert(item, comp.VialSlots[i]))
                return;

            comp.ScanResults[i]     = 2; // NotDone
            comp.SlotDonorNames[i]  = donorName;
            comp.SlotDonorDnas[i]   = dna;

            _popup.PopupEntity(
                Loc.GetString("ninja-blood-scan-inserted",
                    ("vial", MetaData(item).EntityName), ("slot", i + 1)),
                ent, user);

            UpdateVisual(ent, comp);
            UpdateUi(ent, comp);
            return;
        }

        _popup.PopupEntity(Loc.GetString("ninja-blood-scan-full"), ent, user);
    }

    private void EjectSlot(Entity<NinjaBloodScanMachineComponent> ent, NinjaBloodScanMachineComponent comp, int idx)
    {
        var contained = comp.VialSlots[idx].ContainedEntity;
        if (contained == null)
            return;

        _container.Remove(contained.Value, comp.VialSlots[idx]);
        _transform.SetCoordinates(contained.Value, Transform(ent).Coordinates);

        comp.ScanResults[idx]    = 2; // NotDone
        comp.SlotDonorNames[idx] = null;
        comp.SlotDonorDnas[idx]  = null;

        UpdateVisual(ent, comp);
    }

    private void EjectAll(Entity<NinjaBloodScanMachineComponent> ent, NinjaBloodScanMachineComponent comp)
    {
        for (var i = 0; i < SlotCount; i++)
            EjectSlot(ent, comp, i);
    }

    // ── Scan sequence ─────────────────────────────────────────────────────────

    private void StartScan(Entity<NinjaBloodScanMachineComponent> ent, NinjaBloodScanMachineComponent comp)
    {
        if (comp.IsScanning)
            return;

        for (var i = 0; i < SlotCount; i++)
        {
            if (comp.VialSlots[i].ContainedEntity == null)
            {
                SayMessage(ent, Loc.GetString("ninja-blood-scan-say-not-enough"));
                return;
            }
        }

        comp.IsScanning    = true;
        comp.ScanStage     = 1;
        comp.ProgressBar   = 0;
        comp.ScannedDnas.Clear();
        comp.NextStageTime = _timing.CurTime + ActivationDelay;

        _appearance.SetData(ent, BloodScanMachineVisuals.State, BloodScanMachineState.Activation);
        UpdateUi(ent, comp);
    }

    private void AdvanceScanStage(Entity<NinjaBloodScanMachineComponent> ent)
    {
        var comp = ent.Comp;

        switch (comp.ScanStage)
        {
            // Stage 1: Activation done → show Loading + scan slot 0
            case 1:
                _appearance.SetData(ent, BloodScanMachineVisuals.State, BloodScanMachineState.Loading);
                SayMessage(ent, Loc.GetString("ninja-blood-scan-say-loading"));
                ScanSlot(ent, comp, 0, out var early0);
                if (early0)
                    GoToWrong(ent, comp);
                else
                {
                    comp.ScanStage     = 2;
                    comp.NextStageTime = _timing.CurTime + ScanSlotDelay;
                }
                UpdateUi(ent, comp);
                break;

            // Stage 2: scan slot 1
            case 2:
                ScanSlot(ent, comp, 1, out var early1);
                if (early1)
                    GoToWrong(ent, comp);
                else
                {
                    comp.ScanStage     = 3;
                    comp.NextStageTime = _timing.CurTime + ScanSlotDelay;
                }
                UpdateUi(ent, comp);
                break;

            // Stage 3: scan slot 2 then evaluate
            case 3:
                ScanSlot(ent, comp, 2, out var early2);
                if (early2)
                {
                    GoToWrong(ent, comp);
                }
                else
                {
                    comp.ProgressBar   = 99;
                    comp.ScanStage     = 4;
                    comp.NextStageTime = _timing.CurTime + ScanSlotDelay;
                }
                UpdateUi(ent, comp);
                break;

            // Stage 4: end evaluation (all 3 slots done)
            case 4:
            {
                var allSuccess = comp.ScanResults[0] == 1 && comp.ScanResults[1] == 1 && comp.ScanResults[2] == 1;
                comp.LastScanSucceeded = allSuccess;

                if (allSuccess)
                {
                    _appearance.SetData(ent, BloodScanMachineVisuals.State, BloodScanMachineState.Correct);
                    comp.ScanStage     = 8; // Correct → Deactivation
                }
                else
                {
                    SayMessage(ent, Loc.GetString("ninja-blood-scan-say-some-failed"));
                    _appearance.SetData(ent, BloodScanMachineVisuals.State, BloodScanMachineState.Wrong);
                    comp.ScanStage     = 6; // Wrong → Deactivation (wrong path)
                }

                comp.NextStageTime = _timing.CurTime + ResultDelay;
                break;
            }

            // Stage 5: (unused — early-fail wrong path skips here)
            // Stage 6: Deactivation after wrong (from end-eval or early fail)
            case 6:
                _appearance.SetData(ent, BloodScanMachineVisuals.State, BloodScanMachineState.Deactivation);
                comp.ScanStage     = 7;
                comp.NextStageTime = _timing.CurTime + DeactWrongDelay;
                break;

            // Stage 7: Idle reset after wrong
            case 7:
                FinishScanFail(ent, comp);
                break;

            // Stage 8: Deactivation after correct
            case 8:
                _appearance.SetData(ent, BloodScanMachineVisuals.State, BloodScanMachineState.Deactivation);
                comp.ScanStage     = 9;
                comp.NextStageTime = _timing.CurTime + DeactCorrectDelay;
                break;

            // Stage 9: Idle reset after correct — apply all effects
            case 9:
                FinishScanSuccess(ent, comp);
                break;
        }
    }

    private void GoToWrong(Entity<NinjaBloodScanMachineComponent> ent, NinjaBloodScanMachineComponent comp)
    {
        _appearance.SetData(ent, BloodScanMachineVisuals.State, BloodScanMachineState.Wrong);
        comp.LastScanSucceeded = false;
        comp.ScanStage         = 6;
        comp.NextStageTime     = _timing.CurTime + ResultDelay;
    }

    private void FinishScanFail(Entity<NinjaBloodScanMachineComponent> ent, NinjaBloodScanMachineComponent comp)
    {
        comp.IsScanning    = false;
        comp.ScanStage     = 0;
        comp.NextStageTime = null;
        comp.ProgressBar   = 0;
        comp.ScannedDnas.Clear();
        // Vials stay in machine (SS13 behavior for wrong path)
        UpdateVisual(ent, comp);
        UpdateUi(ent, comp);
    }

    private void FinishScanSuccess(Entity<NinjaBloodScanMachineComponent> ent, NinjaBloodScanMachineComponent comp)
    {
        comp.IsScanning    = false;
        comp.ScanStage     = 0;
        comp.NextStageTime = null;
        comp.ProgressBar   = 0;
        comp.ScannedDnas.Clear();

        if (comp.RegisteredNinja != null)
        {
            var ninja = comp.RegisteredNinja.Value;
            _codeCondition.SetCompleted(ninja, "CollectBloodObjective");
            GrantVampireProtection(ninja);
            SayMessage(ent, Loc.GetString("ninja-blood-scan-say-success"));
        }

        EjectAll(ent, comp);
        comp.RegisteredNinja = null;

        // Reset scan results to NotDone after success
        for (var i = 0; i < SlotCount; i++)
            comp.ScanResults[i] = 2;

        UpdateVisual(ent, comp);
        UpdateUi(ent, comp);
    }

    // ── Per-slot scan logic ───────────────────────────────────────────────────

    /// <summary>Scans one slot. Sets <paramref name="earlyFail"/> = true for duplicate/no-mind (stops scanning).</summary>
    private void ScanSlot(Entity<NinjaBloodScanMachineComponent> ent, NinjaBloodScanMachineComponent comp,
        int idx, out bool earlyFail)
    {
        earlyFail = false;
        var slot = idx + 1;
        var dna  = comp.SlotDonorDnas[idx];

        // Duplicate check (before adding to list, matching SS13 order)
        if (!string.IsNullOrEmpty(dna) && comp.ScannedDnas.Contains(dna))
        {
            comp.ScanResults[idx] = 0;
            SayMessage(ent, Loc.GetString("ninja-blood-scan-say-duplicate", ("slot", slot)));
            earlyFail = true;
            return;
        }

        // Progress update + add DNA to visited set
        comp.ProgressBar = Math.Clamp(comp.ProgressBar + 30, 0, 90);
        if (!string.IsNullOrEmpty(dna))
            comp.ScannedDnas.Add(dna);

        // No-mind check (null / empty DNA)
        if (string.IsNullOrEmpty(dna))
        {
            comp.ScanResults[idx] = 0;
            SayMessage(ent, Loc.GetString("ninja-blood-scan-say-no-mind", ("slot", slot)));
            earlyFail = true;
            return;
        }

        // Vampire check — non-fatal, just marks the slot
        if (TryFindEntityByDna(dna, out var donor) && HasComp<VampireComponent>(donor))
        {
            comp.ScanResults[idx] = 1; // Success
        }
        else
        {
            comp.ScanResults[idx] = 0; // Failed
            SayMessage(ent, Loc.GetString("ninja-blood-scan-say-not-vampire", ("slot", slot)));
        }
    }

    // ── Vampire protection bonus ──────────────────────────────────────────────

    private void GrantVampireProtection(EntityUid ninja)
    {
        if (HasComp<NinjaVampireProtectionComponent>(ninja))
            return;

        AddComp<NinjaVampireProtectionComponent>(ninja);
        _popup.PopupEntity(Loc.GetString("ninja-blood-scan-vampire-protection-granted"), ninja, ninja);
    }

    // ── Blood / DNA helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Finds a Blood reagent in any solution of <paramref name="container"/> and extracts the donor's DNA.
    /// Accepts containers that contain Blood even alongside other reagents.
    /// </summary>
    private bool TryExtractBloodDna(EntityUid container, out string? dna, out string donorName)
    {
        dna       = null;
        donorName = string.Empty;

        if (!TryComp<SolutionManagerComponent>(container, out var solutionMgr))
            return false;

        foreach (var (_, solutionEnt) in _solutionContainer.EnumerateSolutions((container, solutionMgr)))
        {
            var solution = solutionEnt.Comp.Solution;

            if (solution.Volume <= 0)
                continue;

            string? foundDna = null;
            var hasBlood = false;

            foreach (var (reagent, _) in solution.Contents)
            {
                if (!BloodReagentIds.Contains(reagent.Prototype))
                    continue;

                hasBlood = true;

                if (foundDna == null && reagent.Data != null)
                {
                    foreach (var data in reagent.Data)
                    {
                        if (data is DnaData dnaData)
                        {
                            foundDna = dnaData.DNA;
                            break;
                        }
                    }
                }
            }

            if (!hasBlood)
                continue;

            dna = foundDna;

            if (dna != null && TryFindEntityByDna(dna, out var owner))
                donorName = MetaData(owner).EntityName;

            return true;
        }

        return false;
    }

    private bool TryFindEntityByDna(string dna, out EntityUid entity)
    {
        entity = default;
        var query = EntityQueryEnumerator<DnaComponent>();
        while (query.MoveNext(out var uid, out var dnaComp))
        {
            if (dnaComp.DNA == dna)
            {
                entity = uid;
                return true;
            }
        }
        return false;
    }

    // ── Visuals ───────────────────────────────────────────────────────────────

    private void UpdateVisual(Entity<NinjaBloodScanMachineComponent> ent, NinjaBloodScanMachineComponent comp)
    {
        var filled = 0;
        for (var i = 0; i < SlotCount; i++)
        {
            if (comp.VialSlots[i].ContainedEntity != null)
                filled++;
        }

        var state = filled switch
        {
            1 => BloodScanMachineState.Idle1,
            2 => BloodScanMachineState.Idle2,
            3 => BloodScanMachineState.Idle3,
            _ => BloodScanMachineState.Idle0,
        };

        _appearance.SetData(ent, BloodScanMachineVisuals.State, state);
    }

    // ── UI ────────────────────────────────────────────────────────────────────

    private void UpdateUi(Entity<NinjaBloodScanMachineComponent> ent, NinjaBloodScanMachineComponent comp)
    {
        var scanStates   = (int[])  comp.ScanResults.Clone();
        var donorNames   = (string?[]) comp.SlotDonorNames.Clone();
        var vialEntities = new NetEntity?[SlotCount];

        for (var i = 0; i < SlotCount; i++)
        {
            var contained = comp.VialSlots[i].ContainedEntity;
            vialEntities[i] = contained.HasValue ? GetNetEntity(contained.Value) : (NetEntity?) null;
        }

        _ui.SetUiState(ent.Owner, NinjaBloodScanUiKey.Key,
            new NinjaBloodScanBuiState(scanStates, donorNames, vialEntities, comp.IsScanning, comp.ProgressBar));
    }

    // ── Objective helpers ─────────────────────────────────────────────────────

    private bool TryGetBloodObjective(EntityUid ninja, out EntityUid? objective)
    {
        objective = null;
        if (!TryComp<MindContainerComponent>(ninja, out var container) ||
            _mind.GetMind(ninja, container) is not { } mindId)
            return false;

        return _mind.TryFindObjective(mindId, "CollectBloodObjective", out objective);
    }

    private bool IsObjectiveComplete(EntityUid ninja)
    {
        if (!TryGetBloodObjective(ninja, out var obj) || obj == null)
            return false;

        return _codeCondition.IsCompleted(obj.Value);
    }

    private void SayMessage(EntityUid uid, string message)
    {
        _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak,
            hideChat: false, ignoreActionBlocker: true);
    }
}
