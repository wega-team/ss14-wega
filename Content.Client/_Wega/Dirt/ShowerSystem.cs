// Content.Client/Shower/ShowerSystem.cs
using Content.Shared.DirtVisuals;
using Robust.Client.GameObjects;

namespace Content.Client.Shower
{
    public sealed class ShowerSystem : EntitySystem
    {
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        [Dependency] private readonly SpriteSystem _sprite = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ShowerComponent, AppearanceChangeEvent>(OnAppearanceChanged);
        }

        private void OnAppearanceChanged(EntityUid uid, ShowerComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            if (!_appearance.TryGetData(uid, ShowerVisuals.Spraying, out bool spraying))
                spraying = false;

            _sprite.LayerSetVisible(uid, ShowerVisuals.Spraying, spraying);
        }
    }
}
