using Content.Shared.Forensics;

namespace Content.Client.Forensics
{
    public sealed class ForensicScannerBoundUserInterface : BoundUserInterface
    {
        private ForensicScannerMenu? _window;

        public ForensicScannerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new ForensicScannerMenu();
            _window.Print.OnPressed += _ => Print();
            _window.Clear.OnPressed += _ => Clear();
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (_window == null || state is not ForensicScannerBoundUserInterfaceState cast)
                return;

            _window.UpdateState(cast);
        }

        private void Print()
        {
            SendMessage(new ForensicScannerPrintMessage());
        }

        private void Clear()
        {
            SendMessage(new ForensicScannerClearMessage());
        }
    }
}
