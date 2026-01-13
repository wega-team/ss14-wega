using Content.Server.Lavaland.Mobs.Components;
using Content.Shared.Lavaland.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Server.Lavaland;

public sealed class ColossusBossSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ColossusBossComponent, ColossusFractionActionEvent>(OnFractionAction);
        SubscribeLocalEvent<ColossusBossComponent, ColossusCrossActionEvent>(OnCrossAction);
        SubscribeLocalEvent<ColossusBossComponent, ColossusSpriralActionEvent>(OnSpiralAction);
        SubscribeLocalEvent<ColossusBossComponent, ColossusTripleFractionActionEvent>(OnTripleFractionAction);
    }

    private void OnFractionAction(Entity<ColossusBossComponent> ent, ref ColossusFractionActionEvent args)
    {
        args.Handled = true;
    }

    private void OnCrossAction(Entity<ColossusBossComponent> ent, ref ColossusCrossActionEvent args)
    {
        args.Handled = true;
    }

    private void OnSpiralAction(Entity<ColossusBossComponent> ent, ref ColossusSpriralActionEvent args)
    {
        args.Handled = true;
    }

    private void OnTripleFractionAction(Entity<ColossusBossComponent> ent, ref ColossusTripleFractionActionEvent args)
    {
        args.Handled = true;
    }
}
