namespace NadekoBot.Modules.Games.Quests;

public sealed class BetFlowersQuest : IQuest
{
    public QuestIds QuestId => QuestIds.BetFlowers;

    public string Name => "Cookies Gambler";

    public string Desc => "Bet 1000 cookies";

    public string ProgDesc => "cookies bet";

    public QuestEventType EventType => QuestEventType.BetPlaced;

    public long RequiredAmount => 1000;

    public long TryUpdateProgress(IDictionary<string, string> metadata, long oldProgress)
    {
        if (
            !metadata.TryGetValue("amount", out var amountStr)
            || !long.TryParse(amountStr, out var amount)
        )
            return oldProgress;

        return oldProgress + amount;
    }
}
