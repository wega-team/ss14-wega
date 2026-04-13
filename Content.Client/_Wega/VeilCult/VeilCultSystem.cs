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
			
			if (_sprite.LayerMapTryGet(uid, "stun", out var stun, true))
                _sprite.RemoveLayer(uid, stun);
			if (_sprite.LayerMapTryGet(uid, "forcepassage", out var passage, true))
                _sprite.RemoveLayer(uid, passage);
			if (_sprite.LayerMapTryGet(uid, "teleportation", out var teleport, true))
                _sprite.RemoveLayer(uid, teleport);
			if (_sprite.LayerMapTryGet(uid, "sealwounds", out var sealwounds, true))
                _sprite.RemoveLayer(uid, sealwounds);
			if (_sprite.LayerMapTryGet(uid, "terraform", out var terraform, true))
                _sprite.RemoveLayer(uid, terraform);
			if (_sprite.LayerMapTryGet(uid, "hidingsclock", out var hidingsclock, true))
                _sprite.RemoveLayer(uid, hidingsclock);
			if (_sprite.LayerMapTryGet(uid, "electricaltouch", out var electricaltouch, true))
                _sprite.RemoveLayer(uid, electricaltouch);
			if (_sprite.LayerMapTryGet(uid, "confusion", out var confusion, true))
                _sprite.RemoveLayer(uid, confusion);
			if (_sprite.LayerMapTryGet(uid, "crusher", out var crusher, true))
                _sprite.RemoveLayer(uid, crusher);
			if (_sprite.LayerMapTryGet(uid, "bloodshed", out var bloodshed, true))
                _sprite.RemoveLayer(uid, bloodshed);
			if (_sprite.LayerMapTryGet(uid, "knockback", out var knockback, true))
                _sprite.RemoveLayer(uid, knockback);
			if (_sprite.LayerMapTryGet(uid, "swordsmen", out var swordsmen, true))
                _sprite.RemoveLayer(uid, swordsmen);
			if (_sprite.LayerMapTryGet(uid, "haste", out var haste, true))
                _sprite.RemoveLayer(uid, haste);
			if (_sprite.LayerMapTryGet(uid, "reflection", out var reflection, true))
                _sprite.RemoveLayer(uid, reflection);
			if (_sprite.LayerMapTryGet(uid, "absorb", out var absorb, true))
                _sprite.RemoveLayer(uid, absorb);
			if (_sprite.LayerMapTryGet(uid, "flash", out var flash, true))
                _sprite.RemoveLayer(uid, flash);
			if (_sprite.LayerMapTryGet(uid, "camouflage", out var camouflage, true))
                _sprite.RemoveLayer(uid, camouflage);
			if (_sprite.LayerMapTryGet(uid, "hardenplates", out var hardenplates, true))
                _sprite.RemoveLayer(uid, hardenplates);
			if (_sprite.LayerMapTryGet(uid, "northstar", out var northstar, true))
                _sprite.RemoveLayer(uid, northstar);
			if (_sprite.LayerMapTryGet(uid, "redflame", out var redflame, true))
                _sprite.RemoveLayer(uid, redflame);
			if (_sprite.LayerMapTryGet(uid, "timestop", out var timestop, true))
                _sprite.RemoveLayer(uid, timestop);
			if (_sprite.LayerMapTryGet(uid, "reconstruction", out var reconstruction, true))
                _sprite.RemoveLayer(uid, reconstruction);
			if (_sprite.LayerMapTryGet(uid, "emp", out var emp, true))
                _sprite.RemoveLayer(uid, emp);
				
		}
    }
}
