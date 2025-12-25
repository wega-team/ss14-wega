using Content.Client.Alerts;
using Content.Client.Movement.Systems;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Vampire;

public sealed class VampireSystem : SharedVampireSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ContentEyeSystem _contentEye = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<VampireToggleFovEvent>(OnToggleFoV);
        SubscribeLocalEvent<VampireComponent, GetStatusIconsEvent>(GetVampireIcons);
        SubscribeLocalEvent<ThrallComponent, GetStatusIconsEvent>(GetThrallIcons);
        SubscribeLocalEvent<VampireComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
    }

    private void OnToggleFoV(VampireToggleFovEvent args)
    {
        var userEntity = _entityManager.GetEntity(args.User);
        var eyeComponent = _entityManager.GetComponent<EyeComponent>(userEntity);
        if (userEntity == _playerManager.LocalEntity)
            _contentEye.RequestToggleFov(userEntity, eyeComponent);
    }

    private void GetVampireIcons(Entity<VampireComponent> ent, ref GetStatusIconsEvent args)
    {
        var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }

    private void GetThrallIcons(Entity<ThrallComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<VampireComponent>(ent))
            return;

        var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }

    private void OnUpdateAlert(Entity<VampireComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != ent.Comp.BloodAlert)
            return;

        var blood = Math.Clamp(ent.Comp.CurrentBlood.Int(), 0, 999);
        _sprite.LayerSetRsiState(args.SpriteViewEnt.Owner, VampireVisualLayers.Digit1, $"{(blood / 100) % 10}");
        _sprite.LayerSetRsiState(args.SpriteViewEnt.Owner, VampireVisualLayers.Digit2, $"{(blood / 10) % 10}");
        _sprite.LayerSetRsiState(args.SpriteViewEnt.Owner, VampireVisualLayers.Digit3, $"{blood % 10}");
    }
}
