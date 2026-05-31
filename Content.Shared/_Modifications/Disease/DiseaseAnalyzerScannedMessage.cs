// Developed by Nox project.
// Author: KloopRe

using Robust.Shared.Serialization;

namespace Content.Shared._Modifications.Disease;

[Serializable, NetSerializable]
public sealed class DiseaseAnalyzerScannedMessage : BoundUserInterfaceMessage
{
    public DiseaseAnalyzerUiState State;

    public DiseaseAnalyzerScannedMessage(DiseaseAnalyzerUiState state)
    {
        State = state;
    }
}

[Serializable, NetSerializable]
public struct DiseaseAnalyzerUiState
{
    public readonly NetEntity? TargetEntity;
    public bool IsInfected;
    public string? StrainId;
    public float CureProgress;
    public int SymptomCount;

    public DiseaseAnalyzerUiState() {}

    public DiseaseAnalyzerUiState(NetEntity? targetEntity, bool isInfected, string? strainId, float cureProgress, int symptomCount)
    {
        TargetEntity = targetEntity;
        IsInfected = isInfected;
        StrainId = strainId;
        CureProgress = cureProgress;
        SymptomCount = symptomCount;
    }
}
