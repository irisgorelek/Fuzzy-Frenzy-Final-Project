using System;

namespace FuzzyFrenzy.Domain
{
    /// <summary>One tile on the board: its type and a unique ID it keeps for its whole life, including while it falls.</summary>
    public readonly struct Tile : IEquatable<Tile>
    {
        public static readonly Tile Empty = new Tile(-1, 0);

        public readonly int Type;
        public readonly int Id;

        public Tile(int type, int id)
        {
            Type = type;
            Id = id;
        }

        public bool IsEmpty => Type < 0;

        public bool Equals(Tile other) => Type == other.Type && Id == other.Id;
        public override bool Equals(object obj) => obj is Tile other && Equals(other);
        public override int GetHashCode() => (Type * 397) ^ Id;
    }
}
