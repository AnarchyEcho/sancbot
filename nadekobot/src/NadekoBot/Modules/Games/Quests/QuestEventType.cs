namespace NadekoBot.Modules.Games.Quests;

public enum QuestEventType
{
    CommandUsed,
    GameWon,
    BetPlaced,
    FishCaught,
    PixelSet, // unused - setpixels quest disabled, kept for enum ordinal stability (stored in DB)
    RaceJoined,
    BankAction,
    PlantOrPick,
    Give,
    WaifuGiftSent, // unused - waifu quest disabled, kept for enum ordinal stability (stored in DB)
}