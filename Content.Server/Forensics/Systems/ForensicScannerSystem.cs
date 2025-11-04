using System.Linq;
using System.Text;
using Content.Server.Popups;
using Content.Shared.UserInterface;
using Content.Shared.DoAfter;
using Content.Shared.Fluids.Components;
using Content.Shared.Forensics;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Verbs;
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Server.Chemistry.Containers.EntitySystems;
using Robust.Shared.Prototypes;
using Content.Shared.PDA; // Corvax-Wega-NanoChat
using Robust.Shared.Containers; // Corvax-Wega-NanoChat
using Content.Shared.CartridgeLoader.Cartridges; // Corvax-Wega-NanoChat
// todo: remove this stinky LINQy

namespace Content.Server.Forensics
{
    public sealed class ForensicScannerSystem : EntitySystem
    {
        [Dependency] private readonly SharedContainerSystem _container = default!; // Corvax-Wega-NanoChat
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly PaperSystem _paperSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;
        [Dependency] private readonly ForensicsSystem _forensicsSystem = default!;
        [Dependency] private readonly TagSystem _tag = default!;

        private static readonly ProtoId<TagPrototype> DNASolutionScannableTag = "DNASolutionScannable";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ForensicScannerComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<ForensicScannerComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<ForensicScannerComponent, BeforeActivatableUIOpenEvent>(OnBeforeActivatableUIOpen);
            SubscribeLocalEvent<ForensicScannerComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
            SubscribeLocalEvent<ForensicScannerComponent, ForensicScannerPrintMessage>(OnPrint);
            SubscribeLocalEvent<ForensicScannerComponent, ForensicScannerClearMessage>(OnClear);
            SubscribeLocalEvent<ForensicScannerComponent, ForensicScannerDoAfterEvent>(OnDoAfter);
        }

        private void UpdateUserInterface(EntityUid uid, ForensicScannerComponent component)
        {
            var state = new ForensicScannerBoundUserInterfaceState(
                component.Fingerprints,
                component.Fibers,
                component.TouchDNAs,
                component.SolutionDNAs,
                component.Residues,
                component.LastScannedName,
                component.PrintCooldown,
                component.PrintReadyAt);

            _uiSystem.SetUiState(uid, ForensicScannerUiKey.Key, state);
        }

        private void OnDoAfter(EntityUid uid, ForensicScannerComponent component, DoAfterEvent args)
        {
            if (args.Handled || args.Cancelled)
                return;

            if (!TryComp(uid, out ForensicScannerComponent? scanner))
                return;

            if (args.Args.Target != null)
            {
                if (!TryComp<ForensicsComponent>(args.Args.Target, out var forensics))
                {
                    scanner.Fingerprints = new();
                    scanner.Fibers = new();
                    scanner.TouchDNAs = new();
                    scanner.Residues = new();
                }
                else
                {
                    scanner.Fingerprints = forensics.Fingerprints.ToList();
                    scanner.Fibers = forensics.Fibers.ToList();
                    scanner.TouchDNAs = forensics.DNAs.ToList();
                    scanner.Residues = forensics.Residues.ToList();
                }

                if (_tag.HasTag(args.Args.Target.Value, DNASolutionScannableTag))
                {
                    scanner.SolutionDNAs = _forensicsSystem.GetSolutionsDNA(args.Args.Target.Value);
                }
                else
                {
                    scanner.SolutionDNAs = new();
                }

                scanner.LastScannedName = MetaData(args.Args.Target.Value).EntityName;
            }

            OpenUserInterface(args.Args.User, (uid, scanner));
        }

