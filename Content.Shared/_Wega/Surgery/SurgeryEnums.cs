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
    Implanting,
    RemoveImplant,
    StoreItem,
    RetrieveItems,
    VeilCultistDeconvertation,
    SurgicalHeal,
    // Synthetic
    Unscrew,
    Screw,
    Pulse,
    Weld,
    CutWire,
    StripWire,
    MendWire,
    Pry,
    Anchor,
    Unanchor,
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
