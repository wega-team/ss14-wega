using Content.Shared.Card.Tarot;
using Content.Shared.Card.Tarot.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Card.Tarot;

public sealed class CardTarotSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CardTarotComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(Entity<CardTarotComponent> entity, ref AppearanceChangeEvent args)
    {
        if (!_appearance.TryGetData(entity, CardTarotVisuals.State, out CardTarot card)
            || !_appearance.TryGetData(entity, CardTarotVisuals.Reversed, out bool reversed))
            return;

        var state = card.ToString().ToLower();
        if (reversed) state += "-reversed";

        _sprite.LayerSetRsiState(entity.Owner, 0, state);
    }
}
