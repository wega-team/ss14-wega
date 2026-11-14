using Content.Server.Carrying;
using Content.Shared.Carrying;
using Content.Shared.HangedMan;

namespace Content.Server.HangedMan;

/// <summary>
/// Removes the noose when somebody tries to carry the hanged victim, so they can
/// be picked up. Runs before <see cref="CarryingSystem"/> so the victim is freed
/// (and unanchored) before the actual pickup happens.
/// </summary>
public sealed partial class HangedManCarrySystem : EntitySystem
{
    [Dependency] private SharedHangedManSystem _hangedMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HangedManVictimComponent, CarryDoAfterEvent>(OnCarried,
            before: new[] { typeof(CarryingSystem) });
    }

    private void OnCarried(Entity<HangedManVictimComponent> ent, ref CarryDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        _hangedMan.RemoveNoose(ent);
    }
}
