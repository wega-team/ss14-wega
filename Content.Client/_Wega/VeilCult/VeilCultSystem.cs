using System.Numerics;
using Content.Shared.Veil.Cult;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.Veil.Cult
{
    public sealed class VeilCultSystem : SharedVeilCultSystem
    {
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SpriteSystem _sprite = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<VeilCultistComponent, GetStatusIconsEvent>(GetCultistIcons);
            SubscribeLocalEvent<VeilCogDisplayComponent, ComponentStartup>(GetHalo);
            SubscribeLocalEvent<VeilCogDisplayComponent, ComponentShutdown>(RemoveHalo);
			SubscribeLocalEvent<EnchantedComponent, ComponentRemove>(OnEnchantRemove);
        }

        private void GetCultistIcons(Entity<VeilCultistComponent> ent, ref GetStatusIconsEvent args)
        {
            var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
            args.StatusIcons.Add(iconPrototype);
        }

        private void GetHalo(EntityUid uid, VeilCogDisplayComponent component, ComponentStartup args)
        {
            if (!TryComp<SpriteComponent>(uid, out var sprite))
                return;

            if (_sprite.LayerMapTryGet(uid, CogKey.Halo, out _, true))
                return;

            var haloVariant = _random.Next(1, 6);
            var haloState = $"halo{haloVariant}";

            var bounds = _sprite.GetLocalBounds((uid, sprite));
            var adj = bounds.Height / 2 + 1.0f / 32 * 6.0f;

            var layerData = new PrototypeLayerData
            {
                Shader = "unshaded",
                RsiPath = "_Wega/Interface/Misc/veilcult_cog.rsi",
                State = haloState,
                Offset = new Vector2(0.0f, adj)
            };

            var layer = _sprite.AddLayer(uid, layerData, null);
            _sprite.LayerMapSet(uid, CogKey.Halo, layer);
        }

        private void RemoveHalo(EntityUid uid, VeilCogDisplayComponent component, ComponentShutdown args)
        {
            if (_sprite.LayerMapTryGet(uid, CogKey.Halo, out var layer, true))
                _sprite.RemoveLayer(uid, layer);
        }

        private enum CogKey
        {
            Halo
        }
		
		private void OnEnchantRemove(EntityUid uid, EnchantedComponent comp, ComponentRemove args)
		{
            if (!TryComp<SpriteComponent>(uid, out var sprite))
                return;
			
			if (_sprite.LayerExists(uid, 1))
                _sprite.RemoveLayer(uid, 1);
		}
    }
}