        /// <remarks>
        /// Hosts logic common between OnUtilityVerb and OnAfterInteract.
        /// </remarks>
        private void StartScan(EntityUid uid, ForensicScannerComponent component, EntityUid user, EntityUid target)
        {
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, component.ScanDelay, new ForensicScannerDoAfterEvent(), uid, target: target, used: uid)
            {
                BreakOnMove = true,
                NeedHand = true
            });
        }

        private void OnUtilityVerb(EntityUid uid, ForensicScannerComponent component, GetVerbsEvent<UtilityVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess || component.CancelToken != null)
                return;

            var verb = new UtilityVerb()
            {
                Act = () => StartScan(uid, component, args.User, args.Target),
                IconEntity = GetNetEntity(uid),
                Text = Loc.GetString("forensic-scanner-verb-text"),
                Message = Loc.GetString("forensic-scanner-verb-message"),
                // This is important because if its true using the scanner will count as touching the object.
                DoContactInteraction = false
            };

            args.Verbs.Add(verb);

            // Corvax-Wega-NanoChat-start
            if (TryComp<PdaComponent>(args.Target, out var pda))
            {
                var printChatVerb = new UtilityVerb()
                {
                    Act = () => PrintChatHistory(uid, component, args.User, args.Target),
                    IconEntity = GetNetEntity(uid),
                    Text = Loc.GetString("forensic-scanner-print-chat-history-text"),
                    Message = Loc.GetString("forensic-scanner-print-chat-history-message")
                };
                args.Verbs.Add(printChatVerb);
            }
            // Corvax-Wega-NanoChat-end
        }

        private void OnAfterInteract(EntityUid uid, ForensicScannerComponent component, AfterInteractEvent args)
        {
            if (component.CancelToken != null || args.Target == null || !args.CanReach)
                return;

            StartScan(uid, component, args.User, args.Target.Value);
        }

        private void OnAfterInteractUsing(EntityUid uid, ForensicScannerComponent component, AfterInteractUsingEvent args)
        {
            if (args.Handled || !args.CanReach)
                return;

            if (!TryComp<ForensicPadComponent>(args.Used, out var pad))
                return;

            foreach (var fiber in component.Fibers)
            {
                if (fiber == pad.Sample)
                {
                    _audioSystem.PlayPvs(component.SoundMatch, uid);
                    _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-match-fiber"), uid, args.User);
                    return;
                }
            }

            foreach (var fingerprint in component.Fingerprints)
            {
                if (fingerprint == pad.Sample)
                {
                    _audioSystem.PlayPvs(component.SoundMatch, uid);
                    _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-match-fingerprint"), uid, args.User);
                    return;
                }
            }

            _audioSystem.PlayPvs(component.SoundNoMatch, uid);
            _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-match-none"), uid, args.User);
        }

        private void OnBeforeActivatableUIOpen(EntityUid uid, ForensicScannerComponent component, BeforeActivatableUIOpenEvent args)
        {
            UpdateUserInterface(uid, component);
        }

        private void OpenUserInterface(EntityUid user, Entity<ForensicScannerComponent> scanner)
        {
            UpdateUserInterface(scanner, scanner.Comp);

            _uiSystem.OpenUi(scanner.Owner, ForensicScannerUiKey.Key, user);
        }

        private void OnPrint(EntityUid uid, ForensicScannerComponent component, ForensicScannerPrintMessage args)
        {
            var user = args.Actor;

            if (_gameTiming.CurTime < component.PrintReadyAt)
            {
                // This shouldn't occur due to the UI guarding against it, but
                // if it does, tell the user why nothing happened.
                _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-printer-not-ready"), uid, user);
                return;
            }

            // Spawn a piece of paper.
            var printed = Spawn(component.MachineOutput, Transform(uid).Coordinates);
            _handsSystem.PickupOrDrop(args.Actor, printed, checkActionBlocker: false);

            if (!TryComp<PaperComponent>(printed, out var paperComp))
            {
                Log.Error("Printed paper did not have PaperComponent.");
                return;
            }

            _metaData.SetEntityName(printed, Loc.GetString("forensic-scanner-report-title", ("entity", component.LastScannedName)));

            var text = new StringBuilder();

            text.AppendLine(Loc.GetString("forensic-scanner-report-header",
                ("entity", component.LastScannedName),
                ("time", _gameTiming.CurTime.ToString("hh\\:mm\\:ss"))));
            text.AppendLine(new string('=', 30));
            text.AppendLine();

            AppendSection(text,
                Loc.GetString("forensic-scanner-interface-fingerprints"),
                component.Fingerprints);

            AppendSection(text,
                Loc.GetString("forensic-scanner-interface-fibers"),
                component.Fibers);

            var allDna = component.TouchDNAs.Concat(
                component.SolutionDNAs.Except(component.TouchDNAs));
            AppendSection(text,
                Loc.GetString("forensic-scanner-interface-dnas"),
                allDna);

            AppendSection(text,
                Loc.GetString("forensic-scanner-interface-residues"),
                component.Residues);

            text.AppendLine();
            text.AppendLine(new string('=', 30));

            _paperSystem.SetContent((printed, paperComp), text.ToString());
            _audioSystem.PlayPvs(component.SoundPrint, uid,
                AudioParams.Default
                .WithVariation(0.25f)
                .WithVolume(3f)
                .WithRolloffFactor(2.8f)
                .WithMaxDistance(4.5f));

            component.PrintReadyAt = _gameTiming.CurTime + component.PrintCooldown;
            UpdateUserInterface(uid, component);
        }

        // Corvax-Wega-NanoChat-start
        private void PrintChatHistory(EntityUid uid, ForensicScannerComponent component, EntityUid user, EntityUid target)
        {
            if (!TryComp<PdaComponent>(target, out var pda) || !TryComp<ContainerManagerComponent>(target, out var containerManager))
                return;

            var container = _container.GetContainer(target, "program-container", containerManager);
            if (container == null || container.ContainedEntities.Count == 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-no-cartridge"), uid, user);
                return;
            }

            var chatCartridges = new List<Entity<NanoChatCartridgeComponent>>();
            foreach (var cartridgeUid in container.ContainedEntities)
            {
                if (TryComp<NanoChatCartridgeComponent>(cartridgeUid, out var nanoChat))
                {
                    chatCartridges.Add((cartridgeUid, nanoChat));
                }
            }

            if (chatCartridges.Count == 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-no-chat-cartridge"), uid, user);
                return;
            }

            var printed = Spawn(component.MachineOutput, Transform(uid).Coordinates);
            _handsSystem.PickupOrDrop(user, printed, checkActionBlocker: false);

            if (!TryComp<PaperComponent>(printed, out var paperComp))
            {
                Log.Error("Printed paper did not have PaperComponent.");
                return;
            }

            _metaData.SetEntityName(printed, Loc.GetString("forensic-scanner-chat-history-title"));

            var text = new StringBuilder();
            text.AppendLine(Loc.GetString("forensic-scanner-chat-history-header",
                ("owner", pda.OwnerName ?? Loc.GetString("generic-unknown-title")),
                ("time", _gameTiming.CurTime.ToString("hh\\:mm\\:ss"))));
            text.AppendLine(new string('=', 36));
            text.AppendLine();

            foreach (var (cartridgeUid, cartComp) in chatCartridges)
            {
                AppendChatHistory(text, cartComp, cartridgeUid);
            }

            text.AppendLine(new string('=', 36));

            _paperSystem.SetContent((printed, paperComp), text.ToString());

            _audioSystem.PlayPvs(component.SoundPrint, uid,
                AudioParams.Default
                .WithVariation(0.25f)
                .WithVolume(3f)
                .WithRolloffFactor(2.8f)
                .WithMaxDistance(4.5f));

            _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-chat-history-printed"), uid, user);
        }
        // Corvax-Wega-NanoChat-end

        private void OnClear(EntityUid uid, ForensicScannerComponent component, ForensicScannerClearMessage args)
        {
            component.Fingerprints = new();
            component.Fibers = new();
            component.TouchDNAs = new();
            component.SolutionDNAs = new();
            component.LastScannedName = string.Empty;

            UpdateUserInterface(uid, component);
        }

        private void AppendSection(StringBuilder text, string title, IEnumerable<string> items)
        {
            text.AppendLine($"( {title.ToUpper()} )");

            if (!items.Any())
            {
                text.AppendLine(Loc.GetString("forensic-scanner-no-data"));
            }
            else
            {
                foreach (var item in items)
                {
                    text.AppendLine($"• {item}");
                }
            }

            text.AppendLine();
        }

        // Corvax-Wega-NanoChat-start
        private void AppendChatHistory(StringBuilder text, NanoChatCartridgeComponent chatComp, EntityUid cartUid)
        {
            text.AppendLine(Loc.GetString("forensic-scanner-chat-cartridge-header",
                ("id", chatComp.ChatId)));
            text.AppendLine();

            if (chatComp.Contacts.Count == 0 && chatComp.Messages.Count == 0)
            {
                text.AppendLine(Loc.GetString("forensic-scanner-chat-no-data"));
                text.AppendLine();
                return;
            }

            foreach (var (contactId, messages) in chatComp.Messages)
            {
                if (contactId.StartsWith("G"))
                    continue;

                if (chatComp.Contacts.TryGetValue(contactId, out var contact))
                {
                    text.AppendLine(Loc.GetString("forensic-scanner-chat-with-contact",
                        ("name", contact.ContactName),
                        ("id", contactId)));
                }
                else
                {
                    text.AppendLine(Loc.GetString("forensic-scanner-chat-with-unknown",
                        ("id", contactId)));
                }

                text.AppendLine(new string('-', 40));

                foreach (var message in messages)
                {
                    var time = $"{(int)message.Timestamp.TotalHours:00}:{message.Timestamp.Minutes:00}";
                    var sender = message.IsOwnMessage ?
                        Loc.GetString("forensic-scanner-chat-you") :
                        message.SenderName;

                    var status = message.Delivered ? "✓" : "✗";

                    text.AppendLine($"[{time}] {sender} ({status}): {message.Message}");
                }

                text.AppendLine();
            }

            foreach (var contact in chatComp.Contacts.Values)
            {
                if (contact.ContactId.StartsWith("G"))
                    continue;

                if (!chatComp.Messages.ContainsKey(contact.ContactId))
                {
                    text.AppendLine(Loc.GetString("forensic-scanner-chat-contact-no-messages",
                        ("name", contact.ContactName),
                        ("id", contact.ContactId)));
                    text.AppendLine();
                }
            }

            if (chatComp.Groups.Count > 0)
            {
                text.AppendLine(Loc.GetString("forensic-scanner-chat-groups-header"));
                text.AppendLine(new string('=', 36));

                foreach (var (groupId, group) in chatComp.Groups)
                {
                    text.AppendLine(Loc.GetString("forensic-scanner-chat-group-info",
                        ("name", group.GroupName),
                        ("id", groupId),
                        ("members", group.MemberCount)));

                    if (chatComp.Messages.TryGetValue(groupId, out var groupMessages))
                    {
                        text.AppendLine(Loc.GetString("forensic-scanner-chat-group-messages"));
                        text.AppendLine(new string('-', 30));

                        foreach (var message in groupMessages)
                        {
                            var time = $"{(int)message.Timestamp.TotalHours:00}:{message.Timestamp.Minutes:00}";
                            var sender = message.IsOwnMessage ?
                                Loc.GetString("forensic-scanner-chat-you") :
                                message.SenderName;

                            var status = message.Delivered ? "✓" : "✗";

                            text.AppendLine($"[{time}] {sender} ({status}): {message.Message}");
                        }
                    }
                    else
                    {
                        text.AppendLine(Loc.GetString("forensic-scanner-chat-group-no-messages"));
                    }

                    text.AppendLine();
                }
            }

            foreach (var (groupId, messages) in chatComp.Messages)
            {
                if (groupId.StartsWith("G") && !chatComp.Groups.ContainsKey(groupId))
                {
                    text.AppendLine(Loc.GetString("forensic-scanner-chat-archived-group",
                        ("id", groupId)));
                    text.AppendLine(new string('-', 30));

                    foreach (var message in messages)
                    {
                        var time = $"{(int)message.Timestamp.TotalHours:00}:{message.Timestamp.Minutes:00}";
                        var sender = message.IsOwnMessage ?
                            Loc.GetString("forensic-scanner-chat-you") :
                            message.SenderName;

                        var status = message.Delivered ? "✓" : "✗";

                        text.AppendLine($"[{time}] {sender} ({status}): {message.Message}");
                    }

                    text.AppendLine();
                }
            }
        }
        // Corvax-Wega-NanoChat-end
    }
}
