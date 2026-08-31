namespace NadekoBot.Modules.Games.Quests;

public sealed class GiveFlowersQuest : IQuest
{
    public QuestIds QuestId
        => QuestIds.GiveFlowers;

    public string Name
        => "Sharing is Caring";

    public string Desc
        => "Give 100 cookies to someone";

    public string ProgDesc
        => "cookies given";

    public QuestEventType EventType
        => QuestEventType.Give;

    public long RequiredAmount
        => 100;

    public long TryUpdateProgress(IDictionary<string, string> metadata, long oldProgress)
    {
        if (!metadata.TryGetValue("amount", out var amountStr)
            || !long.TryParse(amountStr, out var amount))
            return oldProgress;

        return oldProgress + amount;
    }
}