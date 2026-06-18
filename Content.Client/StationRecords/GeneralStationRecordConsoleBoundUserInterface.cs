using Content.Shared.StationRecords;
using Robust.Client.UserInterface;
using Robust.Shared.Player; // Corvax-Wega-Record
using static Robust.Client.UserInterface.Controls.BaseButton; // Corvax-Wega-Record

namespace Content.Client.StationRecords;

public sealed partial class GeneralStationRecordConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private GeneralStationRecordConsoleWindow? _window = default!;

    public GeneralStationRecordConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    [Dependency] private IEntityManager _entityManager = default!; // Corvax-Wega-Record
    [Dependency] private ISharedPlayerManager _playerManager = default!; // Corvax-Wega-Record

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GeneralStationRecordConsoleWindow>();
        _window.OnKeySelected += key =>
            SendMessage(new SelectStationRecord(key));
        _window.OnFiltersChanged += (type, filterValue) =>
            SendMessage(new SetStationRecordFilter(type, filterValue));
        _window.OnDeleted += id => SendMessage(new DeleteStationRecord(id));

        _window.OnJobAdd += OnJobsAdd; // Corvax-Wega-Record
        _window.OnJobSubtract += OnJobsSubtract; // Corvax-Wega-Record
    }

    // Corvax-Wega-Record-start
    private void OnJobsAdd(ButtonEventArgs args)
    {
        if (args.Button.Parent?.Parent is not JobRow row || row.Job == null)
            return;

        var netEntity = _entityManager.GetNetEntity(_playerManager.LocalSession?.AttachedEntity ?? EntityUid.Invalid);
        AdjustStationJobMsg msg = new(netEntity, row.Job, 1);
        SendMessage(msg);
    }

    private void OnJobsSubtract(ButtonEventArgs args)
    {
        if (args.Button.Parent?.Parent is not JobRow row || row.Job == null)
            return;

        var netEntity = _entityManager.GetNetEntity(_playerManager.LocalSession?.AttachedEntity ?? EntityUid.Invalid);
        AdjustStationJobMsg msg = new(netEntity, row.Job, -1);
        SendMessage(msg);
    }
    // Corvax-Wega-Record-end

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not GeneralStationRecordConsoleState cast)
            return;

        _window?.UpdateState(cast);
    }
}
