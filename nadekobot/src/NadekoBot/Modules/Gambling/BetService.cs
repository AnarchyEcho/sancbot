#nullable disable
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Db.Models;
using NadekoBot.Services;
using NadekoBot.Services.Currency;

namespace NadekoBot.Modules.Gambling.Services;

public class BetService : IBetService, INService
{
    private readonly DbService _db;
    private readonly ICurrencyService _cs;

    public BetService(DbService db, ICurrencyService cs)
    {
        _db = db;
        _cs = cs;
    }

    public async Task<BetEvent> CreateEventAsync(
        ulong guildId,
        ulong channelId,
        ulong creatorId,
        string question,
        IReadOnlyList<string> optionLabels)
    {
        await using var uow = _db.GetDbContext();

        var betEvent = new BetEvent
        {
            GuildId = guildId,
            ChannelId = channelId,
            CreatorId = creatorId,
            Question = question,
            Status = BetEventStatus.Open,
            Options = optionLabels.Select(l => new BetOption { Label = l }).ToList(),
        };

        uow.Set<BetEvent>().Add(betEvent);
        await uow.SaveChangesAsync();

        return betEvent;
    }

    public async Task SetMessageIdAsync(int eventId, ulong messageId)
    {
        await using var uow = _db.GetDbContext();

        await uow.GetTable<BetEvent>()
                 .Where(x => x.Id == eventId)
                 .UpdateAsync(x => new BetEvent { MessageId = messageId });
    }

    public async Task<BetEvent> GetEventAsync(int eventId)
    {
        await using var uow = _db.GetDbContext();

        return await uow.Set<BetEvent>()
                        .Include(x => x.Options)
                        .ThenInclude(x => x.Bets)
                        .FirstOrDefaultAsyncEF(x => x.Id == eventId);
    }

    public async Task<bool> LockEventAsync(ulong guildId, int eventId)
    {
        await using var uow = _db.GetDbContext();

        var changed = await uow.GetTable<BetEvent>()
                               .Where(x => x.Id == eventId && x.GuildId == guildId && x.Status == BetEventStatus.Open)
                               .UpdateAsync(x => new BetEvent { Status = BetEventStatus.Locked });

        return changed > 0;
    }

    public async Task<bool> ScheduleLockAsync(ulong guildId, int eventId, TimeSpan lockIn)
    {
        await using var uow = _db.GetDbContext();
        var lockAt = DateTime.UtcNow.Add(lockIn);

        var changed = await uow.GetTable<BetEvent>()
                               .Where(x => x.Id == eventId && x.GuildId == guildId && x.Status == BetEventStatus.Open)
                               .UpdateAsync(x => new BetEvent { LockAt = lockAt });

        return changed > 0;
    }

    public async Task<IReadOnlyList<BetEvent>> LockDueEventsAsync()
    {
        await using var uow = _db.GetDbContext();
        var now = DateTime.UtcNow;

        var dueEvents = await uow.Set<BetEvent>()
                                 .Include(x => x.Options)
                                 .ThenInclude(x => x.Bets)
                                 .Where(x => x.Status == BetEventStatus.Open && x.LockAt != null && x.LockAt <= now)
                                 .ToListAsyncEF();

        foreach (var betEvent in dueEvents)
            betEvent.Status = BetEventStatus.Locked;

        if (dueEvents.Count > 0)
            await uow.SaveChangesAsync();

        return dueEvents;
    }

    public async Task<BetPlacementResult> PlaceBetAsync(int eventId, int optionId, ulong userId, long amount)
    {
        if (amount <= 0)
            return new() { Success = false, ErrorReason = "amount_invalid" };

        await using var uow = _db.GetDbContext();

        var betEvent = await uow.Set<BetEvent>()
                                .Include(x => x.Options)
                                .ThenInclude(x => x.Bets)
                                .FirstOrDefaultAsyncEF(x => x.Id == eventId);

        if (betEvent is null)
            return new() { Success = false, ErrorReason = "event_not_found" };

        if (betEvent.Status != BetEventStatus.Open)
            return new() { Success = false, ErrorReason = "event_closed" };

        var option = betEvent.Options.FirstOrDefault(x => x.Id == optionId);
        if (option is null)
            return new() { Success = false, ErrorReason = "option_not_found" };

        // block hedging: a user can only ever have bets on ONE option per event.
        // Remove this check if you'd rather allow betting both sides.
        var alreadyBetOnOtherOption = betEvent.Options
            .Where(o => o.Id != optionId)
            .SelectMany(o => o.Bets)
            .Any(b => b.UserId == userId);

        if (alreadyBetOnOtherOption)
            return new() { Success = false, ErrorReason = "already_bet_other_option" };

        var balance = await _cs.GetBalanceAsync(userId);
        if (balance < amount)
            return new() { Success = false, ErrorReason = "insufficient_funds" };

        var txData = new TxData(
            Type: "event-bet-placed",
            Extra: $"Bet on \"{option.Label}\" — {betEvent.Question}",
            Note: $"Event #{betEvent.Id}",
            OtherId: (ulong)betEvent.Id);

        var removed = await _cs.RemoveAsync(userId, amount, txData);
        if (!removed)
            return new() { Success = false, ErrorReason = "insufficient_funds" };

        var existingBet = option.Bets.FirstOrDefault(x => x.UserId == userId);
        if (existingBet is not null)
            existingBet.Amount += amount;
        else
            option.Bets.Add(new Bet { UserId = userId, Amount = amount, BetOptionId = option.Id });

        await uow.SaveChangesAsync();

        return new() { Success = true };
    }

