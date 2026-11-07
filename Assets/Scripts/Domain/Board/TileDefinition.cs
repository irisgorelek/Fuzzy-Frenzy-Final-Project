using System;
using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>
    /// Behaviours a tile can have. Rules check traits, never particular animals, so a new animal or obstacle
    /// with an existing behaviour is just data.
    /// </summary>
    [Flags]
    public enum TileTraits
    {
        None = 0,
        /// <summary>Breaks when a tile next to it is cleared (bones).</summary>
        BreaksNextToClear = 1 << 0,
        /// <summary>Line blasts and boosters pass over it without clearing it (bones).</summary>
        ResistsBlasts = 1 << 1,
        /// <summary>May be on the starting board but never refills. It appears later only through gameplay.</summary>
        NoRefill = 1 << 2,
        /// <summary>Swapping it blasts a row or column (black sheep).</summary>
        LineBlastOnSwap = 1 << 3,
    }

    /// <summary>Immutable rules for one kind of tile (an animal or an obstacle). Has no sprites or Unity data.</summary>
    public sealed class TileDefinition
    {
        public TileDefinition(string id, int points, bool canMatch, bool canSwap, bool affectedByGravity, float spawnWeight,
            TileTraits traits = TileTraits.None, string eatsId = null, string leavesId = null)
        {
            Id = id ?? string.Empty;
            Points = points;
            CanMatch = canMatch;
            CanSwap = canSwap;
            AffectedByGravity = affectedByGravity;
            SpawnWeight = spawnWeight;
            Traits = traits;
            EatsId = string.IsNullOrEmpty(eatsId) ? null : eatsId;
            LeavesId = string.IsNullOrEmpty(leavesId) ? null : leavesId;
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
        public TileTraits Traits { get; }

        /// <summary>ID of the tile this one eats when they are next to each other on a settled board, or null.</summary>
        public string EatsId { get; }

        /// <summary>ID of the tile left where the eaten tile was (the wolf leaves bones), or null to leave the cell empty.</summary>
        public string LeavesId { get; }

        public bool Has(TileTraits trait) => (Traits & trait) == trait;

        public bool Eats(TileDefinition other) => EatsId != null && other != null && other.Id == EatsId;
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

        /// <summary>The first registered type with this ID, or -1.</summary>
        public int TypeOfId(string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            for (int i = 0; i < _definitions.Count; i++)
                if (_definitions[i].Id == id) return i;
            return -1;
        }
    }
}
