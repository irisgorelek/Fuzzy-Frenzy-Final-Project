namespace FuzzyFrenzy.Domain
{
    public readonly struct DestroyedCount
    {
        public readonly string Id;
        public readonly int Count;

        public DestroyedCount(string id, int count)
        {
            Id = id;
            Count = count;
        }
    }
}