    public async Task<BetResolutionResult> ResolveEventAsync(ulong guildId, int eventId, int winningOptionId)
    {
        await using var uow = _db.GetDbContext();

        var betEvent = await uow.Set<BetEvent>()
                                .Include(x => x.Options)
                                .ThenInclude(x => x.Bets)
                                .FirstOrDefaultAsyncEF(x => x.Id == eventId && x.GuildId == guildId);

        if (betEvent is null)
            return new() { Success = false, ErrorReason = "event_not_found" };

        if (betEvent.Status is BetEventStatus.Resolved or BetEventStatus.Cancelled)
            return new() { Success = false, ErrorReason = "event_already_closed" };

        var winningOption = betEvent.Options.FirstOrDefault(x => x.Id == winningOptionId);
        if (winningOption is null)
            return new() { Success = false, ErrorReason = "option_not_found" };

        var totalPool = betEvent.Options.SelectMany(o => o.Bets).Sum(b => b.Amount);
        var winningPool = winningOption.Bets.Sum(b => b.Amount);

        // Pari-mutuel payout: each winner gets their proportional share of the ENTIRE
        // pool (their own stake back + a cut of the losers' stakes), rounded down.
        var payouts = new Dictionary<ulong, long>();
        if (winningPool > 0)
        {
            foreach (var bet in winningOption.Bets)
            {
                var share = (long)Math.Floor((decimal)bet.Amount / winningPool * totalPool);
                payouts[bet.UserId] = payouts.GetValueOrDefault(bet.UserId) + share;
            }
        }

        betEvent.Status = BetEventStatus.Resolved;
        betEvent.WinningOptionId = winningOptionId;
        await uow.SaveChangesAsync();

        var payoutTxData = new TxData(
            Type: "event-bet-payout",
            Extra: $"Event bet payout — \"{winningOption.Label}\" won: {betEvent.Question}",
            Note: $"Event #{betEvent.Id}",
            OtherId: (ulong)betEvent.Id);

        foreach (var (userId, amount) in payouts)
        {
            if (amount > 0)
                await _cs.AddAsync(userId, amount, payoutTxData);
        }

        return new()
        {
            Success = true,
            WinningLabel = winningOption.Label,
            TotalPaidOut = payouts.Values.Sum(),
            WinnerCount = payouts.Count(x => x.Value > 0),
        };
    }

    public async Task<bool> CancelEventAsync(ulong guildId, int eventId)
    {
        await using var uow = _db.GetDbContext();

        var betEvent = await uow.Set<BetEvent>()
                                .Include(x => x.Options)
                                .ThenInclude(x => x.Bets)
                                .FirstOrDefaultAsyncEF(x => x.Id == eventId && x.GuildId == guildId);

        if (betEvent is null || betEvent.Status is BetEventStatus.Resolved or BetEventStatus.Cancelled)
            return false;

        betEvent.Status = BetEventStatus.Cancelled;
        await uow.SaveChangesAsync();

        var refundTxData = new TxData(
            Type: "event-bet-refund",
            Extra: $"Event bet refund — cancelled: {betEvent.Question}",
            Note: $"Event #{betEvent.Id}",
            OtherId: (ulong)betEvent.Id);

        foreach (var bet in betEvent.Options.SelectMany(o => o.Bets))
        {
            if (bet.Amount > 0)
                await _cs.AddAsync(bet.UserId, bet.Amount, refundTxData);
        }

        return true;
    }
}
