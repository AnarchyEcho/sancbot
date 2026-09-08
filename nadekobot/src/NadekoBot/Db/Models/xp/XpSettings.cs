#nullable disable
namespace NadekoBot.Db.Models;

[ShardFiltered]
public class XpSettings : DbEntity
{
    public const int DEFAULT_FORMULA_A = 9;
    public const int DEFAULT_FORMULA_C = 27;

    public ulong GuildId { get; set; }

    public int XpFormulaA { get; set; } = DEFAULT_FORMULA_A;
    public int XpFormulaC { get; set; } = DEFAULT_FORMULA_C;

    /// <summary>
    ///     Role given to a user the moment they earn any xp while still at level 0.
    ///     Removed automatically once they reach level 1 and its role reward (if any) is applied.
    /// </summary>
    public ulong? Level0RoleId { get; set; }

    public HashSet<XpRoleReward> RoleRewards { get; set; } = new();
    public HashSet<XpCurrencyReward> CurrencyRewards { get; set; } = new();
}