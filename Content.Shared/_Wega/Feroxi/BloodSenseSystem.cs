namespace Content.Shared.Feroxi;

public sealed partial class BloodSenseSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<BloodSenseComponent, SwitchBloodSenseActionEvent>(OnSwitchBloodSense);
    }
    private void OnSwitchBloodSense(Entity<BloodSenseComponent> ent, ref SwitchBloodSenseActionEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<BloodSenseActiveComponent>(ent.Owner))
            RemComp<BloodSenseActiveComponent>(ent.Owner);
        else
            AddComp<BloodSenseActiveComponent>(ent.Owner);

        args.Toggle = true;
        args.Handled = true;
    }
}
