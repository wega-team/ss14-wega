using System.Linq;
using Content.Server.Actions;
using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.Blood.Cult;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Shared.Blood.Cult;
using Content.Shared.Blood.Cult.Components;
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
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules
{
    public sealed class BloodCultRuleSystem : GameRuleSystem<BloodCultRuleComponent>
    {
        [Dependency] private readonly ActionsSystem _action = default!;
        [Dependency] private readonly AntagSelectionSystem _antag = default!;
        [Dependency] private readonly BodySystem _body = default!;
        [Dependency] private readonly BloodCultSystem _cult = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly ISharedPlayerManager _player = default!;
        [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
        [Dependency] private readonly MetabolizerSystem _metabolism = default!;
        [Dependency] private readonly MindSystem _mind = default!;
        [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
        [Dependency] private readonly RoleSystem _role = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;

        public readonly ProtoId<NpcFactionPrototype> BloodCultNpcFaction = "BloodCult";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BloodCultRuleComponent, ComponentStartup>(OnRuleStartup);
            SubscribeLocalEvent<BloodCultRuleComponent, AfterAntagEntitySelectedEvent>(OnCultistSelected);

            SubscribeLocalEvent<GodCalledEvent>(OnGodCalled);
            SubscribeLocalEvent<RitualConductedEvent>(OnRitualConducted);

            SubscribeLocalEvent<AutoCultistComponent, ComponentStartup>(OnAutoCultistAdded);
            SubscribeLocalEvent<BloodCultistComponent, ComponentRemove>(OnComponentRemove);
            SubscribeLocalEvent<BloodCultistComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<BloodCultistComponent, EntityZombifiedEvent>(OnOperativeZombified);
        }

        private void OnRuleStartup(EntityUid uid, BloodCultRuleComponent component, ComponentStartup args)
        {
            List<string> gods = new List<string> { "Narsie", "Reaper", "Kharin" };
            component.SelectedGod = gods[new Random().Next(gods.Count)];
            Timer.Spawn(TimeSpan.FromMinutes(1), _cult.SelectRandomTargets);
        }

        private void OnCultistSelected(Entity<BloodCultRuleComponent> mindId, ref AfterAntagEntitySelectedEvent args)
        {
            var ent = args.EntityUid;

            MakeCultist(ent);
            _antag.SendBriefing(ent, MakeBriefing(ent), Color.Red, null);
        }

        private void MakeCultist(EntityUid ent)
        {
            var actionPrototypes = new[]
            {
                BloodCultistComponent.CultObjective,
                BloodCultistComponent.CultCommunication,
                BloodCultistComponent.BloodMagic,
                BloodCultistComponent.RecallBloodDagger
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

            HandleMetabolism(ent);
        }

        private string MakeBriefing(EntityUid ent)
        {
            string selectedGod = Loc.GetString("current-god-narsie");
            var query = QueryActiveRules();
            while (query.MoveNext(out _, out _, out var cult, out _))
            {
                selectedGod = cult.SelectedGod switch
                {
                    "Narsie" => Loc.GetString("current-god-narsie"),
                    "Reaper" => Loc.GetString("current-god-reaper"),
                    "Kharin" => Loc.GetString("current-god-kharin"),
                    _ => Loc.GetString("current-god-narsie")
                };
                break;
            }

            var isHuman = HasComp<HumanoidAppearanceComponent>(ent);
            var briefing = isHuman
                ? Loc.GetString("blood-cult-role-greeting-human", ("god", selectedGod))
                : Loc.GetString("blood-cult-role-greeting-animal", ("god", selectedGod));

            return briefing;
        }

        private void OnAutoCultistAdded(EntityUid uid, AutoCultistComponent comp, ComponentStartup args)
        {
            if (!_mind.TryGetMind(uid, out var mindId, out var mind) || HasComp<BloodCultistComponent>(uid))
            {
                RemComp<AutoCultistComponent>(uid);
                return;
            }

            _npcFaction.AddFaction(uid, BloodCultNpcFaction);
            var culsistComp = EnsureComp<BloodCultistComponent>(uid);
            _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(uid)} converted into a Blood Cult");
            if (mindId == default || !_role.MindHasRole<BloodCultistComponent>(mindId))
                _role.MindAddRole(mindId, "MindRoleBloodCultist");
            if (mind is { UserId: not null } && _player.TryGetSessionById(mind.UserId, out var session))
                _antag.SendBriefing(session, MakeBriefing(uid), Color.Red, new SoundPathSpecifier("/Audio/_Wega/Ambience/Antag/bloodcult_start.ogg"));
            RemComp<AutoCultistComponent>(uid);

            MakeCultist(uid);
            var query = QueryActiveRules();
            while (query.MoveNext(out _, out _, out var cult, out _))
            {
                string selectedDagger = cult.SelectedGod switch
                {
                    "Narsie" => "WeaponBloodDagger",
                    "Reaper" => "WeaponDeathDagger",
                    "Kharin" => "WeaponHellDagger",
                    _ => "WeaponBloodDagger"
                };

                var dagger = _entityManager.SpawnEntity(selectedDagger, Transform(uid).Coordinates);
                culsistComp.RecallDaggerActionEntity = dagger;
                _hands.TryPickupAnyHand(uid, dagger);
                break;
            }
        }

        private void HandleMetabolism(EntityUid cultist)
        {
            if (TryComp<BodyComponent>(cultist, out var bodyComponent))
            {
                foreach (var organ in _body.GetBodyOrgans(cultist, bodyComponent))
                {
                    if (TryComp<MetabolizerComponent>(organ.Id, out var metabolizer))
                    {
                        if (TryComp<StomachComponent>(organ.Id, out _))
                            _metabolism.ClearMetabolizerTypes(metabolizer);

                        _metabolism.TryAddMetabolizerType(metabolizer, "Cultist");
                    }
                }
            }
        }

        protected override void AppendRoundEndText(EntityUid uid,
            BloodCultRuleComponent component,
            GameRuleComponent gameRule,
            ref RoundEndTextAppendEvent args)
        {
            var winText = Loc.GetString($"blood-cult-{component.WinType.ToString().ToLower()}");
            args.AddLine(winText);

            foreach (var cond in component.BloodCultWinCondition)
            {
                var text = Loc.GetString($"blood-cult-cond-{cond.ToString().ToLower()}");
                args.AddLine(text);
            }

            args.AddLine(Loc.GetString("blood-cultist-list-start"));

            var antags = _antag.GetAntagIdentifiers(uid);
            foreach (var (_, sessionData, name) in antags)
            {
                args.AddLine(Loc.GetString("blood-cultist-list-name-user", ("name", name), ("user", sessionData.UserName)));
            }
        }

        private void OnGodCalled(GodCalledEvent ev)
        {
            var query = QueryActiveRules();
            while (query.MoveNext(out _, out _, out var cult, out _))
            {
                if (cult.BloodCultWinCondition.Contains(BloodCultWinType.RitualConducted))
                    cult.BloodCultWinCondition.Remove(BloodCultWinType.RitualConducted);

                cult.WinType = BloodCultWinType.GodCalled;

                if (!cult.BloodCultWinCondition.Contains(BloodCultWinType.GodCalled))
                {
                    cult.BloodCultWinCondition.Add(BloodCultWinType.GodCalled);
                    _roundEndSystem.DoRoundEndBehavior(RoundEndBehavior.ShuttleCall, TimeSpan.FromMinutes(1f));
                }
            }
        }

        private void OnRitualConducted(RitualConductedEvent ev)
        {
            var query = QueryActiveRules();
            while (query.MoveNext(out _, out _, out var cult, out _))
            {
                cult.WinType = BloodCultWinType.RitualConducted;

                if (!cult.BloodCultWinCondition.Contains(BloodCultWinType.RitualConducted))
                    cult.BloodCultWinCondition.Add(BloodCultWinType.RitualConducted);
            }
        }

        private void OnMobStateChanged(EntityUid uid, BloodCultistComponent component, MobStateChangedEvent ev)
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

        private void OnComponentRemove(EntityUid uid, BloodCultistComponent component, ComponentRemove args)
        {
            var query = QueryActiveRules();
            while (query.MoveNext(out var ruleUid, out _, out var cult, out _))
            {
                CheckCultLose(ruleUid, cult);
            }
        }

        private void OnOperativeZombified(EntityUid uid, BloodCultistComponent component, EntityZombifiedEvent args)
        {
            var query = QueryActiveRules();
            while (query.MoveNext(out var ruleUid, out _, out var cult, out _))
            {
                CheckCultLose(ruleUid, cult);
            }
        }

        private void CheckCultLose(EntityUid uid, BloodCultRuleComponent cult)
        {
            var hasLivingCultists = EntityManager.EntityQuery<BloodCultistComponent>().Any();
            if (!hasLivingCultists && !cult.BloodCultWinCondition.Contains(BloodCultWinType.GodCalled)
                && !cult.BloodCultWinCondition.Contains(BloodCultWinType.RitualConducted))
            {
                cult.BloodCultWinCondition.Add(BloodCultWinType.CultLose);
                cult.WinType = BloodCultWinType.CultLose;
            }
        }
    }
}
