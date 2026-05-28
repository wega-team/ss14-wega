using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Shared.Atmos.Components;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Popups;
using Content.Shared.Vampire.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Vampire;

public sealed class HolyPointSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly ProtoId<EmotePrototype> Scream = "Scream";
    private static readonly float FireStackCount = 2.5f;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var holyPointQuery = EntityQueryEnumerator<HolyPointComponent>();
        while (holyPointQuery.MoveNext(out var uid, out var holyPoint))
        {
            if (holyPoint.NextTimeTick <= 0)
            {
                holyPoint.NextTimeTick = 10;
                var vampires = _entityLookup.GetEntitiesInRange<VampireComponent>(Transform(uid).Coordinates, holyPoint.Range);
                foreach (var vampire in vampires)
                {
                    if (HasComp<SupremeVampireComponent>(vampire))
                        continue;

                    if (TryComp(vampire.Owner, out FlammableComponent? flammable))
                    {
                        flammable.FireStacks += FireStackCount;
                        _flammable.Ignite(vampire.Owner, uid);

                        _chat.TryEmoteWithoutChat(vampire, _proto.Index(Scream), true);
                        _popup.PopupEntity(Loc.GetString("vampire-holy-point"), vampire.Owner, vampire.Owner, PopupType.LargeCaution);
                    }
                }
            }
            holyPoint.NextTimeTick -= frameTime;
        }
    }
}
