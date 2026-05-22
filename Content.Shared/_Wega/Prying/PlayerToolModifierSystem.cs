using Content.Shared._Wega.Prying.Components;
using Content.Shared.Prying.Components;

namespace Content.Shared._Wega.Prying.Systems;

public sealed partial class PlayerToolModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerToolModifierComponent, GetPryTimeModifierEvent>(OnPry);
    }

    private void OnPry(Entity<PlayerToolModifierComponent> ent, ref GetPryTimeModifierEvent args)
    {
        args.PryTimeModifier *= ent.Comp.PryTimeMultiplier;
    }
}
