namespace FuzzyFrenzy.Domain
{
    /// <summary>How often a special tile (the black sheep) may be armed for the next refill.</summary>
    public readonly struct SpecialSpawnSettings
    {
        public static SpecialSpawnSettings None => new SpecialSpawnSettings(-1, 0, 0f);

        public SpecialSpawnSettings(int type, int everyMoves, float chance)
        {
            Type = type;
            EveryMoves = everyMoves;
            Chance = chance;
        }

        /// <summary>The tile type to arm, or -1 when the level has none.</summary>
        public int Type { get; }
        public bool Enabled => Type >= 0;
        /// <summary>Roll once every this many moves. 0 or less disables rolling.</summary>
        public int EveryMoves { get; }
        /// <summary>Chance from 0 to 1 for each roll.</summary>
        public float Chance { get; }
    }
}
