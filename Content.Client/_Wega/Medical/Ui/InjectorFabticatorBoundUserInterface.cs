using Content.Shared.Injector.Fabticator;
using JetBrains.Annotations;

namespace Content.Client._Wega.Medical.Ui;

[UsedImplicitly]
public sealed class InjectorFabticatorBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private InjectorFabticatorWindow? _window;

    public InjectorFabticatorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = new InjectorFabticatorWindow();
        _window.OnClose += Close;

        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        _window.TransferToBufferPressed += (reagent, amount) =>
            SendMessage(new InjectorFabticatorTransferBeakerToBufferMessage(reagent, amount));
        _window.TransferToBeakerPressed += (reagent, amount) =>
            SendMessage(new InjectorFabticatorTransferBufferToBeakerMessage(reagent, amount));
        _window.EjectButtonPressed += () => SendMessage(new InjectorFabticatorEjectMessage());
        _window.ProduceButtonPressed += (amount, name) =>
            SendMessage(new InjectorFabticatorProduceMessage(amount, name));
        _window.ReagentAdded += (reagent, amount) =>
            SendMessage(new InjectorFabticatorSetReagentMessage(reagent, amount));
        _window.ReagentRemoved += reagent =>
            SendMessage(new InjectorFabticatorRemoveReagentMessage(reagent));

        _window.OpenCenteredLeft();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not InjectorFabticatorBoundUserInterfaceState castState)
            return;

        _window?.UpdateState(castState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _window?.Close();
        _window = null;
    }
}
