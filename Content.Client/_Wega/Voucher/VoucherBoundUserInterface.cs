using Content.Shared.Voucher;
using Content.Shared.Voucher.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Wega.Voucher;

[UsedImplicitly]
public sealed class VoucherBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    [ViewVariables]
    private VoucherKitWindow? _window;

    public VoucherBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<VoucherKitWindow>();
        _window.OnKitSelected += kitId => OnKitSelected(kitId);
        _window.OnClose += Close;

        _window.OpenCentered();
    }

    private void OnKitSelected(ProtoId<VoucherKitPrototype> kitId)
    {
        var user = _playerManager.LocalSession?.AttachedEntity ?? EntityUid.Invalid;
        SendMessage(new VoucherKitSelectedMessage(_entMan.GetNetEntity(user), kitId));
        Close();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is VoucherKitSelectionState cast)
            _window?.Populate(cast);
    }
}
