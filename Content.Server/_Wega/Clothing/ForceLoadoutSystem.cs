using Content.Shared.Clothing.Components;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Server.Clothing.Systems;

namespace Content.Shared.Clothing;

public sealed class ForceLoadoutSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly OutfitSystem _outfitSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForceLoadoutComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, ForceLoadoutComponent component, MapInitEvent args)
    {
        Equip(uid, component.StartingGear);
    }

    public void Equip(EntityUid uid, List<ProtoId<StartingGearPrototype>>? startingGear)
    {
        if (startingGear != null && startingGear.Count > 0)
            _outfitSystem.SetOutfit(uid, _random.Pick(startingGear));

        GearEquipped(uid);
    }

    public void GearEquipped(EntityUid uid)
    {
        var ev = new StartingGearEquippedEvent(uid);
        RaiseLocalEvent(uid, ref ev);
    }
}
