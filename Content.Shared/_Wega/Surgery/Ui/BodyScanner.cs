using Robust.Shared.Serialization;

namespace Content.Shared.Surgery
{
    [Serializable, NetSerializable]
    public enum BodyScannerUiKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public sealed class BodyScannerBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string? PatientName;
        public readonly string? PatientStatus;
        public readonly List<BodyScannerDamageInfo>? Damages;
        public readonly bool ScannerConnected;

        public BodyScannerBoundUserInterfaceState(
            string? patientName,
            string? patientStatus,
            List<BodyScannerDamageInfo>? damages,
            bool scannerConnected)
        {
            PatientName = patientName;
            PatientStatus = patientStatus;
            Damages = damages;
            ScannerConnected = scannerConnected;
        }
    }

    [Serializable, NetSerializable]
    public sealed class BodyScannerDamageInfo
    {
        public readonly string DamageName;
        public readonly List<string> AffectedParts;

        public BodyScannerDamageInfo(string damageName, List<string> affectedParts)
        {
            DamageName = damageName;
            AffectedParts = affectedParts;
        }
    }
}
