using Content.Shared.Injector.Fabticator;
using Robust.Client.GameObjects;

public sealed partial class InjectorFabticatorSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InjectorFabticatorComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(EntityUid uid, InjectorFabticatorComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<bool>(uid, InjectorFabticatorVisuals.IsRunning, out var isRunning, args.Component))
            return;

        _sprite.LayerSetVisible(uid, InjectorFabticatorVisuals.IsRunning, isRunning);
    }
}
