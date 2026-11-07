using System;
using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>Immutable rules for one kind of tile (an animal or an obstacle). Has no sprites or Unity data.</summary>
    public sealed class TileDefinition
    {
        public TileDefinition(string id, int points, bool canMatch, bool canSwap, bool affectedByGravity, float spawnWeight)
        {
            Id = id ?? string.Empty;
            Points = points;
            CanMatch = canMatch;
            CanSwap = canSwap;
            AffectedByGravity = affectedByGravity;
            SpawnWeight = spawnWeight;
        }

        /// <summary>Index in the owning <see cref="TileCatalog"/>. This is the runtime type of a tile.</summary>
        public int Type { get; internal set; } = -1;

        /// <summary>Stable string ID, used for goals, saves and achievements.</summary>
        public string Id { get; }
        public int Points { get; }
        public bool CanMatch { get; }
        public bool CanSwap { get; }
        public bool AffectedByGravity { get; }
        public float SpawnWeight { get; }
    }

    /// <summary>Registry of tile definitions. A tile's runtime type is its index here, so lookups are array reads.</summary>
    public sealed class TileCatalog
    {
        private readonly List<TileDefinition> _definitions = new List<TileDefinition>();

        public int Count => _definitions.Count;

        public int Add(TileDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition.Type >= 0) throw new InvalidOperationException($"Tile '{definition.Id}' is already registered.");

            definition.Type = _definitions.Count;
            _definitions.Add(definition);
            return definition.Type;
        }

        /// <summary>Returns the definition for a type, or null for an empty cell (-1).</summary>
        public TileDefinition Get(int type) => type >= 0 && type < _definitions.Count ? _definitions[type] : null;
    }

    /// <summary>Which tile types play the special roles in a level. -1 means the role isn't used.</summary>
    public sealed class SpecialTiles
    {
        public int Wolf = -1;
        public int Sheep = -1;
        public int Bone = -1;
        public int BlackSheep = -1;
    }
}
