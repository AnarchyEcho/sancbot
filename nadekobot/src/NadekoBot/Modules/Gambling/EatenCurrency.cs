#nullable disable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NadekoBot.Db.Models;

public class EatenCurrency : DbEntity
{
    public ulong UserId { get; set; }
    public string Username { get; set; }
    public long EatenAmount { get; set; }
}

public sealed class EatenCurrencyEntityConfiguration : IEntityTypeConfiguration<EatenCurrency>
{
    public void Configure(EntityTypeBuilder<EatenCurrency> builder)
    {
        builder.HasIndex(x => x.UserId)
               .IsUnique();
    }
}
