using Content.Shared.Examine;
using Content.Shared.Ninja.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client.Ninja.Systems;

/// <summary>
/// Renders the ninja fully invisible (alpha 0, no distortion shader) while phase cloak is active.
/// Also blocks examine/context-menu access and hides all status icons (job, SSD, etc.).
/// </summary>
public sealed partial class NinjaCloakClientSystem : EntitySystem
{
    [Dependency] private SpriteSystem   _sprite        = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    private readonly Dictionary<EntityUid, Color> _savedColors = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaCloakActiveComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<NinjaCloakActiveComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<NinjaCloakActiveComponent, ExamineAttemptEvent>(OnExamineAttempt);
        SubscribeLocalEvent<NinjaCloakActiveComponent, GetStatusIconsEvent>(OnGetStatusIcons);
    }

    private void OnStartup(EntityUid uid, NinjaCloakActiveComponent _, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _savedColors[uid] = sprite.Color;

        sprite.PostShader      = null;
        sprite.GetScreenTexture = false;
        sprite.RaiseShaderEvent = false;
        _sprite.SetColor((uid, sprite), Color.Transparent);
    }

    private void OnShutdown(EntityUid uid, NinjaCloakActiveComponent _, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.PostShader      = null;
        sprite.GetScreenTexture = false;
        sprite.RaiseShaderEvent = false;

        var restored = _savedColors.Remove(uid, out var saved) ? saved : Color.White;
        _sprite.SetColor((uid, sprite), restored);
    }

    private void OnExamineAttempt(EntityUid uid, NinjaCloakActiveComponent _, ExamineAttemptEvent args)
    {
        // Allow the ninja to examine themselves
        if (args.Examiner == uid)
            return;

        // Allow the local player (ninja self) to be examined by themselves from within containers
        var local = _playerManager.LocalSession?.AttachedEntity;
        if (local == uid)
            return;

        args.Cancel();
    }

    private void OnGetStatusIcons(EntityUid uid, NinjaCloakActiveComponent _, ref GetStatusIconsEvent args)
    {
        // The ninja themselves always sees their own icons
        var local = _playerManager.LocalSession?.AttachedEntity;
        if (local == uid)
            return;

        args.StatusIcons.Clear();
    }
}
