namespace NadekoBot.Modules.Games.Fish;

public sealed class WhaleEvent : IDisposable
{
    public enum Phase
    {
        WaitingForHelpers,
        Succeeded,
        Failed
    }

    public const int REQUIRED_HELPERS = 2;
    public const int DURATION_SECONDS = 60;

    public event Func<WhaleEvent, Task>? OnHelperJoined;
    public event Func<WhaleEvent, Task>? OnSucceeded;
    public event Func<WhaleEvent, Task>? OnFailed;

    public Phase CurrentPhase { get; private set; } = Phase.WaitingForHelpers;

    public (ulong UserId, string Username) Initiator { get; }

    public IReadOnlyList<(ulong UserId, string Username)> Helpers
        => _helpers.AsReadOnly();

    public IEnumerable<(ulong UserId, string Username)> AllParticipants
        => new[] { Initiator }.Concat(_helpers);

    private readonly List<(ulong UserId, string Username)> _helpers = new();
    private readonly SemaphoreSlim _locker = new(1, 1);
    private Timer? _timeoutTimer;

    public WhaleEvent(ulong userId, string username)
    {
        Initiator = (userId, username);
    }

    /// <summary>
    /// Starts the expiry timer. Called separately from the constructor so the
    /// caller has a chance to subscribe to events before anything can fire.
    /// </summary>
    public void Initialize()
    {
        if (_timeoutTimer is not null)
            return;

        _timeoutTimer = new Timer(async _ =>
            {
                await _locker.WaitAsync();
                try
                {
                    if (CurrentPhase != Phase.WaitingForHelpers)
                        return;

                    CurrentPhase = Phase.Failed;
                    _ = OnFailed?.Invoke(this);
                }
                finally { _locker.Release(); }
            },
            null,
            TimeSpan.FromSeconds(DURATION_SECONDS),
            Timeout.InfiniteTimeSpan);
    }

    public async Task<bool> Join(ulong userId, string username)
    {
        await _locker.WaitAsync();
        try
        {
            if (CurrentPhase != Phase.WaitingForHelpers)
                return false;

            if (userId == Initiator.UserId || _helpers.Any(x => x.UserId == userId))
                return false;

            _helpers.Add((userId, username));

            if (_helpers.Count >= REQUIRED_HELPERS)
            {
                CurrentPhase = Phase.Succeeded;
                _timeoutTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _ = OnSucceeded?.Invoke(this);
            }
            else
            {
                _ = OnHelperJoined?.Invoke(this);
            }

            return true;
        }
        finally { _locker.Release(); }
    }

    public void Dispose()
    {
        OnHelperJoined = null;
        OnSucceeded = null;
        OnFailed = null;
        _timeoutTimer?.Dispose();
    }
}
