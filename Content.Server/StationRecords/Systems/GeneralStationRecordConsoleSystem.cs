using System.Linq;
using Content.Server.NukeOps; // Corvax-Wega-Record
using Content.Server.Popups; // Corvax-Wega-Record
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Components;
using Content.Shared.Access.Components; // Corvax-Wega-Record
using Content.Shared.Inventory; // Corvax-Wega-Record
using Content.Shared.PDA; // Corvax-Wega-Record
using Content.Shared.StationRecords;
using Robust.Server.Audio; // Corvax-Wega-Record
using Robust.Server.GameObjects;
using Robust.Shared.Timing; // Corvax-Wega-Record

namespace Content.Server.StationRecords.Systems;

public sealed class GeneralStationRecordConsoleSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly StationJobsSystem _stationJobsSystem = default!; // Corvax-Wega-Record
    [Dependency] private readonly PopupSystem _popup = default!; // Corvax-Wega-Record
    [Dependency] private readonly InventorySystem _inventory = default!; // Corvax-Wega-Record

    private readonly HashSet<string> _requiredAccessLevels = new HashSet<string> { "Captain", "HeadOfPersonnel" }; // Corvax-Wega-Record
    private bool _war = false; // Corvax-Wega-Record

    public override void Initialize()
    {
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, RecordModifiedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, AfterGeneralRecordCreatedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, RecordRemovedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, AdjustStationJobMsg>(OnAdjustJob); // Corvax-Wega-Record
        SubscribeLocalEvent<WarDeclaredEvent>(OnWarDeclared);

        Subs.BuiEvents<GeneralStationRecordConsoleComponent>(GeneralStationRecordConsoleKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(UpdateUserInterface);
            subs.Event<SelectStationRecord>(OnKeySelected);
            subs.Event<SetStationRecordFilter>(OnFiltersChanged);
            subs.Event<DeleteStationRecord>(OnRecordDelete);
        });
    }

    private void OnRecordDelete(Entity<GeneralStationRecordConsoleComponent> ent, ref DeleteStationRecord args)
    {
        if (!ent.Comp.CanDeleteEntries)
            return;

        var owning = _station.GetOwningStation(ent.Owner);

        if (owning != null)
            _stationRecords.RemoveRecord(new StationRecordKey(args.Id, owning.Value));
        UpdateUserInterface(ent); // Apparently an event does not get raised for this.
    }

    private void UpdateUserInterface<T>(Entity<GeneralStationRecordConsoleComponent> ent, ref T args)
    {
        UpdateUserInterface(ent);
    }

    // TODO: instead of copy paste shitcode for each record console, have a shared records console comp they all use
    // then have this somehow play nicely with creating ui state
    // if that gets done put it in StationRecordsSystem console helpers section :)
    private void OnKeySelected(Entity<GeneralStationRecordConsoleComponent> ent, ref SelectStationRecord msg)
    {
        ent.Comp.ActiveKey = msg.SelectedKey;
        UpdateUserInterface(ent);
    }

    private void OnFiltersChanged(Entity<GeneralStationRecordConsoleComponent> ent, ref SetStationRecordFilter msg)
    {
        if (ent.Comp.Filter == null ||
            ent.Comp.Filter.Type != msg.Type || ent.Comp.Filter.Value != msg.Value)
        {
            ent.Comp.Filter = new StationRecordsFilter(msg.Type, msg.Value);
            UpdateUserInterface(ent);
        }
    }

    // Corvax-Wega-Record-start
    private void OnWarDeclared(ref WarDeclaredEvent ev)
    {
        _war = true;
        Timer.Spawn((int)(40f * 60000), () => { _war = false; });
    }

    private void OnAdjustJob(Entity<GeneralStationRecordConsoleComponent> ent, ref AdjustStationJobMsg msg)
    {
        var user = GetEntity(msg.User);
        var idCard = GetIdCard(user);

        if (idCard is null || !HasRequiredAccess(idCard.Value, msg.JobProto))
        {
            _audio.PlayPvs("/Audio/Effects/Cargo/buzz_sigh.ogg", ent);
            _popup.PopupCursor(Loc.GetString("general-station-record-console-access-denied"), user);
            return;
        }
        else if (_war)
        {
            _audio.PlayPvs("/Audio/Effects/Cargo/buzz_sigh.ogg", ent);
            _popup.PopupCursor(Loc.GetString("general-station-record-console-error"), user);
            return;
        }

        var stationUid = _station.GetOwningStation(ent);
        if (stationUid is not EntityUid station || !_stationJobsSystem.TryGetJobSlot(station, msg.JobProto, out var currentSlots))
            return;

        var commandJobs = new HashSet<string>
        {
            "Captain", "IAA", "BlueShieldOfficer", "ChiefEngineer", "ChiefMedicalOfficer",
            "HeadOfPersonnel", "HeadOfSecurity", "ResearchDirector", "Quartermaster"
        };

        var newSlotCount = currentSlots + msg.Amount;
        if (commandJobs.Contains(msg.JobProto) && currentSlots == 1 && msg.Amount > 0)
        {
            _audio.PlayPvs("/Audio/Effects/Cargo/buzz_sigh.ogg", ent);
            _popup.PopupCursor(Loc.GetString("general-station-record-console-job-slot-limit"), user);
            return;
        }
        else if (msg.Amount > 0 && (currentSlots > 3 || newSlotCount > 3))
        {
            _audio.PlayPvs("/Audio/Effects/Cargo/buzz_sigh.ogg", ent);
            _popup.PopupCursor(Loc.GetString("general-station-record-console-job-slot-limit"), user);
            return;
        }

        _stationJobsSystem.TryAdjustJobSlot(station, msg.JobProto, msg.Amount, clamp: true);
        UpdateUserInterface(ent);
    }

    private EntityUid? GetIdCard(EntityUid senderUid)
    {
        if (!_inventory.TryGetSlotEntity(senderUid, "id", out var idUid))
            return null;

        if (EntityManager.TryGetComponent(idUid, out PdaComponent? pda) && pda.ContainedId != null)
        {
            return pda.ContainedId;
        }

        return idUid;
    }

    private bool HasRequiredAccess(EntityUid idCard, string jobProtoId)
    {
        if (!TryComp<AccessComponent>(idCard, out var accessComp))
            return false;

        var cardAccess = new HashSet<string>(accessComp.Tags.Select(tag => tag.ToString()));
        var centcommAccessJobs = new HashSet<string> { "Captain", "IAA", "BlueShieldOfficer" };
        var highAccessJobs = new HashSet<string>
        {
            "ChiefEngineer", "ChiefMedicalOfficer", "HeadOfPersonnel",
            "HeadOfSecurity", "ResearchDirector", "Quartermaster"
        };

        if (centcommAccessJobs.Contains(jobProtoId))
            return cardAccess.Contains("CentralCommand");
        else if (highAccessJobs.Contains(jobProtoId))
            return cardAccess.Contains("Captain");
        return _requiredAccessLevels.Any(cardAccess.Contains);
    }
    // Corvax-Wega-Record-end

    private void UpdateUserInterface(Entity<GeneralStationRecordConsoleComponent> ent)
    {
        var (uid, console) = ent;
        var owningStation = _station.GetOwningStation(uid);

        if (!TryComp<StationRecordsComponent>(owningStation, out var stationRecords))
        {
            _ui.SetUiState(uid, GeneralStationRecordConsoleKey.Key, new GeneralStationRecordConsoleState());
            return;
        }

        var jobList = _stationJobsSystem.GetJobs(owningStation.Value); // Corvax-Wega-Record

        var listing = _stationRecords.BuildListing((owningStation.Value, stationRecords), console.Filter);

        switch (listing.Count)
        {
            case 0:
                _ui.SetUiState(uid, GeneralStationRecordConsoleKey.Key, new GeneralStationRecordConsoleState());
                return;
            default:
                if (console.ActiveKey == null)
                    console.ActiveKey = listing.Keys.First();
                break;
        }

        if (console.ActiveKey is not { } id)
            return;

        var key = new StationRecordKey(id, owningStation.Value);
        _stationRecords.TryGetRecord<GeneralStationRecord>(key, out var record, stationRecords);

        GeneralStationRecordConsoleState newState = new(id, record, listing, jobList, console.Filter, ent.Comp.CanDeleteEntries); // Corvax-Wega-Record
        _ui.SetUiState(uid, GeneralStationRecordConsoleKey.Key, newState);
    }
}
