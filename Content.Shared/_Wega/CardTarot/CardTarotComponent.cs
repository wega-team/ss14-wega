using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Card.Tarot.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CardTarotComponent : Component
{
    [DataField(required: true)]
    public CardTarot Card = CardTarot.NotEnchanted;

    [DataField]
    public CardTarotType CardType = CardTarotType.Normal;

    public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");
}
