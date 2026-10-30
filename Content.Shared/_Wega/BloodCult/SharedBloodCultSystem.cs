using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Blood.Cult.Components;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Veil.Cult.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Blood.Cult;

public abstract partial class SharedBloodCultSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private MindLinkSystem _mindLink = default!;
    [Dependency] private IGameTiming _gameTiming = default!;

    private readonly Dictionary<EntityUid, TimeSpan> _lastGunPopup = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultistComponent, ShotAttemptedEvent>(OnShotAttempted); // Corvax-Wega-Testing
        SubscribeLocalEvent<CultAllowedGunComponent, ExaminedEvent>(OnCultAllowedGunExamine);
    }

    #region Deconvertation
    public void CultistDeconvertation(EntityUid cultist)
    {
        if (!TryComp<BloodCultistComponent>(cultist, out var bloodCultist))
            return;

        if (TryComp<ActionsContainerComponent>(cultist, out var actionsContainer))
        {
            foreach (var actionId in actionsContainer.Container.ContainedEntities.ToArray())
            {
                if (!TryComp(actionId, out MetaDataComponent? meta))
                    continue;

                var protoId = meta.EntityPrototype?.ID;
                if (protoId == BloodCultistComponent.BloodMagic.Id
                    || protoId == BloodCultistComponent.RecallBloodDagger.Id)
                {
                    _action.RemoveAction(cultist, actionId);
                }
            }
        }

        _action.RemoveAction(cultist, bloodCultist.RecallSpearActionEntity);
        _action.RemoveAction(cultist, bloodCultist.SelectedSpell);

        foreach (var spell in bloodCultist.SelectedEmpoweringSpells)
            _action.RemoveAction(cultist, spell);

        _mindLink.RemoveChannel(cultist, bloodCultist.CultMindChannel);

        _stun.TryKnockdown(cultist, TimeSpan.FromSeconds(4), true);
        _popup.PopupEntity(Loc.GetString("blood-cult-break-control", ("name", Identity.Entity(cultist, EntityManager))), cultist);

        RemComp<BloodCultistComponent>(cultist);
        RemComp<BloodCultistEyesComponent>(cultist);
        RemComp<BloodPentagramDisplayComponent>(cultist);
    }
    #endregion

    // Corvax-Wega-Testing-start
    // Да я пометил тегами чтобы банально не забыть про это и чо?
    private void OnShotAttempted(Entity<BloodCultistComponent> ent, ref ShotAttemptedEvent args)
    {
        if (HasComp<CultAllowedGunComponent>(args.Used))
            return;

        var now = _gameTiming.CurTime;

        if (!_lastGunPopup.TryGetValue(ent.Owner, out var last) ||
            now - last >= TimeSpan.FromSeconds(1))
        {
            _lastGunPopup[ent.Owner] = now;

            _popup.PopupEntity(
                Loc.GetString("gun-disabled"),
                ent.Owner,
                ent.Owner,
                PopupType.LargeCaution);
        }

        args.Cancel();
    }
    // Corvax-Wega-Testing-end

    private void OnCultAllowedGunExamine(EntityUid uid, CultAllowedGunComponent component, ExaminedEvent args)
    {
        if (!HasComp<BloodCultistComponent>(args.Examiner) && !HasComp<VeilCultistComponent>(args.Examiner))
            return;

        args.PushMarkup(Loc.GetString("cult-allowed-gun-examine"));
    }
}
