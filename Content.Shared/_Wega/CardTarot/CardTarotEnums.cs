using Robust.Shared.Serialization;

namespace Content.Shared.Card.Tarot;

[Serializable, NetSerializable]
public enum CardTarotType : byte
{
    Normal,
    Reversed
}

[Serializable, NetSerializable]
public enum CardTarot : byte
{
    NotEnchanted,
    Fool,
    Magician,
    HighPriestess,
    Empress,
    Emperor,
    Hierophant,
    Lovers,
    Chariot,
    Justice,
    Hermit,
    WheelOfFortune,
    Strength,
    HangedMan,
    Death,
    Temperance,
    Devil,
    Tower,
    Stars,
    Moon,
    Sun,
    Judgement,
    TheWorld // !!!!
}

[Serializable, NetSerializable]
public enum CardTarotVisuals : byte
{
    State,
    Reversed
}
