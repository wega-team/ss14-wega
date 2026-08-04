using Content.Client.Silicons.Borgs;
using Content.Shared.Ninja.Components;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace Content.Client.Ninja.Systems;

/// <summary>
/// Swaps a borg chassis between its original skin, the ninja skin, and the service-borg disguise skin.
/// </summary>
public sealed partial class NinjaBorgVisualizerSystem : EntitySystem
{
    [Dependency] private BorgSystem _borgSystem = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private static readonly ResPath NinjaRsi = new("_Wega/Mobs/Silicon/ninja.rsi");
    private static readonly ResPath ChassisRsi = new("Mobs/Silicon/chassis.rsi");

    // Cached original Body/Light layer states + borg mind states, keyed by entity.
    // Shared between ninja and chameleon disguise — they are always mutually exclusive.
    private readonly Dictionary<EntityUid, (RSI.StateId Body, RSI.StateId Light, string HasMind, string NoMind)> _cache = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NinjaBorgVisualsComponent, ComponentStartup>(OnNinjaStartup);
        SubscribeLocalEvent<NinjaBorgVisualsComponent, ComponentRemove>(OnNinjaRemoved);
        SubscribeLocalEvent<NinjaBorgChameleonDisguisedComponent, ComponentStartup>(OnDisguisedStartup);
        SubscribeLocalEvent<NinjaBorgChameleonDisguisedComponent, ComponentRemove>(OnDisguisedRemoved);
    }

    // ── Ninja skin ──────────────────────────────────────────────────────────

    private void OnNinjaStartup(EntityUid uid, NinjaBorgVisualsComponent comp, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        CacheIfNeeded(uid, sprite);

        if (TryComp<BorgChassisComponent>(uid, out var borg))
            _borgSystem.SetMindStates((uid, borg), "ninja_e", "ninja_e");

        _sprite.LayerSetRsi((uid, sprite), BorgVisualLayers.Body, NinjaRsi, "ninja");
        _sprite.LayerSetRsi((uid, sprite), BorgVisualLayers.Light, NinjaRsi, "ninja_e");
        _sprite.LayerSetVisible((uid, sprite), BorgVisualLayers.Light, true);
    }

    // ── Service borg disguise skin ──────────────────────────────────────────

    private void OnDisguisedStartup(EntityUid uid, NinjaBorgChameleonDisguisedComponent comp, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        CacheIfNeeded(uid, sprite);

        if (TryComp<BorgChassisComponent>(uid, out var borg))
            _borgSystem.SetMindStates((uid, borg), "service_e", "service_e_r");

        _sprite.LayerSetRsi((uid, sprite), BorgVisualLayers.Body, ChassisRsi, "service");
        _sprite.LayerSetRsi((uid, sprite), BorgVisualLayers.Light, ChassisRsi, "service_e");
        _sprite.LayerSetVisible((uid, sprite), BorgVisualLayers.Light, true);
    }

    // ── Shared restore ──────────────────────────────────────────────────────

    private void OnNinjaRemoved(EntityUid uid, NinjaBorgVisualsComponent comp, ComponentRemove args)
        => RestoreFromCache(uid);

    private void OnDisguisedRemoved(EntityUid uid, NinjaBorgChameleonDisguisedComponent comp, ComponentRemove args)
        => RestoreFromCache(uid);

    private void RestoreFromCache(EntityUid uid)
    {
        if (!_cache.TryGetValue(uid, out var saved))
            return;

        _cache.Remove(uid);

        if (TerminatingOrDeleted(uid))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.LayerSetRsi((uid, sprite), BorgVisualLayers.Body, (RSI?) null, saved.Body);
        _sprite.LayerSetRsi((uid, sprite), BorgVisualLayers.Light, (RSI?) null, saved.Light);

        if (TryComp<BorgChassisComponent>(uid, out var borgComp))
            _borgSystem.SetMindStates((uid, borgComp), saved.HasMind, saved.NoMind);
    }

    // ── Cache helper ────────────────────────────────────────────────────────

    private void CacheIfNeeded(EntityUid uid, SpriteComponent sprite)
    {
        if (_cache.ContainsKey(uid))
            return;

        if (!_sprite.LayerMapTryGet((uid, sprite), BorgVisualLayers.Body, out var bodyIdx, false) ||
            !_sprite.LayerMapTryGet((uid, sprite), BorgVisualLayers.Light, out var lightIdx, false))
            return;

        var hasMind = string.Empty;
        var noMind = string.Empty;
        if (TryComp<BorgChassisComponent>(uid, out var borgComp))
        {
            hasMind = borgComp.HasMindState;
            noMind = borgComp.NoMindState;
        }

        _cache[uid] = (
            _sprite.LayerGetRsiState((uid, sprite), bodyIdx),
            _sprite.LayerGetRsiState((uid, sprite), lightIdx),
            hasMind,
            noMind
        );
    }
}
