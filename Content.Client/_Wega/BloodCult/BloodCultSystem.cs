
using System.Numerics;
using Content.Shared.Blood.Cult;
using Content.Shared.Blood.Cult.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client.Blood.Cult
{
    public sealed class BloodCultSystem : SharedBloodCultSystem
    {
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SpriteSystem _sprite = default!;

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
            if (!_appearance.TryGetData(entity, RuneColorVisuals.Color, out Color color))
                return;

            _sprite.SetColor(entity.Owner, color);
        }

        private void OnRuneAppearanceChanged(Entity<BloodRitualDimensionalRendingComponent> entity, ref AppearanceChangeEvent args)
        {
            if (!_appearance.TryGetData(entity, RuneColorVisuals.Color, out Color color))
                return;

            _sprite.SetColor(entity.Owner, color);
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

            if (_sprite.LayerMapTryGet(uid, PentagramKey.Halo, out _, true))
                return;

            var haloVariant = _random.Next(1, 6);
            var haloState = $"halo{haloVariant}";

            var bounds = _sprite.GetLocalBounds((uid, sprite));
            var adj = bounds.Height / 2 + 1.0f / 32 * 6.0f;

            var layerData = new PrototypeLayerData
            {
                Shader = "unshaded",
                RsiPath = "_Wega/Interface/Misc/bloodcult_halo.rsi",
                State = haloState,
                Offset = new Vector2(0.0f, adj)
            };

            var layer = _sprite.AddLayer(uid, layerData, null);
            _sprite.LayerMapSet(uid, PentagramKey.Halo, layer);
        }

        private void RemoveHalo(EntityUid uid, PentagramDisplayComponent component, ComponentShutdown args)
        {
            if (_sprite.LayerMapTryGet(uid, PentagramKey.Halo, out var layer, true))
            {
                _sprite.RemoveLayer(uid, layer);
            }
        }

        private void OnSoulStoneAppearanceChanged(EntityUid uid, StoneSoulComponent component, ref AppearanceChangeEvent args)
        {
            if (!_appearance.TryGetData(uid, StoneSoulVisuals.HasSoul, out bool hasSoul))
                hasSoul = false;

            _sprite.LayerSetVisible(uid, StoneSoulVisualLayers.Soul, hasSoul);
            if (!hasSoul)
            {
                _sprite.LayerSetVisible(uid, StoneSoulVisualLayers.Base, true);
            }
            else
            {
                _sprite.LayerSetVisible(uid, StoneSoulVisualLayers.Base, false);
            }
        }

        private enum PentagramKey
        {
            Halo
        }
    }
}
