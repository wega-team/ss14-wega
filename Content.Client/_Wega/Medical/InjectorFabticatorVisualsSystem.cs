using Content.Shared.Injector.Fabticator;
using Robust.Client.GameObjects;

public sealed class InjectorFabticatorSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InjectorFabticatorComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(EntityUid uid, InjectorFabticatorComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var sprite = args.Sprite;
        if (!_appearance.TryGetData<bool>(uid, InjectorFabticatorVisuals.IsRunning, out var isRunning, args.Component))
            return;

        sprite.LayerSetVisible(InjectorFabticatorVisuals.IsRunning, isRunning);
    }
}