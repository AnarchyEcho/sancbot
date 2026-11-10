#nullable disable
using Discord;
using Discord.WebSocket;
using NadekoBot.Common.ModuleBehaviors;
using NadekoBot.Db.Models;

namespace NadekoBot.Modules.Gambling.Services;

public class BetInteractionService : INService, IReadyExecutor
{
    private readonly DiscordSocketClient _client;
    private readonly IBetService _betService;

    public BetInteractionService(DiscordSocketClient client, IBetService betService)
    {
        _client = client;
        _betService = betService;
    }

    public Task OnReadyAsync()
    {
        _client.ButtonExecuted += ButtonExecuted;
        _client.ModalSubmitted += ModalSubmitted;
        _ = AutoLockLoopAsync();
        return Task.CompletedTask;
    }

    private async Task AutoLockLoopAsync()
    {
        while (true)
        {
            try
            {
                var lockedEvents = await _betService.LockDueEventsAsync();
                foreach (var betEvent in lockedEvents)
                    await RefreshLockedBoardMessageAsync(betEvent);
            }
            catch
            {
                // Keep the loop alive even if one sweep fails (e.g. transient DB/Discord hiccup).
            }

            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }

    private async Task RefreshLockedBoardMessageAsync(BetEvent betEvent)
    {
        var guild = _client.GetGuild(betEvent.GuildId);
        var channel = guild?.GetTextChannel(betEvent.ChannelId);
        if (channel is null)
            return;

        if (await channel.GetMessageAsync(betEvent.MessageId) is not IUserMessage msg)
            return;

        await msg.ModifyAsync(x =>
        {
            x.Embed = BetBoardHelper.BuildLockedEmbed(betEvent);
            x.Components = BetBoardHelper.BuildClosedComponents();
        });
    }

    private async Task ButtonExecuted(SocketMessageComponent interaction)
    {
        var parts = interaction.Data.CustomId.Split(':');
        if (parts.Length != 3 || parts[0] != "betvote")
            return;

        if (!int.TryParse(parts[1], out var eventId) || !int.TryParse(parts[2], out var optionId))
            return;

        var betEvent = await _betService.GetEventAsync(eventId);
        if (betEvent is null || betEvent.Status != BetEventStatus.Open)
        {
            await interaction.RespondAsync("This event isn't accepting bets right now.", ephemeral: true);
            return;
        }

        var option = betEvent.Options.FirstOrDefault(x => x.Id == optionId);
        if (option is null)
            return;

        var modal = new ModalBuilder($"Bet on \"{option.Label}\"", $"betamount:{eventId}:{optionId}")
            .AddTextInput(
                label: "Amount to bet",
                customId: "amount",
                placeholder: "e.g. 500",
                required: true,
                minLength: 1,
                maxLength: 12)
            .Build();

        await interaction.RespondWithModalAsync(modal);
    }

    private async Task ModalSubmitted(SocketModal modal)
    {
        var parts = modal.Data.CustomId.Split(':');
        if (parts.Length != 3 || parts[0] != "betamount")
            return;

        if (!int.TryParse(parts[1], out var eventId) || !int.TryParse(parts[2], out var optionId))
            return;

        var rawAmount = modal.Data.Components.FirstOrDefault(x => x.CustomId == "amount")?.Value;

        if (!long.TryParse(rawAmount, out var amount) || amount <= 0)
        {
            await modal.RespondAsync("That's not a valid bet amount.", ephemeral: true);
            return;
        }

        var result = await _betService.PlaceBetAsync(eventId, optionId, modal.User.Id, amount);

        if (!result.Success)
        {
            var reason = result.ErrorReason switch
            {
                "insufficient_funds" => "You don't have enough currency for that bet.",
                "event_closed" => "Betting on this event has closed.",
                "already_bet_other_option" => "You've already bet on a different option in this event.",
                "amount_invalid" => "That's not a valid bet amount.",
                _ => "Something went wrong placing that bet.",
            };

            await modal.RespondAsync(reason, ephemeral: true);
            return;
        }

        await modal.DeferAsync();

        var betEvent = await _betService.GetEventAsync(eventId);
        await RefreshBoardMessageAsync(betEvent);
    }

    private async Task RefreshBoardMessageAsync(BetEvent betEvent)
    {
        var guild = _client.GetGuild(betEvent.GuildId);
        var channel = guild?.GetTextChannel(betEvent.ChannelId);
        if (channel is null)
            return;

        if (await channel.GetMessageAsync(betEvent.MessageId) is not IUserMessage msg)
            return;

        await msg.ModifyAsync(x =>
        {
            x.Embed = BetBoardHelper.BuildOpenEmbed(betEvent);
            x.Components = BetBoardHelper.BuildOpenComponents(betEvent);
        });
    }
}
