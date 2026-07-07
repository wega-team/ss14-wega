using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Lavaland.Artefacts.Components;
using Content.Shared.Lavaland.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Lavaland.Artefacts.Systems;

public sealed partial class WendigoBloodSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WendigoBloodComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<WendigoBloodComponent, WendigoBloodDoAfterEvent>(OnDoAfter);
    }

    private void OnUseInHand(Entity<WendigoBloodComponent> ent, ref UseInHandEvent args)
    {
        var ev = new DragonBloodDoAfterEvent();
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(5), ev, ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f
        });
    }

    private void OnDoAfter(Entity<WendigoBloodComponent> ent, ref WendigoBloodDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        _action.AddAction(args.User, ent.Comp.EntAction);
        _audio.PlayPredicted(ent.Comp.UseSound, Transform(ent).Coordinates, null);
        _popup.PopupEntity(Loc.GetString($"wendigo-blood-effect"), args.User, args.User);
        args.Handled = true;
        Del(ent);
    }
}
