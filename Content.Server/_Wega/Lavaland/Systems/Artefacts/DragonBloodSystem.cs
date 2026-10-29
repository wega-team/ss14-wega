using Content.Server.Polymorph.Systems;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Lavaland.Artefacts.Components;
using Content.Shared.Lavaland.Components;
using Content.Shared.Lavaland.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.Lavaland.Artefacts.Systems;

public sealed partial class DragonBloodSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DragonBloodComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<DragonBloodComponent, DragonBloodDoAfterEvent>(OnDoAfter);
    }

    private void OnUseInHand(Entity<DragonBloodComponent> ent, ref UseInHandEvent args)
    {
        var ev = new DragonBloodDoAfterEvent();
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(5), ev, ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f
        });
    }

    private void OnDoAfter(Entity<DragonBloodComponent> ent, ref DragonBloodDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        var i = _random.Next(1, 4);
        switch (i)
        {
            case 1:
                _polymorph.PolymorphEntity(args.User, ent.Comp.Skeleton);
                break;
            case 2:
                EnsureComp<LavaWalkingComponent>(args.User);
                break;
            case 3:
                _action.AddAction(args.User, ent.Comp.LowerDrake);
                break;

            default: break;
        }

        _audio.PlayPredicted(ent.Comp.UseSound, Transform(ent).Coordinates, null);
        _popup.PopupEntity(Loc.GetString($"dragon-blood-effect-{i}"), args.User, args.User);
        args.Handled = true;
        Del(ent);
    }
}
