using System.Linq;
using Content.Server.Actions;
using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.Bed.Cryostorage;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Shared.Achievements;
using Content.Shared.Blood.Cult;
using Content.Shared.Blood.Cult.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Clumsy;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Metabolism;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Zombies;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules
{
    public sealed class BloodCultRuleSystem : GameRuleSystem<BloodCultRuleComponent>
    {
        [Dependency] private readonly SharedAchievementsSystem _achievement = default!;
        [Dependency] private readonly ActionsSystem _action = default!;
        [Dependency] private readonly AntagSelectionSystem _antag = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly ISharedPlayerManager _player = default!;
        [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
        [Dependency] private readonly MetabolizerSystem _metabolism = default!;
        [Dependency] private readonly MindSystem _mind = default!;
        [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
        [Dependency] private readonly RoleSystem _role = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
        [Dependency] private readonly ObjectivesSystem _objectives = default!;
        [Dependency] private readonly TargetObjectiveSystem _target = default!;
        [Dependency] private readonly MetaDataSystem _meta = default!;

        public readonly ProtoId<NpcFactionPrototype> BloodCultNpcFaction = "BloodCult";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BloodCultRuleComponent, ComponentStartup>(OnRuleStartup);
            SubscribeLocalEvent<BloodCultRuleComponent, AfterAntagEntitySelectedEvent>(OnCultistSelected);

            SubscribeLocalEvent<BloodCultistComponent, ComponentStartup>((_, _, _) => CheckStage());
            SubscribeLocalEvent<BloodCultConstructComponent, ComponentStartup>((_, _, _) => CheckStage());
            SubscribeLocalEvent<BloodCultObjectComponent, ComponentShutdown>(OnBloodCultObjectShutdown);
            SubscribeLocalEvent<BloodCultObjectComponent, CryostorageEnterEvent>(OnCryostorageEnter);

            SubscribeLocalEvent<GodCalledEvent>(OnGodCalled);
            SubscribeLocalEvent<RitualConductedEvent>(OnRitualConducted);

            SubscribeLocalEvent<AutoCultistComponent, ComponentStartup>(OnAutoCultistAdded);
            SubscribeLocalEvent<BloodCultistComponent, ComponentRemove>(OnComponentRemove);
            SubscribeLocalEvent<BloodCultistComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<BloodCultistComponent, EntityZombifiedEvent>(OnCultistZombified);
        }

        private void OnRuleStartup(EntityUid uid, BloodCultRuleComponent component, ComponentStartup args)
        {
            component.SelectedGod = (BloodCultGod)_random.Next(0, 3);
        }

        private void OnCryostorageEnter(EntityUid uid, BloodCultObjectComponent component, CryostorageEnterEvent args)
        {
            var cult = GetActiveRule();
            if (cult == null)
                return;

            if (!cult.SelectedTargets.Contains(uid))
                return;

            var newTarget = FindNewRandomTarget(cult, uid);
            if (newTarget == null)
                return;

            ReplaceTargetForAllCultists(uid, newTarget.Value);

            cult.SelectedTargets.Remove(uid);
            cult.SelectedTargets.Add(newTarget.Value);

            RemComp<BloodCultObjectComponent>(uid);
            EnsureComp<BloodCultObjectComponent>(newTarget.Value);
        }

        #region Cultist Processing

        private void OnCultistSelected(Entity<BloodCultRuleComponent> mindId, ref AfterAntagEntitySelectedEvent args)
        {
            var ent = args.EntityUid;

            if (mindId.Comp.SelectedTargets.Count == 0)
                SelectRandomTargets(mindId.Comp);

            MakeCultist(ent);
            _antag.SendBriefing(ent, MakeBriefing(ent), Color.Red, null);
        }

        private void SelectRandomTargets(BloodCultRuleComponent cult)
        {
            cult.SelectedTargets.Clear();

            var mindShieldCandidates = new List<EntityUid>();
            var enumerator = EntityQueryEnumerator<MindShieldComponent>();
            while (enumerator.MoveNext(out var uid, out _))
                mindShieldCandidates.Add(uid);

            if (mindShieldCandidates.Count >= 2)
            {
                var selectedIndices = new HashSet<int>();
                while (selectedIndices.Count < 2)
                {
                    var index = _random.Next(0, mindShieldCandidates.Count);
                    selectedIndices.Add(index);
                }

                foreach (var index in selectedIndices)
                {
                    var target = mindShieldCandidates[index];
                    cult.SelectedTargets.Add(target);
                    EnsureComp<BloodCultObjectComponent>(target);
                }
                return;
            }

            foreach (var target in mindShieldCandidates)
            {
                cult.SelectedTargets.Add(target);
                EnsureComp<BloodCultObjectComponent>(target);
            }

            var globalCandidates = new List<EntityUid>();
            var globalEnumerator = EntityQueryEnumerator<HumanoidProfileComponent, ActorComponent>();
            while (globalEnumerator.MoveNext(out var uid, out _, out _))
            {
                if (cult.SelectedTargets.Contains(uid) || HasComp<BloodCultistComponent>(uid))
                    continue;

                globalCandidates.Add(uid);
            }

            while (cult.SelectedTargets.Count < 2 && globalCandidates.Count > 0)
            {
                var index = _random.Next(0, globalCandidates.Count);
                var target = globalCandidates[index];
                cult.SelectedTargets.Add(target);
                EnsureComp<BloodCultObjectComponent>(target);
                globalCandidates.RemoveAt(index);
            }
        }

        private EntityUid? FindNewRandomTarget(BloodCultRuleComponent cult, EntityUid excludedTarget)
        {
            var candidates = new List<EntityUid>();
            var query = EntityQueryEnumerator<HumanoidProfileComponent, ActorComponent>();
            while (query.MoveNext(out var uid, out _, out _))
            {
                if (uid == excludedTarget || cult.SelectedTargets.Contains(uid)
                    || HasComp<BloodCultistComponent>(uid)
                    || HasComp<BloodCultObjectComponent>(uid))
                    continue;

                candidates.Add(uid);
            }

            if (candidates.Count == 0)
                return null;

            var index = _random.Next(0, candidates.Count);
            return candidates[index];
        }

        private void ReplaceTargetForAllCultists(EntityUid oldTarget, EntityUid newTarget)
        {
            var replacedObjectives = new List<EntityUid>();
            var query = EntityQueryEnumerator<TargetObjectiveComponent, BloodCultTargetObjectiveComponent>();
            while (query.MoveNext(out var objectiveUid, out var targetComp, out _))
            {
                if (targetComp.Target == oldTarget)
                {
                    replacedObjectives.Add(objectiveUid);
                }
            }

            foreach (var objectiveUid in replacedObjectives)
            {
                _target.SetTarget(objectiveUid, newTarget);
                _meta.SetEntityName(objectiveUid, Loc.GetString("objective-condition-blood-ritual-person-title",
                    ("targetName", Name(newTarget))));
            }
        }

        private void MakeCultist(EntityUid ent)
        {
            var actionPrototypes = new[]
            {
                BloodCultistComponent.BloodMagic,
                BloodCultistComponent.RecallBloodDagger
            };

            foreach (var actionPrototype in actionPrototypes)
                _action.AddAction(ent, actionPrototype);

            var componentsToRemove = new[]
            {
                typeof(PacifiedComponent),
                typeof(ClumsyComponent)
            };

            foreach (var compType in componentsToRemove)
                RemComp(ent, compType);

            HandleMetabolism(ent);
            CreateObjectivesForCultist(ent);
        }

        private void CreateObjectivesForCultist(EntityUid cultist)
        {
            var cult = GetActiveRule();
            if (cult == null || cult.SelectedTargets.Count == 0)
                return;

            if (!_mind.TryGetMind(cultist, out var mindId, out var mind))
                return;

            foreach (var target in cult.SelectedTargets)
            {
                if (!Exists(target))
                    continue;

                var objective = _objectives.TryCreateObjective(mindId, mind, cult.ObjectivePrototype);
                if (objective == null)
                    continue;

                _target.SetTarget(objective.Value, target);
                _meta.SetEntityName(objective.Value, Loc.GetString("objective-condition-blood-ritual-person-title",
                    ("targetName", Name(target)))); // <see cref="ObjectiveAssignedEvent"/> here doesn't worked, or i'm stupid
                _mind.AddObjective(mindId, mind, objective.Value);
            }
        }

        private void HandleMetabolism(EntityUid cultist)
        {
            if (TryComp<BodyComponent>(cultist, out var bodyComponent) && bodyComponent.Organs != null)
            {
                foreach (var organ in bodyComponent.Organs.ContainedEntities)
                {
                    if (TryComp<MetabolizerComponent>(organ, out var metabolizer))
                    {
                        if (TryComp<StomachComponent>(organ, out _))
                            _metabolism.ClearMetabolizerTypes(metabolizer);

                        _metabolism.TryAddMetabolizerType(metabolizer, "BloodCultist");
                    }
                }
            }
        }

        private string MakeBriefing(EntityUid ent)
        {
            string selectedGod = "";
            var query = QueryActiveRules();
            while (query.MoveNext(out _, out _, out var cult, out _))
            {
                selectedGod = cult.SelectedGod switch
                {
                    BloodCultGod.NarSi => Loc.GetString("current-god-narsie"),
                    BloodCultGod.Reaper => Loc.GetString("current-god-reaper"),
                    BloodCultGod.Kharin => Loc.GetString("current-god-kharin"),
                    _ => Loc.GetString("current-god-narsie")
                };
                break;
            }

            var isHuman = HasComp<HumanoidProfileComponent>(ent);
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

            var mindLink = EnsureComp<MindLinkComponent>(uid);
            mindLink.Channels.Add(culsistComp.CultMindChannel);

            MakeCultist(uid);
            var query = QueryActiveRules();
            while (query.MoveNext(out _, out _, out var cult, out _))
            {
                EntProtoId selectedDagger = cult.SelectedGod switch
                {
                    BloodCultGod.NarSi => "WeaponBloodDagger",
                    BloodCultGod.Reaper => "WeaponDeathDagger",
                    BloodCultGod.Kharin => "WeaponHellDagger",
                    _ => "WeaponBloodDagger"
                };

                var dagger = _entityManager.SpawnEntity(selectedDagger, Transform(uid).Coordinates);
                culsistComp.RecallDaggerActionEntity = dagger;
                _hands.TryPickupAnyHand(uid, dagger);
                break;
            }
        }

        #endregion

        #region Stages

        private void OnBloodCultObjectShutdown(EntityUid uid, BloodCultObjectComponent component, ComponentShutdown args)
        {
            var cult = GetActiveRule();
            if (cult == null)
                return;

            if (!cult.SelectedTargets.Contains(uid))
            {
                CheckStage();
                if (cult.SelectedTargets.Count == 0)
                    RaiseLocalEvent(new RitualConductedEvent());
                return;
            }

            var newTarget = FindNewRandomTarget(cult, uid);
            if (newTarget == null)
                return;

            ReplaceTargetForAllCultists(uid, newTarget.Value);

            cult.SelectedTargets.Remove(uid);
            cult.SelectedTargets.Add(newTarget.Value);

            EnsureComp<BloodCultObjectComponent>(newTarget.Value);
        }

        private void CheckStage()
        {
            var cult = GetActiveRule();
            if (cult == null)
                return;

            var totalCultEntities = GetCultEntities();
            var playerCount = GetPlayerCount();

            // Second
            if (playerCount >= 100 && totalCultEntities >= playerCount * 0.1f || playerCount < 100 && totalCultEntities >= playerCount * 0.2f || cult.RitualStage)
            {
                foreach (var cultist in GetAllCultists())
                {
                    if (!HasComp<BloodCultistEyesComponent>(cultist))
                    {
                        UpdateCultistEyes(cultist);
                        AddComp<BloodCultistEyesComponent>(cultist);
                    }
                }

                if (!cult.FirstTriggered)
                {
                    var actorFilter = Filter.Empty();
                    var actorQuery = EntityQueryEnumerator<ActorComponent, BloodCultistComponent>();
                    while (actorQuery.MoveNext(out var actorUid, out var actor, out _))
                    {
                        actorFilter.AddPlayer(actor.PlayerSession);
                        _popup.PopupEntity(Loc.GetString("blood-cult-first-warning"), actorUid, actorUid, PopupType.SmallCaution);
                    }

                    _audio.PlayGlobal(new SoundPathSpecifier("/Audio/_Wega/Ambience/Antag/bloodcult_eyes.ogg"), actorFilter, true);
                    cult.FirstTriggered = true;
                }
            }

            // Third
            if (playerCount >= 100 && totalCultEntities >= playerCount * 0.2f || playerCount < 100 && totalCultEntities >= playerCount * 0.3f || cult.RitualStage)
            {
                foreach (var cultist in GetAllCultists())
                {
                    EnsureComp<BloodPentagramDisplayComponent>(cultist);
                }

                if (!cult.SecondTriggered)
                {
                    var actorFilter = Filter.Empty();
                    var actorQuery = EntityQueryEnumerator<ActorComponent, BloodCultistComponent>();
                    while (actorQuery.MoveNext(out var actorUid, out var actor, out _))
                    {
                        actorFilter.AddPlayer(actor.PlayerSession);
                        _popup.PopupEntity(Loc.GetString("blood-cult-second-warning"), actorUid, actorUid, PopupType.SmallCaution);
                    }

                    _audio.PlayGlobal(new SoundPathSpecifier("/Audio/_Wega/Ambience/Antag/bloodcult_halos.ogg"), actorFilter, true);
                    cult.SecondTriggered = true;
                }
            }
        }

        private void UpdateCultistEyes(EntityUid cultist)
        {
            if (_visualBody.TryGatherMarkingsData(cultist, null, out var profiles, out _, out _))
            {
                var cultistEyeColor = Color.FromHex("#E22218FF");

                var updatedProfiles = profiles.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value with { EyeColor = cultistEyeColor });

                _visualBody.ApplyProfiles(cultist, updatedProfiles);
            }
        }

        private int GetCultEntities()
        {
            var totalCultists = GetAllCultists().Count;
            var totalConstructs = GetAllConstructs().Count;
            return totalCultists + totalConstructs;
        }

        private int GetPlayerCount()
        {
            int count = 0;
            var players = AllEntityQuery<HumanoidProfileComponent, ActorComponent, TransformComponent>();
            while (players.MoveNext(out _, out _, out _, out _))
                count++;

            return count;
        }

        private List<EntityUid> GetAllCultists()
        {
            var cultists = new List<EntityUid>();
            var enumerator = EntityQueryEnumerator<BloodCultistComponent>();
            while (enumerator.MoveNext(out var uid, out _))
                cultists.Add(uid);

            return cultists;
        }

        private List<EntityUid> GetAllConstructs()
        {
            var constructs = new List<EntityUid>();
            var enumerator = EntityQueryEnumerator<BloodCultConstructComponent>();
            while (enumerator.MoveNext(out var uid, out _))
                constructs.Add(uid);

            return constructs;
        }

        #endregion

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

        public BloodCultRuleComponent? GetActiveRule()
        {
            var query = QueryActiveRules();
            while (query.MoveNext(out _, out _, out var cult, out _))
            {
                return cult;
            }
            return null;
        }

        private void OnGodCalled(GodCalledEvent ev)
        {
            var cult = GetActiveRule();
            if (cult == null)
                return;

            if (cult.BloodCultWinCondition.Contains(BloodCultWinType.RitualConducted))
                cult.BloodCultWinCondition.Remove(BloodCultWinType.RitualConducted);

            cult.WinType = BloodCultWinType.GodCalled;

            if (!cult.BloodCultWinCondition.Contains(BloodCultWinType.GodCalled))
            {
                cult.BloodCultWinCondition.Add(BloodCultWinType.GodCalled);
                _roundEndSystem.DoRoundEndBehavior(RoundEndBehavior.ShuttleCall, TimeSpan.FromMinutes(1f));
            }

            var cultists = GetAllCultists();
            cultists.AddRange(GetAllConstructs());
            foreach (var cultist in cultists)
            {
                // Yes, you have reached this stage, and it was achieved with your help.
                _achievement.QueueAchievement(cultist, AchievementsEnum.BloodCult);
            }
        }

        private void OnRitualConducted(RitualConductedEvent ev)
        {
            var cult = GetActiveRule();
            if (cult == null)
                return;

            cult.RitualStage = true;
            cult.WinType = BloodCultWinType.RitualConducted;

            CheckStage();

            if (!cult.BloodCultWinCondition.Contains(BloodCultWinType.RitualConducted))
                cult.BloodCultWinCondition.Add(BloodCultWinType.RitualConducted);
        }

        private void OnMobStateChanged(EntityUid uid, BloodCultistComponent component, MobStateChangedEvent ev)
        {
            if (ev.NewMobState == MobState.Dead)
                CheckCultLose(GetActiveRule());
        }

        private void OnComponentRemove(EntityUid uid, BloodCultistComponent component, ComponentRemove args)
        {
            CheckCultLose(GetActiveRule());
        }

        private void OnCultistZombified(EntityUid uid, BloodCultistComponent component, EntityZombifiedEvent args)
        {
            CheckCultLose(GetActiveRule());
        }

        private void CheckCultLose(BloodCultRuleComponent? cult)
        {
            if (cult == null)
                return;

            var hasLivingCultists = EntityQuery<BloodCultistComponent>().Any();
            if (!hasLivingCultists && !cult.BloodCultWinCondition.Contains(BloodCultWinType.GodCalled)
                && !cult.BloodCultWinCondition.Contains(BloodCultWinType.RitualConducted))
            {
                cult.BloodCultWinCondition.Add(BloodCultWinType.CultLose);
                cult.WinType = BloodCultWinType.CultLose;
            }
        }
    }
}
