using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>A black sheep blasted its row or column. A ClearStep for the line follows it.</summary>
    public sealed class LineBlastStep : TurnStep
    {
        public LineBlastStep(GridPos center, bool swipedVertically, IReadOnlyList<BlastWave> waves)
        {
            Center = center;
            SwipedVertically = swipedVertically;
            Waves = waves;
        }

        public GridPos Center { get; }
        /// <summary>A vertical swipe blasts the row; a horizontal swipe blasts the column.</summary>
        public bool SwipedVertically { get; }
        public IReadOnlyList<BlastWave> Waves { get; }
    }
}
