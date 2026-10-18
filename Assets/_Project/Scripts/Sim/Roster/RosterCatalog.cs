namespace Reforged.Sim
{
    public enum RosterRole : byte { Hero, Unlockable, AssistOnly }

    public sealed class RosterEntry
    {
        public readonly string Name;
        public readonly RosterRole Role;
        public readonly string Archetype;

        public RosterEntry(string name, RosterRole role, string archetype)
        {
            Name = name;
            Role = role;
            Archetype = archetype;
        }
    }

    /// <summary>
    /// Everyone who appears on the character-select screen, in display order. A name is only
    /// selectable once the ContentDatabase has a HeroDef for it; the rest show as "coming soon".
    /// </summary>
    public static class RosterCatalog
    {
        public static readonly RosterEntry[] All =
        {
            new RosterEntry(GrayboxContent.CaptainAmerica, RosterRole.Hero, "All-rounder"),
            new RosterEntry(GrayboxContent.IronMan, RosterRole.Hero, "Zoner"),
            new RosterEntry("Thor", RosterRole.Hero, "Heavy / AoE"),
            new RosterEntry("Hulk", RosterRole.Hero, "Grappler"),
            new RosterEntry("Black Widow", RosterRole.Hero, "Rushdown"),
            new RosterEntry("Hawkeye", RosterRole.Hero, "Tactical"),
            new RosterEntry("Black Panther", RosterRole.Hero, "Counter"),
            new RosterEntry("Captain Marvel", RosterRole.Hero, "Power"),
            new RosterEntry("Ant-Man", RosterRole.Hero, "Shape-shifter"),
            new RosterEntry("Vision", RosterRole.Hero, "Phase-shifter"),
            new RosterEntry("Spider-Man", RosterRole.Unlockable, "Aerial"),
            new RosterEntry("Scarlet Witch", RosterRole.Unlockable, "Controller"),
            new RosterEntry("Wasp", RosterRole.Unlockable, "Size-shifter"),
            new RosterEntry("Falcon", RosterRole.Unlockable, "Flyer"),
            new RosterEntry(GrayboxContent.NickFury, RosterRole.AssistOnly, "Assist"),
            new RosterEntry(GrayboxContent.WarMachine, RosterRole.AssistOnly, "Assist"),
            new RosterEntry("Shuri", RosterRole.AssistOnly, "Assist"),
            new RosterEntry("Okoye", RosterRole.AssistOnly, "Assist"),
        };
    }
}
