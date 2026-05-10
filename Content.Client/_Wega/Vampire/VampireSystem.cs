using Content.Client.Administration.Managers;
using Content.Client.Alerts;
using Content.Client.Ghost;
using Content.Shared.Administration;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Vampire;

public sealed class VampireSystem : SharedVampireSystem
{
    [Dependency] private readonly GhostSystem? _ghost = default;
    [Dependency] private readonly IClientAdminManager _admin = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
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
        var userEntity = GetEntity(args.User);
        if (userEntity == _playerManager.LocalEntity)
        {
            if (!TryComp<EyeComponent>(userEntity, out var eyeComponent))
                return;

            eyeComponent.NetSyncEnabled = false;
            _eye.SetDrawFov(userEntity, args.Enabled, eyeComponent);
        }
    }

    // Okey, let's go
    private void GetVampireIcons(Entity<VampireComponent> ent, ref GetStatusIconsEvent args)
    {
        // If the local user is an admin in the ghost?
        if (_admin.HasFlag(AdminFlags.Admin) && _ghost is { IsGhost: true })
        {
            ShowIcon(_prototype.Index(ent.Comp.StatusIcon), ref args);
            return;
        }

        // --- Admins ignore this above and see all vampires ---
        // If he's not the owner of the thralls, we will not see the icon
        if (!HasComp<ThrallOwnerComponent>(ent))
            return;

        var localPlayer = _playerManager.LocalEntity;
        if (localPlayer == ent.Owner) // Is that you?
        {
            ShowIcon(_prototype.Index(ent.Comp.StatusIcon), ref args);
            return;
        }

        // If we're a vampire's servant?
        if (TryComp<ThrallComponent>(localPlayer, out var thrall) && thrall.VampireOwner == ent.Owner)
        {
            ShowIcon(_prototype.Index(ent.Comp.StatusIcon), ref args);
            return;
        }
    }

    private void GetThrallIcons(Entity<ThrallComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<VampireComponent>(ent))
            return;

        // If the local user is an admin in the ghost?
        if (_admin.HasFlag(AdminFlags.Admin) && _ghost is { IsGhost: true })
        {
            ShowIcon(_prototype.Index(ent.Comp.StatusIcon), ref args);
            return;
        }

        var localPlayer = _playerManager.LocalEntity;
        if (localPlayer == ent.Owner) // Is that you?
        {
            ShowIcon(_prototype.Index(ent.Comp.StatusIcon), ref args);
            return;
        }

        // If we are the vampire owner of this servant?
        if (ent.Comp.VampireOwner == localPlayer)
        {
            ShowIcon(_prototype.Index(ent.Comp.StatusIcon), ref args);
            return;
        }

        // If we were another servant of the same vampire owner?
        if (TryComp<ThrallComponent>(localPlayer, out var localThrall)
            && localThrall.VampireOwner == ent.Comp.VampireOwner)
        {
            ShowIcon(_prototype.Index(ent.Comp.StatusIcon), ref args);
            return;
        }
    }

    private void ShowIcon(FactionIconPrototype icon, ref GetStatusIconsEvent args)
        => args.StatusIcons.Add(icon);

    private void OnUpdateAlert(Entity<VampireComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != ent.Comp.BloodAlert)
            return;

        var blood = Math.Clamp(ent.Comp.CurrentBlood.Int(), 0, 9999);

        _sprite.LayerSetRsiState(args.SpriteViewEnt.Owner, VampireVisualLayers.Digit1, $"{(blood / 1000) % 10}");
        _sprite.LayerSetRsiState(args.SpriteViewEnt.Owner, VampireVisualLayers.Digit2, $"{(blood / 100) % 10}");
        _sprite.LayerSetRsiState(args.SpriteViewEnt.Owner, VampireVisualLayers.Digit3, $"{(blood / 10) % 10}");
        _sprite.LayerSetRsiState(args.SpriteViewEnt.Owner, VampireVisualLayers.Digit4, $"{blood % 10}");
    }
}
