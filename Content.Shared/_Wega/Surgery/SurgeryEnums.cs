namespace Content.Shared.Surgery;

public enum SurgeryActionType : byte
{
    Empty,
    Cut,
    Retract,
    ClampBleeding,
    HealInternalDamage,
    RemoveOrgan,
    InsertOrgan,
    RemovePart,
    AttachPart,
    Implanting
}

public enum SurgeryFailedType : byte
{
    Empty,
    Cut,
    Bleeding,
    Burn,
    Fracture,
    Pain
}
