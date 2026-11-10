#nullable disable
using System.Collections.Generic;

namespace NadekoBot.Db.Models;

public enum BetEventStatus
{
    Open = 0,
    Locked = 1,
    Resolved = 2,
    Cancelled = 3,
}

// ASSUMPTION: DbEntity provides `Id` (int, PK) — same base ShopEntry presumably uses.
// If your base class is named differently, swap it here.
public class BetEvent : DbEntity
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
    public ulong CreatorId { get; set; }
    public string Question { get; set; }
    public BetEventStatus Status { get; set; } = BetEventStatus.Open;
    public int? WinningOptionId { get; set; }
    public DateTime? LockAt { get; set; }

    public List<BetOption> Options { get; set; } = new();
}

public class BetOption : DbEntity
{
    public int BetEventId { get; set; }
    public BetEvent BetEvent { get; set; }
    public string Label { get; set; }

    public List<Bet> Bets { get; set; } = new();
}

public class Bet : DbEntity
{
    public int BetOptionId { get; set; }
    public BetOption BetOption { get; set; }
    public ulong UserId { get; set; }
    public long Amount { get; set; }
}
