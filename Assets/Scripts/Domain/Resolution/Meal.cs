namespace FuzzyFrenzy.Domain
{
    /// <summary>A predator eating the tile next to it (a wolf eating a sheep).</summary>
    public readonly struct Meal
    {
        public readonly GridPos Predator;
        public readonly GridPos Prey;
        /// <summary>What the prey turns into, or -1 to leave the cell empty.</summary>
        public readonly int RemainsType;

        public Meal(GridPos predator, GridPos prey, int remainsType)
        {
            Predator = predator;
            Prey = prey;
            RemainsType = remainsType;
        }
    }
}
