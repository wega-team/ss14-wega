using Content.Shared.Lavaland;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.Lavaland;

public sealed class UtilityVendorBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private UtilityVendorMenu? _window;

    public UtilityVendorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<UtilityVendorMenu>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        _window.OpenCentered();

        _window.OnClose += Close;

        _window.OnPurchase += item => SendMessage(new UtilityVendorPurchaseMessage(item));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is UtilityVendorBoundUserInterfaceState cast)
            _window?.UpdateState(cast);
    }
}
