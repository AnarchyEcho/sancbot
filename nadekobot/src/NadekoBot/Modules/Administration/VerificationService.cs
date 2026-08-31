#nullable disable
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Discord.Interactions;
using NadekoBot.Common.ModuleBehaviors;

namespace NadekoBot.Modules.Administration;

public class VerificationService
    : InteractionModuleBase<SocketInteractionContext<SocketMessageComponent>>,
        INService,
        IReadyExecutor
{
    private readonly DiscordSocketClient _client;

    public VerificationService(DiscordSocketClient client)
    {
        _client = client;
    }

    // testing pending id: 1128430200766607420
    // actual pending id: 819388590340571137
    ulong pendingId = 819388590340571137;

    // testing archive id: 1128430200766607416
    // actual archive id: 819388851977322496
    ulong archiveId = 819388851977322496;

    // testing verifyHere channel id: 1128430200766607414
    // actual verifyHere channel id: 819389997777551370
    ulong verifyHereId = 819389997777551370;

    // testing welcome channel id: 1128430201261527079
    // actual welcome channel id: 757552802703605801
    ulong welcomeChanId = 757552802703605801;

    // testing verified id: 1128430198178725992
    // actual verified id: 817144618093248583
    ulong verifiedRoleId = 817144618093248583;

    // testing verified nsfw id: 1128430198145167453
    // actual verified nsfw id: 757556140279070780
    ulong verifiedNsfwRoleId = 757556140279070780;

    // testing welcome id: 1128430198212268244
    // actual welcome id: 804072986570260521
    ulong welcomeRoleId = 804072986570260521;

    // testing roles channel id: 1128430200506552430
    // actual roles channel id: 879490219785732117
    ulong rolesChanId = 879490219785732117;

    // testing nsfw roles channel id: 1128430200506552431
    // actual nsfw roles channel id: 879497988467204106
    ulong rolesNsfwChanId = 879497988467204106;

    private string qOne = "Where did you find us / Who invited you?";
    private string qTwo = "How did you join the furry community?";
    private string qThree = "Do you have a sona? Tell us about them.";
    private string qFour = "How old are you?";
    private string qFive = "Secret password? (See #welcome-rules)";

    public Task OnReadyAsync()
    {
        _client.ButtonExecuted += ButtonExecuted;

        _client.SelectMenuExecuted += SelectMenuExecuted;

        _client.ModalSubmitted += ModalSubmitted;

        return Task.CompletedTask;
    }

    private async Task ButtonExecuted(SocketMessageComponent interaction)
    {
        SocketGuild guild = _client.GetGuild((ulong)interaction.GuildId);
        if (interaction.Data.CustomId == $"verifyBtn")
        {
            await interaction.RespondWithModalAsync(await ModalCreate(interaction.User.Id));
            return;
        }
        else
        {
            if (
                interaction.Data.CustomId.Contains("rejectBtn")
                && interaction.Data.CustomId == $"rejectBtn{await GetUserId(interaction)}"
            )
            {
                await interaction.RespondWithModalAsync(
                    await RejectModalCreate(await GetUserId(interaction))
                );
            }

            SocketTextChannel archive = guild.GetTextChannel(archiveId);
            if (
                interaction.Data.CustomId.Contains("approveBtn")
                && interaction.Data.CustomId == $"approveBtn{await GetUserId(interaction)}"
            )
            {
                await AcceptVerification(
                    userId: await GetUserId(interaction),
                    adult: false,
                    inter: interaction
                );
                await archive.SendMessageAsync(
                    components: await SubmitArchive(
                        userId: await GetUserId(interaction),
                        adult: false,
                        inter: interaction
                    ),
                    allowedMentions: new AllowedMentions(AllowedMentionTypes.None)
                );

                SocketTextChannel pending = guild.GetTextChannel(pendingId);
                await pending.DeleteMessageAsync(interaction.Message.Id);
                await interaction.DeferAsync();
            }
            if (
                interaction.Data.CustomId.Contains("approveAdultBtn")
                && interaction.Data.CustomId == $"approveAdultBtn{await GetUserId(interaction)}"
            )
            {
                await AcceptVerification(
                    userId: await GetUserId(interaction),
                    adult: true,
                    inter: interaction
                );
                await archive.SendMessageAsync(
                    components: await SubmitArchive(
                        userId: await GetUserId(interaction),
                        adult: true,
                        inter: interaction
                    ),
                    allowedMentions: new AllowedMentions(AllowedMentionTypes.None)
                );

                SocketTextChannel pending = guild.GetTextChannel(pendingId);
                await pending.DeleteMessageAsync(interaction.Message.Id);
                await interaction.DeferAsync();
            }
        }
    }

    private async Task SelectMenuExecuted(SocketMessageComponent components)
    {
        if (
            components.Data.CustomId.Contains("rejectSelect")
            && components.Data.CustomId == $"rejectSelect{await GetUserId(components)}"
        )
        {
            await components.RespondWithModalAsync(
                await RejectModalCreate(await GetUserId(components), components.Data.Values.First())
            );
        }
    }

    private async Task ModalSubmitted(SocketModal modal)
    {
        SocketGuild guild = _client.GetGuild((ulong)modal.GuildId);
        if (modal.Data.CustomId == $"verifyModal{modal.User.Id}")
        {
            List<SocketMessageComponentData> components = modal.Data.Components.ToList();
            SocketTextChannel pending = guild.GetTextChannel(pendingId);

            if (pending is not null)
            {
                await pending.SendMessageAsync(
                    components: await ModalSubmitPending(modal, components, modal.User.Id, false),
                    allowedMentions: new AllowedMentions(AllowedMentionTypes.None)
                );
            }
            await modal.DeferAsync();
        }
        if (
            (modal.Data.CustomId.Contains("rejectModal"))
            && modal.Data.CustomId == $"rejectModal{await GetUserId(modal)}"
        )
        {
            List<IMessageComponent> components = modal.Message.Components.ToList();

            await ModalSubmitArchive(
                modal,
                components,
                await GetUserId(modal),
                true,
                modal.Data.Components.ToList()
            );
        }
    }

    private async Task<Modal> ModalCreate(ulong userId)
    {
        var modal = new ModalBuilder("Verification", $"verifyModal{userId}");

        modal.AddTextInput(label: qOne, customId: $"qOne{userId}", required: true, maxLength: 500);
        modal.AddTextInput(
            label: qTwo,
            customId: $"qTwo{userId}",
            required: true,
            style: Discord.TextInputStyle.Paragraph,
            maxLength: 1566
        );
        modal.AddTextInput(
            label: qThree,
            customId: $"qThree{userId}",
            required: true,
            style: Discord.TextInputStyle.Paragraph,
            maxLength: 1566
        );
        modal.AddTextInput(
            label: qFour,
            customId: $"qFour{userId}",
            required: true,
            minLength: 1,
            maxLength: 3
        );
        modal.AddTextInput(
            label: qFive,
            customId: $"qFive{userId}",
            required: true,
            minLength: 1,
            maxLength: 20
        );
        return modal.Build();
    }

    private async Task<MessageComponent> ModalSubmitPending(
        SocketModal modal,
        List<SocketMessageComponentData> components,
        ulong userId,
        bool lastStep,
        [Optional] List<SocketMessageComponentData> rejectionReason
    )
    {
        ComponentBuilderV2 pendingMessage = new ComponentBuilderV2();

        if (modal.User.GetAvatarUrl() is not null)
        {
            pendingMessage.WithTextDisplay(
                $"""
                # Verification for {modal.User.Mention}
                """
            );
            SectionBuilder details = new SectionBuilder();
            details.WithTextDisplay(
                $"""
                -# Username: {modal.User.Username}, Id: {modal.User.Id}
                """,
                16
            );
            details.WithAccessory(new ThumbnailBuilder(media: modal.User.GetAvatarUrl()));
            pendingMessage.WithSection(details);
        }
        else
        {
            pendingMessage.WithTextDisplay(
                $"""
                # Verification for {modal.User.Mention}
                -# Username: {modal.User.Username}, Id: {modal.User.Id}
                """,
                16
            );
        }

        if (lastStep)
        {
            pendingMessage.WithTextDisplay(
                $"""
                ## {qOne}
                > {components.First(x => x.CustomId == $"qOne{userId}").Value}

                ## {qTwo}
                > {components.First(x => x.CustomId == $"qTwo{userId}").Value}

                ## {qThree}
                > {components.First(x => x.CustomId == $"qThree{userId}").Value}

                ## {qFour}
                > {components.First(x => x.CustomId == $"qFour{userId}").Value}

                ## {qFive}
                > {components.First(x => x.CustomId == $"qFive{userId}").Value}

                ## Rejection Reason
                > {components.First(x => x.CustomId == $"rejectionReason{userId}").Value}
                """
            );
        }
        else
        {
            pendingMessage.WithTextDisplay(
                $"""
                ## {qOne}
                > {components.First(x => x.CustomId == $"qOne{userId}").Value.ReplaceLineEndings(
                    System.Environment.NewLine + "> "
                )}
                ## {qTwo}
                > {components.First(x => x.CustomId == $"qTwo{userId}").Value.ReplaceLineEndings(
                    System.Environment.NewLine + "> "
                )}
                ## {qThree}
                > {components.First(x => x.CustomId == $"qThree{userId}").Value.ReplaceLineEndings(
                    System.Environment.NewLine + "> "
                )}
                ## {qFour}
                > {components.First(x => x.CustomId == $"qFour{userId}").Value.ReplaceLineEndings(
                    System.Environment.NewLine + "> "
                )}
                ## {qFive}
                > {components.First(x => x.CustomId == $"qFive{userId}").Value.ReplaceLineEndings(
                    System.Environment.NewLine + "> "
                )}
                """,
                55
            );
            pendingMessage.WithTextDisplay(
                $"""
                -# Reject will pop up a modal so you can write the rejection reason.
                -# You can only select a template ONCE, discord limitation, changing does nothing.
                """
            );

            ActionRowBuilder actionRow = new ActionRowBuilder();
            actionRow.WithButton(
                label: "Approve",
                customId: $"approveBtn{userId}",
                style: ButtonStyle.Success,
                emote: new Emoji("✅")
            );
            // actionRow.WithButton(
            //     label: "Approve (18+)",
            //     customId: $"approveAdultBtn{userId}",
            //     style: ButtonStyle.Secondary,
            //     emote: new Emoji("🔞")
            // );
            actionRow.WithButton(
                label: "Reject",
                customId: $"rejectBtn{userId}",
                style: ButtonStyle.Danger,
                emote: new Emoji("✖️")
            );
            pendingMessage.WithActionRow(actionRow);

            ActionRowBuilder actionRowTwo = new ActionRowBuilder();
            actionRowTwo.WithSelectMenu(
                customId: $"rejectSelect{userId}",
                type: ComponentType.SelectMenu,
                placeholder: "Reject With Template",
                options:
                [
                    new SelectMenuOptionBuilder(label: "No reason", value: "No reason"),
                    new SelectMenuOptionBuilder(label: "Duplicate", value: "Duplicate"),
                    new SelectMenuOptionBuilder(
                        label: "Incorrect Password",
                        value: "Wrong password. Please check rule 7 in the Welcome Rules for the correct one."
                    ),
                    new SelectMenuOptionBuilder(
                        label: "Incorrect Server Invite Source",
                        value: "Your invite source doesn't match our logs. Where did you find us or get the link?"
                    ),
                    new SelectMenuOptionBuilder(
                        label: "User Left/Banned",
                        value: "User left or was banned"
                    ),
                    new SelectMenuOptionBuilder(
                        label: "Too little info",
                        value: "Please provide more information."
                    ),
                ]
            );
            pendingMessage.WithActionRow(actionRowTwo);
        }

        return pendingMessage.Build();
    }

    private async Task<MessageComponent> SubmitArchive(
        ulong userId,
        bool adult,
        SocketMessageComponent inter
    )
    {
        SocketGuildUser user = _client.GetGuild((ulong)inter.GuildId).GetUser(userId);
        ComponentBuilderV2 archiveMessage = new ComponentBuilderV2();

        if (user.GetAvatarUrl() is not null)
        {
            archiveMessage.WithTextDisplay(
                $"""
                # Verification for {user.Mention}
                """
            );
            SectionBuilder details = new SectionBuilder();
            details.WithTextDisplay(
                $"""
                -# Username: {user.Username}, Id: {userId}
                """,
                16
            );
            details.WithAccessory(new ThumbnailBuilder(media: user.GetAvatarUrl()));
            archiveMessage.WithSection(details);
        }
        else
        {
            archiveMessage.WithTextDisplay(
                $"""
                # Verification for {user.Mention}
                -# Username: {user.Username}, Id: {userId}
                """,
                16
            );
        }

        string msg = ((TextDisplayComponent)inter.Message.Components.FindComponentById(55)).Content;
        // just in case @"\?\n(?<qOne>\S+)[\n\w\s]+\?\n(?<qTwo>\S+)[\n\w\s\?]+\.\n(?<qThree>\S+)[\n\w\s]+\?\n(?<qFour>\S+)[\n\w\s]+.+\n(?<qFive>\S+)"i

        string approveType = adult ? "Approved (18+)" : "Approved";
        archiveMessage.WithTextDisplay(
            $"""
            {msg}
            ### {approveType} by {inter.User.Mention}
            """
        );

        return archiveMessage.Build();
    }

    private async Task ModalSubmitArchive(
        SocketModal modal,
        List<IMessageComponent> components,
        ulong userId,
        bool lastStep,
        [Optional] List<SocketMessageComponentData> rejectionReason
    )
    {
        SocketGuildUser user = _client.GetGuild((ulong)modal.GuildId).GetUser(userId);
        ComponentBuilderV2 archiveMessage = new ComponentBuilderV2();

        SocketGuild guild = _client.GetGuild((ulong)modal.GuildId);

        SocketTextChannel archive = guild.GetTextChannel(archiveId);
        SocketTextChannel pending = guild.GetTextChannel(pendingId);

        if (user is null)
        {
            archiveMessage.WithTextDisplay(
                $"""
                # Verification for @{userId} not possible, user not in server or left.
                """
            );
            await modal.DeferAsync();
            await archive.SendMessageAsync(
                components: archiveMessage.Build(),
                allowedMentions: new AllowedMentions(AllowedMentionTypes.None)
            );
            await pending.DeleteMessageAsync(modal.Message.Id);
            return;
        }
        if (user.GetAvatarUrl() is not null)
        {
            archiveMessage.WithTextDisplay(
                $"""
                # Verification for {user.Mention}
                """
            );
            SectionBuilder details = new SectionBuilder();
            details.WithTextDisplay(
                $"""
                -# Username: {user.Username}, Id: {userId}
                """,
                16
            );
            details.WithAccessory(new ThumbnailBuilder(media: user.GetAvatarUrl()));
            archiveMessage.WithSection(details);
        }
        else
        {
            archiveMessage.WithTextDisplay(
                $"""
                # Verification for {user.Mention}
                -# Username: {user.Username}, Id: {userId}
                """,
                16
            );
        }

        string msg = ((TextDisplayComponent)modal.Message.Components.FindComponentById(55)).Content;
        // just in case @"\?\n(?<qOne>\S+)[\n\w\s]+\?\n(?<qTwo>\S+)[\n\w\s\?]+\.\n(?<qThree>\S+)[\n\w\s]+\?\n(?<qFour>\S+)[\n\w\s]+.+\n(?<qFive>\S+)"i

        archiveMessage.WithTextDisplay(
            $"""
            {msg}
            ## Rejection Reason
            > {rejectionReason.First().Value}
            ### Rejected by {modal.User.Mention}
            """
        );

        SocketTextChannel verifyHere = guild.GetTextChannel(verifyHereId);

        await modal.DeferAsync();
        try
        {
            await (await user.CreateDMChannelAsync()).SendMessageAsync(
                $"""
                **Your application was __rejected__.**
                **Reason**: {rejectionReason.First().Value}
                Please resolve the issue above and re-submit your application.
                """
            );
            await archive.SendMessageAsync(
                components: archiveMessage.Build(),
                allowedMentions: new AllowedMentions(AllowedMentionTypes.None)
            );
            await pending.DeleteMessageAsync(modal.Message.Id);
        }
        catch (System.Exception)
        {
            await archive.SendMessageAsync(
                components: archiveMessage.Build(),
                allowedMentions: new AllowedMentions(AllowedMentionTypes.None)
            );
            await pending.DeleteMessageAsync(modal.Message.Id);
            var rej = await verifyHere.SendMessageAsync(
                $"""
                {user.Mention}
                ## Please open up your DMs, I can't message you.

                **Your application was __rejected__.**
                **Reason**: {rejectionReason.First().Value}
                Please resolve the issue above and re-submit your application.
                """,
                allowedMentions: new AllowedMentions(AllowedMentionTypes.Users)
            );
            await Task.Delay(30000)
                .ContinueWith(async _ =>
                {
                    await rej.DeleteAsync();
                });
        }
    }

    private async Task<Modal> RejectModalCreate(ulong userId, [Optional] string template)
    {
        var modal = new ModalBuilder("Rejection", $"rejectModal{userId}");

        if (template is not null)
        {
            modal.AddTextInput(
                label: "Rejection Reason",
                customId: $"rejectReason{userId}",
                style: TextInputStyle.Paragraph,
                value: template
            );
        }
        else
        {
            modal.AddTextInput(
                label: "Rejection Reason",
                customId: $"rejectReason{userId}",
                style: TextInputStyle.Paragraph
            );
        }
        return modal.Build();
    }

    private async Task AcceptVerification(ulong userId, bool adult, SocketMessageComponent inter)
    {
        SocketGuild guild = _client.GetGuild((ulong)inter.GuildId);
        SocketGuildUser user = guild.GetUser(userId);
        SocketTextChannel wlcmchnl = guild.GetTextChannel(welcomeChanId);
        SocketTextChannel rolesChnl = guild.GetTextChannel(rolesChanId);
        SocketTextChannel rolesChnlNsfw = guild.GetTextChannel(rolesNsfwChanId);
        var wlcmRole = guild.GetRole(welcomeRoleId);
        string condi = adult ? $" and {rolesChnlNsfw.Mention}" : "";

        if (adult)
        {
            await user.AddRolesAsync([verifiedRoleId, verifiedNsfwRoleId]);
        }
        else
        {
            await user.AddRoleAsync(verifiedRoleId);
        }
        await wlcmchnl.SendMessageAsync(
            $"""
            {wlcmRole.Mention}
            Please welcome {user.Mention} to the server! We hope you enjoy your stay with us!
            If you have any questions then please don't hesitate to ask!
            And don't forget to grab some roles in {rolesChnl.Mention} and {rolesChnlNsfw.Mention}!
            """
        );
        await SubmitArchive(userId, adult, inter);
    }

    private async Task<ulong> GetUserId(SocketMessageComponent entry)
    {
        var regex = new Regex(@"id: (?<id>\d+)", RegexOptions.IgnoreCase);
        string msg = ((TextDisplayComponent)entry.Message.Components.FindComponentById(16)).Content;
        var match = regex.Match(msg);
        ulong id = (ulong)(Decimal.Parse(match.Groups["id"].Value));
        return id;
    }

    private async Task<ulong> GetUserId(SocketModal entry)
    {
        var regex = new Regex(@"id: (?<id>\d+)", RegexOptions.IgnoreCase);
        string msg = ((TextDisplayComponent)entry.Message.Components.FindComponentById(16)).Content;
        var match = regex.Match(msg);
        ulong id = (ulong)(Decimal.Parse(match.Groups["id"].Value));
        return id;
    }
}
