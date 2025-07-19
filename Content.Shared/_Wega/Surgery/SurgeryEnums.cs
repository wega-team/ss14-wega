namespace Content.Shared.Surgery;

public enum SurgeryActionType : byte
{
    Empty,
    Cut,
    Retract,
    ClampBleeding,
    DrillThrough,
    HealInternalDamage,
    RemoveOrgan,
    InsertOrgan,
    RemovePart,
    AttachPart,
    Implanting,
    RemoveImplant,
    StoreItem,
    RetrieveItems
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
