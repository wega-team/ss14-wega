using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Crawling;

[Serializable, NetSerializable]
public sealed partial class CrawlingStandUpDoAfterEvent : SimpleDoAfterEvent;
