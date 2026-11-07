using System;
using System.Collections.Generic;

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

    /// <summary>
    /// The authoritative board. Owns the grid and level-wide board settings (spawn pool, special roles).
    /// Knows nothing about scoring, goals, animation, audio or Unity.
    /// </summary>
    public sealed class BoardState
    {
        private readonly Tile[,] _cells;
        private int _nextTileId = 1;

        public BoardState(TileCatalog catalog, int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Board must be at least 1x1.");

            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Width = width;
            Height = height;
            _cells = new Tile[width, height];

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _cells[x, y] = Tile.Empty;

            Scratch = new BoardScratch(width * height);
        }

        /// <summary>Reusable buffers for the searches that run many times per turn, so they don't allocate.</summary>
        internal BoardScratch Scratch { get; }

        public TileCatalog Catalog { get; }
        public int Width { get; }
        public int Height { get; }
        public SpecialTiles Specials { get; } = new SpecialTiles();

        /// <summary>Tile types refill and level generation pick from.</summary>
        public List<int> SpawnPool { get; } = new List<int>();

        /// <summary>When set, the next refill places exactly one black sheep.</summary>
        public bool BlackSheepArmed { get; set; }

        /// <summary>Tile types the next refills use before random picks. For tests and scripted boards.</summary>
        public Queue<int> ForcedSpawns { get; } = new Queue<int>();

        public bool InBounds(GridPos p) => p.X >= 0 && p.X < Width && p.Y >= 0 && p.Y < Height;

        public Tile Get(GridPos p) => InBounds(p) ? _cells[p.X, p.Y] : Tile.Empty;

        public int TypeAt(GridPos p) => Get(p).Type;

        public TileDefinition DefinitionAt(GridPos p) => Catalog.Get(TypeAt(p));

        public bool IsEmpty(GridPos p) => Get(p).IsEmpty;

        public bool IsMatchable(GridPos p)
        {
            var def = DefinitionAt(p);
            return def != null && def.CanMatch;
        }

        public bool IsBone(GridPos p) => Specials.Bone >= 0 && TypeAt(p) == Specials.Bone;

        /// <summary>
        /// True when the tile at p is a black sheep. Compared by string ID, so a black sheep placed by a
        /// debug tool counts even if it is a different asset from the level's own.
        /// </summary>
        public bool IsBlackSheep(GridPos p) => IsBlackSheepType(TypeAt(p));

        public bool IsBlackSheepType(int type)
        {
            if (type < 0 || Specials.BlackSheep < 0) return false;
            if (type == Specials.BlackSheep) return true;
            string id = Catalog.Get(Specials.BlackSheep).Id;
            return id.Length > 0 && Catalog.Get(type).Id == id;
        }

        /// <summary>Places a new tile (with a new ID). A negative type empties the cell.</summary>
        public void Place(GridPos p, int type)
        {
            if (!InBounds(p)) return;
            _cells[p.X, p.Y] = type < 0 ? Tile.Empty : new Tile(type, _nextTileId++);
        }

        public void Clear(GridPos p)
        {
            if (InBounds(p)) _cells[p.X, p.Y] = Tile.Empty;
        }

        /// <summary>Moves a tile, keeping its ID, and leaves the source empty.</summary>
        public void Move(GridPos from, GridPos to)
        {
            _cells[to.X, to.Y] = _cells[from.X, from.Y];
            _cells[from.X, from.Y] = Tile.Empty;
        }

        public void Swap(GridPos a, GridPos b)
        {
            (_cells[a.X, a.Y], _cells[b.X, b.Y]) = (_cells[b.X, b.Y], _cells[a.X, a.Y]);
        }

        /// <summary>Writes a tile back exactly as it was (same ID). Used by shuffles.</summary>
        internal void Put(GridPos p, Tile tile) => _cells[p.X, p.Y] = tile;

        public bool HasEmptyCell()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (_cells[x, y].IsEmpty) return true;
            return false;
        }

        public BoardSnapshot Snapshot()
        {
            var types = new int[Width * Height];
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    types[y * Width + x] = _cells[x, y].Type;
            return new BoardSnapshot(Width, Height, types);
        }
    }

    /// <summary>
    /// Pooled working memory for one board. Each buffer has a single user at a time; callers take it,
    /// use it and are done before returning, so nothing here is shared across calls.
    /// </summary>
    internal sealed class BoardScratch
    {
        private readonly bool[] _seen;

        public BoardScratch(int cellCount)
        {
            _seen = new bool[cellCount];
        }

        public readonly List<SwapMove> Moves = new List<SwapMove>();
        public readonly List<GridPos> ShuffleCells = new List<GridPos>();
        public readonly List<Tile> ShuffleTiles = new List<Tile>();

        /// <summary>A cleared per-cell flag array, indexed y * width + x.</summary>
        public bool[] BeginSeen()
        {
            Array.Clear(_seen, 0, _seen.Length);
            return _seen;
        }
    }

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
