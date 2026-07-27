using Content.Shared.Station;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Systems;
using Robust.Client.UserInterface;

namespace Content.Client.StationRecords;

public sealed partial class GeneralStationRecordConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private GeneralStationRecordConsoleWindow? _window;

    [Dependency] private SharedStationSystem _stationSys = default!;
    [Dependency] private StationRecordsSystem _recordsSys = default!;
    public GeneralStationRecordConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GeneralStationRecordConsoleWindow>();
        _window.OnKeySelected += SelectStationRecord;
        _window.OnFiltersChanged += SetStationRecordFilter;
        _window.OnDeleted += id => SendPredictedMessage(new DeleteStationRecord(id));

        Update();
    }

    public override void Update()
    {
        base.Update();

        if (!EntMan.TryGetComponent(Owner, out GeneralStationRecordConsoleComponent? comp))
            return;

        var owningStation = _stationSys.GetOwningStation(Owner);

        if (!EntMan.TryGetComponent(owningStation, out StationRecordsComponent? stationRecords))
            return;

        var listing = _recordsSys.BuildListing((owningStation.Value, stationRecords), comp.Filter);

        GeneralStationRecord? record = null;
        if (comp.ActiveKey != null)
        {
            var key = new StationRecordKey(comp.ActiveKey.Value, owningStation.Value);
            _recordsSys.TryGetRecord(key, out record, stationRecords);
        }

        _window?.UpdateState(comp.ActiveKey, record, listing, comp.Filter, comp.CanDeleteEntries);
    }

    // Corvax-Wega-Record-start
    // private void OnJobsAdd(ButtonEventArgs args)
    // {
    //     if (args.Button.Parent?.Parent is not JobRow row || row.Job == null)
    //         return;

    //     var netEntity = _entityManager.GetNetEntity(_playerManager.LocalSession?.AttachedEntity ?? EntityUid.Invalid);
    //     AdjustStationJobMsg msg = new(netEntity, row.Job, 1);
    //     SendMessage(msg);
    // }

    // private void OnJobsSubtract(ButtonEventArgs args)
    // {
    //     if (args.Button.Parent?.Parent is not JobRow row || row.Job == null)
    //         return;

    //     var netEntity = _entityManager.GetNetEntity(_playerManager.LocalSession?.AttachedEntity ?? EntityUid.Invalid);
    //     AdjustStationJobMsg msg = new(netEntity, row.Job, -1);
    //     SendMessage(msg);
    // }
    // Corvax-Wega-Record-end

    private void SelectStationRecord(uint? key)
    {
        if (!EntMan.TryGetComponent(Owner, out GeneralStationRecordConsoleComponent? comp))
            return;

        comp.ActiveKey = key;
        Update();
    }

    private void SetStationRecordFilter(StationRecordFilterType type, string value)
    {
        if (!EntMan.TryGetComponent(Owner, out GeneralStationRecordConsoleComponent? comp))
            return;

        comp.Filter = new StationRecordsFilter(type, value);
        Update();
    }
}
