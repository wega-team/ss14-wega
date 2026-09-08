using Content.Shared.Surgery;
using Content.Shared.Surgery.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.Surgery.Ui;

[UsedImplicitly]
public sealed partial class SurgeryBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SurgeryWindow? _window;

    public SurgeryBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SurgeryWindow>();

        _window.OnStepPressed += (targetNode, stepIndex, isParallel) =>
        {
            SendMessage(new SurgeryStartMessage(targetNode, stepIndex, isParallel));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (EntMan.TryGetComponent(Owner, out OperatedComponent? comp)
            && state is SurgeryProcedureDtoState procedureState)
            _window?.UpdateState(procedureState, comp);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (message is SurgerySterilityUpdateMessage msg)
            _window?.UpdateSterilityToolTip(msg.SterilityInfo);
    }
}
