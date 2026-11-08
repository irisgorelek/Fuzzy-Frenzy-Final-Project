namespace FuzzyFrenzy.Domain
{
    public readonly struct CollectGoal
    {
        public readonly string TileId;
        public readonly int Amount;

        public CollectGoal(string tileId, int amount)
        {
            TileId = tileId;
            Amount = amount;
        }
    }
}
