using System;

namespace FuzzyFrenzy.Domain
{
    /// <summary>Immutable rules for one kind of tile (an animal or an obstacle). Has no sprites or Unity data.</summary>
    public sealed class TileDefinition
    {
        public TileDefinition(string id, int points, bool canMatch, bool canSwap, bool affectedByGravity, float spawnWeight,
                              TileTraits traits = TileTraits.None, string eatsId = null, string leavesId = null, string matchGroupId = null, 
                              string nextTurnFormId = null, string permanentAwakeFormId = null)
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
            MatchGroupId = string.IsNullOrEmpty(matchGroupId) ? null : matchGroupId;
            NextTurnFormId = string.IsNullOrEmpty(nextTurnFormId) ? null : nextTurnFormId;
            PermanentAwakeFormId = string.IsNullOrEmpty(permanentAwakeFormId) ? null : permanentAwakeFormId;
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

        public string MatchGroupId { get; }

        public string NextTurnFormId { get; }

        public string PermanentAwakeFormId { get; }


        public bool Has(TileTraits trait) => (Traits & trait) == trait;

        public bool Eats(TileDefinition other) => EatsId != null && other != null && other.Id == EatsId;

        public bool CanMatchWith(TileDefinition other)
        {
            if (other == null)
                return false;

            if (!CanMatch || !other.CanMatch)
                return false;

            // Tiles in a match group match the whole group (every pig form has MatchGroupId "pig").
            // Tiles without one match only their own kind. CanMatch still turns matching off, as for sleeping pigs.
            if (MatchGroupId != null || other.MatchGroupId != null)
                return MatchGroupId == other.MatchGroupId;

            return ReferenceEquals(this, other);
        }
    }
}
