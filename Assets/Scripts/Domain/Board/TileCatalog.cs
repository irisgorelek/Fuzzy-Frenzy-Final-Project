using System;
using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
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

        public bool CanMatch(int firstType, int secondType)
        {
            TileDefinition first = Get(firstType);
            TileDefinition second = Get(secondType);

            return first?.CanMatchWith(second) == true;
        }
    }
}
