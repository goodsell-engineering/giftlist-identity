namespace Identity.Domain.Common;

/// <summary>
/// An instant in this domain has MILLISECOND resolution. This normalises one to that resolution.
/// </summary>
/// <remarks>
/// <para>
/// That is a domain decision, not a storage detail: a user's <c>CreatedAt</c> is meaningful to
/// the millisecond and no finer, so two instants that differ only below that are the same
/// instant here. Stating it in the Domain means every layer inherits one answer instead of each
/// rediscovering it.
/// </para>
/// <para>
/// It is the same defect <c>GiftLists.Domain.Common.Timestamps</c> was written to fix, found
/// there first: .NET ticks are 100ns and the BSON dates <c>UserDocument</c> stores are
/// milliseconds, so a <c>User</c> built from <see cref="DateTimeOffset.UtcNow"/> held up to 9999
/// ticks that storage silently discarded, and a reloaded user did not equal the one saved
/// (GL-63, raised by the Batch 11 review of GL-20 — the fix GL-20 made to <c>GiftLists.Domain</c>
/// but did not carry to this one).
/// </para>
/// <para>
/// Normalising at construction rather than in the persistence mapper is what makes "saved equals
/// reloaded" true by construction: truncating only on the way out would fix reads while leaving a
/// freshly-built aggregate holding precision destined to vanish. Truncate, never round, so a
/// value can only move toward the past.
/// </para>
/// <para>
/// This cannot be shared with <c>GiftLists.Domain.Common.Timestamps</c> — Domain references
/// nothing at all (CONVENTIONS.md "Project reference graph"), so nowhere both could reference exists — and so it is a
/// deliberate per-service copy, exactly as <see cref="IDomainEvent"/> is. Tracked rather than
/// left as a silent asymmetry: see that type's own remarks for the full reasoning, and see the
/// GiftLists copy's own doc comment, which cross-references this one.
/// </para>
/// </remarks>
public static class Timestamps
{
    /// <summary>Drops sub-millisecond ticks, preserving the offset.</summary>
    public static DateTimeOffset ToStoredPrecision(DateTimeOffset value) =>
        value.AddTicks(-(value.Ticks % TimeSpan.TicksPerMillisecond));
}
