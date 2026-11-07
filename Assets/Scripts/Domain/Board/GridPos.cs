using System;

namespace FuzzyFrenzy.Domain
{
    /// <summary>A cell on the board. x is the column, y is the row counted from the top (y = 0 is the top row).</summary>
    public readonly struct GridPos : IEquatable<GridPos>
    {
        public readonly int X;
        public readonly int Y;

        public GridPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static readonly GridPos Up = new GridPos(0, 1);
        public static readonly GridPos Down = new GridPos(0, -1);
        public static readonly GridPos Left = new GridPos(-1, 0);
        public static readonly GridPos Right = new GridPos(1, 0);

        /// <summary>Neighbour order used by every adjacency rule. The order is part of the game rules (it decides which wolf eats which sheep).</summary>
        public static readonly GridPos[] Orthogonal = { Up, Down, Left, Right };

        public bool IsAdjacentTo(GridPos other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y) == 1;

        public static GridPos operator +(GridPos a, GridPos b) => new GridPos(a.X + b.X, a.Y + b.Y);
        public static GridPos operator -(GridPos a, GridPos b) => new GridPos(a.X - b.X, a.Y - b.Y);
        public static bool operator ==(GridPos a, GridPos b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(GridPos a, GridPos b) => !(a == b);

        public bool Equals(GridPos other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridPos other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public override string ToString() => $"({X}, {Y})";
    }
}
