using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Roles;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Vampire.Components;

namespace Content.Server.GameTicking.Rules
{
    public sealed class VampireRuleSystem : GameRuleSystem<VampireRuleComponent>
    {
        [Dependency] private readonly AntagSelectionSystem _antag = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<VampireRuleComponent, AfterAntagEntitySelectedEvent>(OnVampireSelected);
            SubscribeLocalEvent<VampireRoleComponent, GetBriefingEvent>(OnVampireBriefing);
        }

        private void OnVampireSelected(Entity<VampireRuleComponent> mindId, ref AfterAntagEntitySelectedEvent args)
        {
            var ent = args.EntityUid;
            _antag.SendBriefing(ent, MakeBriefing(ent), Color.Purple, null);
        }

        private void OnVampireBriefing(Entity<VampireRoleComponent> vampire, ref GetBriefingEvent args)
        {
            var ent = args.Mind.Comp.OwnedEntity;
            if (ent is null)
                return;

            args.Append(MakeBriefing(ent.Value));
        }

        private string MakeBriefing(EntityUid ent)
        {
            var isHuman = HasComp<HumanoidProfileComponent>(ent);
            var briefing = isHuman
                ? Loc.GetString("vampire-role-greeting-human")
                : Loc.GetString("vampire-role-greeting-animal");

            return briefing;
        }

        protected override void AppendRoundEndText(EntityUid uid,
            VampireRuleComponent component,
            GameRuleComponent gameRule,
            ref RoundEndTextAppendEvent args)
        {
            var totalBloodDrank = GetTotalBloodDrankInRound();
            args.AddLine(Loc.GetString("vampires-drank-total-blood", ("bloodAmount", totalBloodDrank)));
        }

        private float GetTotalBloodDrankInRound()
        {
            var totalBloodDrank = 0f;
            foreach (var vampireEntity in EntityQuery<VampireComponent>(true))
                totalBloodDrank += vampireEntity.TotalBloodDrank;

            return totalBloodDrank;
        }
    }
}
