using Robust.Shared.GameStates;

namespace Content.Shared.Surgery.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class BodyScannerConsoleComponent : Component
    {
        public const string ScannerPort = "SurgeryTableSender";

        [ViewVariables]
        public EntityUid? SurgeryTable = null;

        [DataField("maxDistance")]
        public float MaxDistance = 2f;

        public bool SurgeryTableInRange = true;

        public TimeSpan NextUpdate;

        public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);
    }
}