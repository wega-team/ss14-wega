namespace Content.Shared.Surgery;

public enum SurgeryActionType : byte
{
    Empty,
    Cut,
    Retract,
    ClampBleeding,
    RemoveOrgan,
    InsertOrgan,
    RemovePart,
    AttachPart,
    Implanting
}