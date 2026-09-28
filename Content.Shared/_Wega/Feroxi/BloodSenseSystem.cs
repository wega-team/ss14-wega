using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;

namespace Content.Shared.Feroxi;

public sealed partial class BloodSenseSystem : EntitySystem
{
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BloodSenseComponent, SwitchBloodSenseActionEvent>(OnSwitchBloodSense);
    }

    private void OnSwitchBloodSense(Entity<BloodSenseComponent> ent, ref SwitchBloodSenseActionEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.ActionEntity = args.Action;

        if (HasComp<BloodSenseActiveComponent>(ent.Owner))
        {
            RemComp<BloodSenseActiveComponent>(ent.Owner);
            _popup.PopupEntity(Loc.GetString("blood-sense-off"), ent.Owner, ent.Owner);
        }
        else
        {
            if (!_mobState.IsAlive(ent.Owner))
            {
                args.Handled = true;
                return;
            }

            if (IsTooThirsty(ent))
            {
                _popup.PopupEntity(Loc.GetString("blood-sense-too-thirsty"), ent.Owner, ent.Owner, PopupType.SmallCaution);
                args.Handled = true;
                return;
            }

            AddComp<BloodSenseActiveComponent>(ent.Owner);
            _popup.PopupEntity(Loc.GetString("blood-sense-on"), ent.Owner, ent.Owner);
        }

        args.Toggle = true;
        args.Handled = true;
    }

    public bool IsTooThirsty(Entity<BloodSenseComponent> ent)
    {
        return TryComp<SatiationComponent>(ent.Owner, out var satiation)
               && _satiation.IsValueInRange((ent.Owner, satiation), SatiationSystem.Thirst, below: ent.Comp.AutoOffThreshold);
    }
}
