using System.Linq;
using Content.Server.Actions;
using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.Veil.Cult;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Shared.Veil.Cult;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.Body.Components;
using Content.Shared.Clumsy;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Zombies;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules
{
    public sealed class VeilCultRuleSystem : GameRuleSystem<VeilCultRuleComponent>
    {
        [Dependency] private readonly ActionsSystem _action = default!;
        [Dependency] private readonly AntagSelectionSystem _antag = default!;
        [Dependency] private readonly BodySystem _body = default!;
        [Dependency] private readonly VeilCultSystem _cult = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
        [Dependency] private readonly MindSystem _mind = default!;
        [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
        [Dependency] private readonly RoleSystem _role = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;

        public readonly ProtoId<NpcFactionPrototype> VeilCultNpcFaction = "VeilCult";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<VeilCultRuleComponent, ComponentStartup>(OnRuleStartup);
            SubscribeLocalEvent<VeilCultRuleComponent, AfterAntagEntitySelectedEvent>(OnCultistSelected);

            SubscribeLocalEvent<GodCalledEvent>(OnGodCalled);
            SubscribeLocalEvent<RitualConductedEvent>(OnRitualConducted);

            SubscribeLocalEvent<AutoCultistComponent, ComponentStartup>(OnAutoCultistAdded);
            SubscribeLocalEvent<VeilCultistComponent, ComponentRemove>(OnComponentRemove);
            SubscribeLocalEvent<VeilCultistComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<VeilCultistComponent, EntityZombifiedEvent>(OnOperativeZombified);
        }

        private void MakeCultist(EntityUid ent)
        {
            var actionPrototypes = new[]
            {
                VeilCultistComponent.CultObjective,
                VeilCultistComponent.CultCommunication,
                VeilCultistComponent.VeilMagic,
            };

            foreach (var actionPrototype in actionPrototypes)
            {
                _action.AddAction(ent, actionPrototype);
            }

            var componentsToRemove = new[]
            {
                typeof(PacifiedComponent),
                typeof(ClumsyComponent)
            };

            foreach (var compType in componentsToRemove)
            {
                if (HasComp(ent, compType))
                    RemComp(ent, compType);
            }

        }

        private void OnAutoCultistAdded(EntityUid uid, AutoCultistComponent comp, ComponentStartup args)
        {
            if (!_mind.TryGetMind(uid, out var mindId, out var mind) || HasComp<VeilCultistComponent>(uid))
            {
                RemComp<AutoCultistComponent>(uid);
                return;
            }

            _npcFaction.AddFaction(uid, VeilCultNpcFaction);
            var culsistComp = EnsureComp<VeilCultistComponent>(uid);
            _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(uid)} converted into a Veil Cult");
            if (mindId == default || !_role.MindHasRole<VeilCultistComponent>(mindId))
                _role.MindAddRole(mindId, "MindRoleVeilCultist");
            if (mind?.Session != null)
                _antag.SendBriefing(mind.Session, MakeBriefing(uid), Color.Red, new SoundPathSpecifier("/Audio/_Wega/Ambience/Antag/veilcult_start.ogg"));
            RemComp<AutoCultistComponent>(uid);

            MakeCultist(uid);
            var query = QueryActiveRules();

        }

        protected override void AppendRoundEndText(EntityUid uid,
            VeilCultRuleComponent component,
            GameRuleComponent gameRule,
            ref RoundEndTextAppendEvent args)
        {
            var winText = Loc.GetString($"veil-cult-{component.WinType.ToString().ToLower()}");
            args.AddLine(winText);

            foreach (var cond in component.VeilCultWinCondition)
            {
                var text = Loc.GetString($"veil-cult-cond-{cond.ToString().ToLower()}");
                args.AddLine(text);
            }

            args.AddLine(Loc.GetString("veil-cultist-list-start"));

            var antags = _antag.GetAntagIdentifiers(uid);
            foreach (var (_, sessionData, name) in antags)
            {
                args.AddLine(Loc.GetString("veil-cultist-list-name-user", ("name", name), ("user", sessionData.UserName)));
            }
        }

        private void OnGodCalled(GodCalledEvent ev)
        {
            var query = QueryActiveRules();
            while (query.MoveNext(out _, out _, out var cult, out _))
            {
                if (cult.VeilCultWinCondition.Contains(VeilCultWinType.RitualConducted))
                    cult.VeilCultWinCondition.Remove(VeilCultWinType.RitualConducted);

                cult.WinType = VeilCultWinType.GodCalled;

                if (!cult.VeilCultWinCondition.Contains(VeilCultWinType.GodCalled))
                {
                    cult.VeilCultWinCondition.Add(VeilCultWinType.GodCalled);
                    _roundEndSystem.DoRoundEndBehavior(RoundEndBehavior.ShuttleCall, TimeSpan.FromMinutes(1f));
                }
            }
        }

        private void OnRitualConducted(RitualConductedEvent ev)
        {
            var query = QueryActiveRules();
            while (query.MoveNext(out _, out _, out var cult, out _))
            {
                cult.WinType = VeilCultWinType.RitualConducted;

                if (!cult.VeilCultWinCondition.Contains(VeilCultWinType.RitualConducted))
                    cult.VeilCultWinCondition.Add(VeilCultWinType.RitualConducted);
            }
        }

        private void OnMobStateChanged(EntityUid uid, VeilCultistComponent component, MobStateChangedEvent ev)
        {
            if (ev.NewMobState == MobState.Dead)
            {
                var query = QueryActiveRules();
                while (query.MoveNext(out var ruleUid, out _, out var cult, out _))
                {
                    CheckCultLose(ruleUid, cult);
                }
            }
        }

        private void OnComponentRemove(EntityUid uid, VeilCultistComponent component, ComponentRemove args)
        {
            var query = QueryActiveRules();
            while (query.MoveNext(out var ruleUid, out _, out var cult, out _))
            {
                CheckCultLose(ruleUid, cult);
            }
        }

        private void OnOperativeZombified(EntityUid uid, VeilCultistComponent component, EntityZombifiedEvent args)
        {
            var query = QueryActiveRules();
            while (query.MoveNext(out var ruleUid, out _, out var cult, out _))
            {
                CheckCultLose(ruleUid, cult);
            }
        }

        private void CheckCultLose(EntityUid uid, VeilCultRuleComponent cult)
        {
            var hasLivingCultists = EntityManager.EntityQuery<VeilCultistComponent>().Any();
            if (!hasLivingCultists && !cult.VeilCultWinCondition.Contains(VeilCultWinType.GodCalled)
                && !cult.VeilCultWinCondition.Contains(VeilCultWinType.RitualConducted))
            {
                cult.VeilCultWinCondition.Add(VeilCultWinType.CultLose);
                cult.WinType = VeilCultWinType.CultLose;
            }
        }
    }
}
