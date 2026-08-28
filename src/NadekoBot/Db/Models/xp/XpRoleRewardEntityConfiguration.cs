using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NadekoBot.Db.Models;

public class XpRoleRewardEntityConfiguration : IEntityTypeConfiguration<XpRoleReward>
{
    public void Configure(EntityTypeBuilder<XpRoleReward> builder)
    {
        // A level can have up to two role rewards: one that adds a role (Remove = false)
        // and one that removes a role (Remove = true). The index includes Remove so both
        // can coexist for the same (XpSettingsId, Level), while still preventing two
        // "add" rewards or two "remove" rewards from being set on the same level.
        builder
            .HasIndex(x => new
            {
                x.XpSettingsId,
                x.Level,
                x.Remove,
            })
            .IsUnique();
    }
}
