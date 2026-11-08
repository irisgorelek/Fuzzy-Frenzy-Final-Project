namespace FuzzyFrenzy.Domain
{
    /// <summary>
    /// The board's tile types at one moment in a turn. Turn steps carry these so the view can draw
    /// each moment correctly while the real board has already moved on.
    /// </summary>
    public sealed class BoardSnapshot
    {
        private readonly int[] _types;

        public BoardSnapshot(int width, int height, int[] types)
        {
            Width = width;
            Height = height;
            _types = types;
        }

        public int Width { get; }
        public int Height { get; }

        public int TypeAt(GridPos p) =>
            p.X >= 0 && p.X < Width && p.Y >= 0 && p.Y < Height ? _types[p.Y * Width + p.X] : -1;
    }
}
