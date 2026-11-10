#nullable disable
using Discord;
using Discord.WebSocket;
using NadekoBot.Db.Models;
using NadekoBot.Modules.Gambling.Common;
using NadekoBot.Modules.Gambling.Services;

namespace NadekoBot.Modules.Gambling;

public partial class Gambling
{
    [Group]
    public partial class BetCommands : GamblingModule<IBetService>
    {
        private readonly DiscordSocketClient _client;

        public BetCommands(DiscordSocketClient client, GamblingConfigService gambService)
            : base(gambService)
        {
            _client = client;
        }

        // Usage: .betcreate "Will Team A win tonight's match?" Yes|No
        // Options are pipe-separated so you can have more than two (max 5).
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task BetCreate(string question, [Leftover] string pipeSeparatedOptions)
        {
            var options = pipeSeparatedOptions
                ?.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? Array.Empty<string>();

            if (string.IsNullOrWhiteSpace(question) || options.Length < 2 || options.Length > 5)
            {
                await ctx.ErrorAsync();
                return;
            }

            var betEvent = await _service.CreateEventAsync(ctx.Guild.Id, ctx.Channel.Id, ctx.User.Id, question, options);

            var embed = BetBoardHelper.BuildOpenEmbed(betEvent);
            var components = BetBoardHelper.BuildOpenComponents(betEvent);

            var msg = await ctx.Channel.SendMessageAsync(embed: embed, components: components);
            await _service.SetMessageIdAsync(betEvent.Id, msg.Id);
        }

        // Usage: .betlock <eventId>              -> locks immediately
        //        .betlock <eventId> <hours>       -> auto-locks after the given number of hours
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task BetLock(int eventId, double? hours = null)
        {
            var betEvent = await _service.GetEventAsync(eventId);
            if (betEvent is null || betEvent.GuildId != ctx.Guild.Id || betEvent.Status != BetEventStatus.Open)
            {
                await ctx.ErrorAsync();
                return;
            }

            if (hours is null or <= 0)
            {
                var succ = await _service.LockEventAsync(ctx.Guild.Id, eventId);
                if (!succ)
                {
                    await ctx.ErrorAsync();
                    return;
                }

                betEvent.Status = BetEventStatus.Locked;
                await UpdateBoardMessageAsync(
                    betEvent,
                    BetBoardHelper.BuildLockedEmbed(betEvent),
                    BetBoardHelper.BuildClosedComponents());
                await ctx.OkAsync();
            }
            else
            {
                var succ = await _service.ScheduleLockAsync(ctx.Guild.Id, eventId, TimeSpan.FromHours(hours.Value));
                if (!succ)
                {
                    await ctx.ErrorAsync();
                    return;
                }

                await ctx.Channel.SendMessageAsync(
                    $"✅ Event #{eventId} will automatically lock in {hours.Value:0.##} hour(s).");
            }
        }

        // Usage: .betresolve <eventId> <winning option label, exactly as typed at creation>
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task BetResolve(int eventId, [Leftover] string winningOptionLabel)
        {
            var betEvent = await _service.GetEventAsync(eventId);
            if (betEvent is null || betEvent.GuildId != ctx.Guild.Id)
            {
                await ctx.ErrorAsync();
                return;
            }

            var winningOption = betEvent.Options.FirstOrDefault(
                x => string.Equals(x.Label, winningOptionLabel, StringComparison.OrdinalIgnoreCase));

            if (winningOption is null)
            {
                await ctx.ErrorAsync();
                return;
            }

            var result = await _service.ResolveEventAsync(ctx.Guild.Id, eventId, winningOption.Id);
            if (!result.Success)
            {
                await ctx.ErrorAsync();
                return;
            }

            await UpdateBoardMessageAsync(
                betEvent,
                BetBoardHelper.BuildResolvedEmbed(betEvent, result),
                BetBoardHelper.BuildClosedComponents());
            await ctx.OkAsync();
        }

        // Usage: .betcancel <eventId>
        [Cmd]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.ManageGuild)]
        public async Task BetCancel(int eventId)
        {
            var betEvent = await _service.GetEventAsync(eventId);
            if (betEvent is null || betEvent.GuildId != ctx.Guild.Id)
            {
                await ctx.ErrorAsync();
                return;
            }

            var succ = await _service.CancelEventAsync(ctx.Guild.Id, eventId);
            if (!succ)
            {
                await ctx.ErrorAsync();
                return;
            }

            await UpdateBoardMessageAsync(
                betEvent,
                BetBoardHelper.BuildCancelledEmbed(betEvent),
                BetBoardHelper.BuildClosedComponents());
            await ctx.OkAsync();
        }

        private async Task UpdateBoardMessageAsync(BetEvent betEvent, Embed finalEmbed, MessageComponent components)
        {
            var guild = _client.GetGuild(betEvent.GuildId);
            var channel = guild?.GetTextChannel(betEvent.ChannelId);
            if (channel is null)
                return;

            if (await channel.GetMessageAsync(betEvent.MessageId) is not IUserMessage msg)
                return;

            await msg.ModifyAsync(x =>
            {
                x.Embed = finalEmbed;
                x.Components = components;
            });
        }
    }
}
