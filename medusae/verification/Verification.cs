using Discord;
using NadekoBot.Medusa;

public sealed class Verification : Snek
{
    [cmd(["verifycreate", "vfycreate"])]
    [bot_owner_onlyAttribute]
    [prio(99)]
    public async Task VerifyCreate(AnyContext ctx)
    {
        string veriTitle =
            "<:BCatHeartlove:805524754907136030> Welcome to the server! <:DeerHeartLove:776472886860054578>";
        string veriText =
            """Once you've read the <#782684464283123753> and you're ready to get verified, please go ahead and click the button below to start the verification process. You'll be asked a series of questions which we ask that you please put a bit of effort into, as you may get rejected if your answers are too simple or too short. Once you're done a staff member will look over the application and verify you shortly, so please be patient (**be sure to have your dms open**)! <:YeenBoop:813374880207994920>""";
        ButtonBuilder btnBuilder = new ButtonBuilder(
            "Verify",
            $"verifyBtn",
            Discord.ButtonStyle.Success
        );

        ComponentBuilderV2 builder = new ComponentBuilderV2();
        builder
            .WithTextDisplay(
                $"""
                # {veriTitle}

                {veriText}
                """
            )
            .WithActionRow([btnBuilder]);

        MessageComponent embed = builder.Build();

        await ctx.Message.DeleteAsync();
        var msg = await ctx.Channel.SendMessageAsync(components: embed);
    }
}
