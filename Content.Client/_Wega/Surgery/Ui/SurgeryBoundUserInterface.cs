using Content.Shared.Surgery;
using Content.Shared.Surgery.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Player;

namespace Content.Client._Wega.Surgery.Ui;

[UsedImplicitly]
public sealed class SurgeryBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    [ViewVariables]
    private SurgeryWindow? _window;

    public SurgeryBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SurgeryWindow>();

        _window.OnStepPressed += (targetNode, stepIndex, isParallel) =>
        {
            var netEntity = EntMan.GetNetEntity(_playerManager.LocalSession?.AttachedEntity ?? EntityUid.Invalid);
            SendMessage(new SurgeryStartMessage(netEntity, targetNode, stepIndex, isParallel));
        };
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (_window == null || message is not SurgeryProcedureDto msg)
            return;

        if (EntMan.TryGetComponent(Owner, out OperatedComponent? comp))
        {
            _window.UpdateState(msg, comp);
        }
    }
}
