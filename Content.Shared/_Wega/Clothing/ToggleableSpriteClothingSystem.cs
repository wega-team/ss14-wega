using Content.Shared.Body.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.DoAfter;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing;

public sealed class ToggleableSpriteClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ToggleableSpriteClothingComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<ToggleableSpriteClothingComponent, GetVerbsEvent<AlternativeVerb>>(AddToggleVerb);

        SubscribeLocalEvent<BodyComponent, ToggleSpriteClothingDoAfterEvent>(OnDoAfter); // Fuck, I'm too lazy to think of something
    }

    private static void OnGetState(EntityUid uid, ToggleableSpriteClothingComponent component, ref ComponentGetState args)
    {
        args.State = new ToggleableSpriteClothingComponentState(component.ActiveSuffix);
    }

    private void AddToggleVerb(Entity<ToggleableSpriteClothingComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;
        if (!args.CanAccess || !args.CanInteract || Transform(entity).ParentUid != user
            || !HasComp<ClothingComponent>(entity))
            return;

        var text = entity.Comp.IsToggled
            ? Loc.GetString("toggleable-clothing-verb-reset")
            : Loc.GetString("toggleable-clothing-verb-toggle");

        AlternativeVerb verb = new()
        {
            Text = text,
            Icon = new SpriteSpecifier.Texture(new("/Textures/_Wega/Interface/VerbIcons/clothing.svg.192.dpi.png")),
            Act = () => ToggleClothing(user, entity)
        };
        args.Verbs.Add(verb);
    }

    public void ToggleClothing(EntityUid user, Entity<ToggleableSpriteClothingComponent> entity)
    {
        var args = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(entity.Comp.DoAfterTime),
            new ToggleSpriteClothingDoAfterEvent(), user, entity)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        _doAfterSystem.TryStartDoAfter(args);
    }

    private void OnDoAfter(Entity<BodyComponent> entity, ref ToggleSpriteClothingDoAfterEvent args)
    {
        if (args.Handled || args.Target == null)
            return;

        if (!TryComp<ToggleableSpriteClothingComponent>(args.Target, out var toggleable))
            return;

        args.Handled = true;
        toggleable.ActiveSuffix = toggleable.IsToggled
            ? string.Empty
            : toggleable.DefaultSuffix;

        _audio.PlayLocal(toggleable.Sound, args.Target.Value, args.Target);
        Dirty(args.Target.Value, toggleable);
    }
}
