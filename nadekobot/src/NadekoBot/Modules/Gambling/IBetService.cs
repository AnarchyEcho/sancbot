#nullable disable
using NadekoBot.Db.Models;

namespace NadekoBot.Modules.Gambling.Services;

public interface IBetService
{
    Task<BetEvent> CreateEventAsync(
        ulong guildId,
        ulong channelId,
        ulong creatorId,
        string question,
        IReadOnlyList<string> optionLabels);

    Task SetMessageIdAsync(int eventId, ulong messageId);

    Task<BetEvent> GetEventAsync(int eventId);

    /// <summary>Locks an open event immediately — no more bets accepted until resolved/cancelled.</summary>
    Task<bool> LockEventAsync(ulong guildId, int eventId);

    /// <summary>Schedules an open event to auto-lock after the given delay.</summary>
    Task<bool> ScheduleLockAsync(ulong guildId, int eventId, TimeSpan lockIn);

    /// <summary>
    ///     Locks every open event whose scheduled LockAt has passed. Called periodically
    ///     by a background loop. Returns the events that were just locked so the caller
    ///     can refresh their board messages.
    /// </summary>
    Task<IReadOnlyList<BetEvent>> LockDueEventsAsync();

    /// <summary>
    ///     Places a bet, or adds to a user's existing bet on the same option (stacking).
    ///     Fails if the user already has a bet on a *different* option in this event.
    /// </summary>
    Task<BetPlacementResult> PlaceBetAsync(int eventId, int optionId, ulong userId, long amount);

    Task<BetResolutionResult> ResolveEventAsync(ulong guildId, int eventId, int winningOptionId);

    /// <summary>Cancels the event and fully refunds every bettor.</summary>
    Task<bool> CancelEventAsync(ulong guildId, int eventId);
}

public sealed class BetPlacementResult
{
    public bool Success { get; init; }
    public string ErrorReason { get; init; }
}

public sealed class BetResolutionResult
{
    public bool Success { get; init; }
    public string ErrorReason { get; init; }
    public string WinningLabel { get; init; }
    public long TotalPaidOut { get; init; }
    public int WinnerCount { get; init; }
}
