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

public sealed class DragonBloodSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DragonBloodComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<DragonBloodComponent, DragonBloodDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<BecomeToDrakeActionEvent>(OnBecomeToDrake);
        SubscribeLocalEvent<DrakeReturnBackActionEvent>(OnReturnBack);
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

    private void OnBecomeToDrake(BecomeToDrakeActionEvent args)
    {
        var polymorph = _polymorph.PolymorphEntity(args.Performer, args.LowerDrake);
        if (polymorph == null)
            return;

        _action.AddAction(polymorph.Value, args.ReturnBack);
        args.Handled = true;
    }

    private void OnReturnBack(DrakeReturnBackActionEvent args)
    {
        _action.RemoveAction(args.Performer, args.Action.Owner);
        _polymorph.Revert(args.Performer);
    }
}
