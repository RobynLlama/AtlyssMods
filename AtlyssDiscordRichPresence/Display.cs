namespace Marioalexsan.AtlyssDiscordRichPresence;

using static States;

public class Display
{
    // Chip Lv10 (100/100 HP)
    public string PlayerAlive { get; set; } = $"{{{PLAYERNAME}}} Lv{{{LVL}}} ({{{HP}}}/{{{MAXHP}}} HP)";
    internal const string PlayerAliveNote = "Text to display for player stats while alive.";

    // Chip Lv10 (Fainted)
    public string PlayerDead { get; set; } = $"{{{PLAYERNAME}}} Lv{{{LVL}}} (Fainted)";
    internal const string PlayerDeadNote = "Text to display for player stats while dead.";

    // In Main Menu
    public string MainMenu { get; set; } = $"In Main Menu";
    internal const string MainMenuNote = "Text to display while you're in the main menu.";

    // Exploring Sanctum
    public string Exploring { get; set; } = $"Exploring {{{WORLDAREA}}}";
    internal const string ExploringNote = "Text to display while exploring the world.";

    public string Idle { get; set; } = $"Idle in {{{WORLDAREA}}}";
    internal const string IdleNote = "Text to display while being idle in the world.";

    // Fighting in Sanctum Catacombs
    public string FightingInArena { get; set; } = $"Fighting in {{{WORLDAREA}}}";
    internal const string FightingInArenaNote = "Text to display while an arena is active.";

    // Fighting a boss in Sanctum Catacombs
    public string FightingBoss { get; set; } = $"Fighting a boss in {{{WORLDAREA}}}";
    internal const string FightingBossNote = "Text to display while a boss is active.";
}
