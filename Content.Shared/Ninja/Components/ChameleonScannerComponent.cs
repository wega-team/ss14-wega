using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Placed on the scanner item given to the ninja by the Chameleon module.
/// Scan a humanoid to capture their data, then use on yourself to transform.
/// </summary>
[RegisterComponent]
public sealed partial class ChameleonScannerComponent : Component
{
    /// <summary>The ninja suit entity this scanner was created for.</summary>
    [DataField]
    public EntityUid SuitEntity;

    /// <summary>Seconds required to complete the scan of a target.</summary>
    [DataField]
    public float ScanTime = 2.0f;

    /// <summary>Seconds required to complete the self-transformation.</summary>
    [DataField]
    public float TransformTime = 3.0f;

    /// <summary>The entity whose appearance was captured. Null means no scan has been performed yet.</summary>
    [DataField]
    public EntityUid? ScannedTarget;

    public bool HasScannedTarget => ScannedTarget.HasValue;
}

[Serializable, NetSerializable]
public sealed partial class ChameleonScanDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class ChameleonTransformDoAfterEvent : SimpleDoAfterEvent;
