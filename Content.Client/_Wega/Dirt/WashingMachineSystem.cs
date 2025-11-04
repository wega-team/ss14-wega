
using Content.Shared.DirtVisuals;
using Robust.Client.GameObjects;

namespace Content.Client.WashingMachine
{
    public sealed class WashingMachineSystem : EntitySystem
    {
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        [Dependency] private readonly SpriteSystem _sprite = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<WashingMachineComponent, AppearanceChangeEvent>(OnAppearanceChanged);
        }

        private void OnAppearanceChanged(EntityUid uid, WashingMachineComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            if (!_appearance.TryGetData(uid, WashingMachineVisuals.IsWashing, out bool washing))
                washing = false;

            _sprite.LayerSetVisible(uid, WashingMachineVisuals.Washing, washing);
        }
    }
}