#nullable disable
using Discord;
using NadekoBot.Db.Models;

namespace NadekoBot.Modules.Gambling.Services;

public static class BetBoardHelper
{
    public static Embed BuildOpenEmbed(BetEvent betEvent)
    {
        var totalPot = betEvent.Options.SelectMany(o => o.Bets).Sum(b => b.Amount);

        var eb = new EmbedBuilder()
            .WithTitle(betEvent.Question)
            .WithColor(Color.Blue)
            .WithFooter($"Event #{betEvent.Id}  •  Total pot: {totalPot:N0} cookies  •  Click a button to bet");

        foreach (var option in betEvent.Options)
        {
            var optionTotal = option.Bets.Sum(b => b.Amount);
            var pct = totalPot > 0 ? (double)optionTotal / totalPot * 100 : 0;

            eb.AddField(option.Label, $"{pct:0.#}%  •  {optionTotal:N0} cookies", true);
        }

        return eb.Build();
    }

    public static MessageComponent BuildOpenComponents(BetEvent betEvent)
    {
        var builder = new ComponentBuilder();

        foreach (var option in betEvent.Options)
            builder.WithButton(option.Label, $"betvote:{betEvent.Id}:{option.Id}", ButtonStyle.Primary);

        return builder.Build();
    }

    public static Embed BuildLockedEmbed(BetEvent betEvent)
    {
        var totalPot = betEvent.Options.SelectMany(o => o.Bets).Sum(b => b.Amount);

        var eb = new EmbedBuilder()
            .WithTitle(betEvent.Question)
            .WithColor(Color.Orange)
            .WithFooter($"Event #{betEvent.Id}  •  Total pot: {totalPot:N0} cookies  •  🔒 Betting closed, awaiting result");

        foreach (var option in betEvent.Options)
        {
            var optionTotal = option.Bets.Sum(b => b.Amount);
            var pct = totalPot > 0 ? (double)optionTotal / totalPot * 100 : 0;

            eb.AddField(option.Label, $"{pct:0.#}%  •  {optionTotal:N0} cookies", true);
        }

        return eb.Build();
    }

    public static Embed BuildResolvedEmbed(BetEvent betEvent, BetResolutionResult result) =>
        new EmbedBuilder()
            .WithTitle(betEvent.Question)
            .WithColor(Color.Gold)
            .WithDescription(
                $"🏆 **{result.WinningLabel}** won! Paid out **{result.TotalPaidOut:N0}** cookies "
                + $"to **{result.WinnerCount}** user{(result.WinnerCount == 1 ? "" : "s")}.")
            .WithFooter($"Event #{betEvent.Id}  •  Resolved")
            .Build();

    public static Embed BuildCancelledEmbed(BetEvent betEvent) =>
        new EmbedBuilder()
            .WithTitle(betEvent.Question)
            .WithColor(Color.Red)
            .WithDescription("This event was cancelled. All bets have been refunded.")
            .WithFooter($"Event #{betEvent.Id}  •  Cancelled")
            .Build();

    // Empty component set removes the buttons once betting is closed.
    public static MessageComponent BuildClosedComponents() => new ComponentBuilder().Build();
}
