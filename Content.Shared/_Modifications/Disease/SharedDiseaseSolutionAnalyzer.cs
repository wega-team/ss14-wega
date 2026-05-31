// Developed by Nox project.
// Author: KloopRe

using Robust.Shared.Serialization;

namespace Content.Shared._Modifications.Disease;

[Serializable, NetSerializable]
public enum DiseaseSolutionAnalyzerVisuals : byte
{
    Status
}

[Serializable, NetSerializable]
public enum DiseaseSolutionContainerAnalyzerVisuals : byte
{
    Status
}

[Serializable, NetSerializable]
public enum DiseaseSolutionAnalyzerStatus : byte
{
    On,
    Off,
    Scanning,
    Denial,
    Successfully
}

[Serializable, NetSerializable]
public enum DiseaseSolutionContainerAnalyzerStatus : byte
{
    Empty,
    Fill
}
