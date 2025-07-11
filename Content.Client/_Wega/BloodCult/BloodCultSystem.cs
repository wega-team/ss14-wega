
using System.Numerics;
using Content.Shared.Blood.Cult;
using Content.Shared.Blood.Cult.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Blood.Cult
{
    public sealed class BloodCultSystem : SharedBloodCultSystem
    {
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BloodRuneComponent, AppearanceChangeEvent>(OnRuneAppearanceChanged);
            SubscribeLocalEvent<BloodRitualDimensionalRendingComponent, AppearanceChangeEvent>(OnRuneAppearanceChanged);
            SubscribeLocalEvent<BloodCultistComponent, GetStatusIconsEvent>(GetCultistIcons);
            SubscribeLocalEvent<PentagramDisplayComponent, ComponentStartup>(GetHalo);
            SubscribeLocalEvent<PentagramDisplayComponent, ComponentShutdown>(RemoveHalo);
            SubscribeLocalEvent<StoneSoulComponent, AppearanceChangeEvent>(OnSoulStoneAppearanceChanged);
        }

        private void OnRuneAppearanceChanged(Entity<BloodRuneComponent> entity, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            var sprite = args.Sprite;
            if (!_appearance.TryGetData(entity, RuneColorVisuals.Color, out Color color))
                return;

            sprite.Color = color;
        }

        private void OnRuneAppearanceChanged(Entity<BloodRitualDimensionalRendingComponent> entity, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            var sprite = args.Sprite;
            if (!_appearance.TryGetData(entity, RuneColorVisuals.Color, out Color color))
                return;

            sprite.Color = color;
        }

        private void GetCultistIcons(Entity<BloodCultistComponent> ent, ref GetStatusIconsEvent args)
        {
            var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
            args.StatusIcons.Add(iconPrototype);
        }

        private void GetHalo(EntityUid uid, PentagramDisplayComponent component, ComponentStartup args)
        {
            if (!TryComp<SpriteComponent>(uid, out var sprite))
                return;

            if (sprite.LayerMapTryGet(PentagramKey.Halo, out _))
                return;

            var haloVariant = new Random().Next(1, 6);
            var haloState = $"halo{haloVariant}";

            var adj = sprite.Bounds.Height / 2 + 1.0f / 32 * 6.0f;
            var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(new ResPath("_Wega/Interface/Misc/bloodcult_halo.rsi"), haloState));
            sprite.LayerMapSet(PentagramKey.Halo, layer);

            sprite.LayerSetOffset(layer, new Vector2(0.0f, adj));
            sprite.LayerSetShader(layer, "unshaded");
        }

        private void RemoveHalo(EntityUid uid, PentagramDisplayComponent component, ComponentShutdown args)
        {
            if (!TryComp<SpriteComponent>(uid, out var sprite))
                return;

            if (sprite.LayerMapTryGet(PentagramKey.Halo, out var layer))
            {
                sprite.RemoveLayer(layer);
            }
        }

        private void OnSoulStoneAppearanceChanged(EntityUid uid, StoneSoulComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            var sprite = args.Sprite;
            if (!_appearance.TryGetData(uid, StoneSoulVisuals.HasSoul, out bool hasSoul))
                hasSoul = false;

            sprite.LayerSetVisible(StoneSoulVisualLayers.Soul, hasSoul);
            if (!hasSoul)
            {
                sprite.LayerSetVisible(StoneSoulVisualLayers.Base, true);
            }
            else
            {
                sprite.LayerSetVisible(StoneSoulVisualLayers.Base, false);
            }
        }

        private enum PentagramKey
        {
            Halo
        }
    }
}
