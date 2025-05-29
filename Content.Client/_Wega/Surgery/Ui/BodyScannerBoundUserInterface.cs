using Content.Shared.Surgery;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Wega.Surgery.Ui
{
    [UsedImplicitly]
    public sealed class BodyScannerBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private BodyScannerWindow? _window;

        public BodyScannerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<BodyScannerWindow>();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is BodyScannerBoundUserInterfaceState scannerState)
                _window?.UpdateState(scannerState);
        }
    }
}
