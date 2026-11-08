using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>Compacts each column downward around tiles that don't fall (bones), and records every move.</summary>
    public static class GravitySolver
    {
        public static void Apply(BoardState board, List<FallMove> moves)
        {
            for (int x = 0; x < board.Width; x++)
            {
                int writeY = board.Height - 1; // next free slot, scanning bottom to top

                for (int y = board.Height - 1; y >= 0; y--)
                {
                    var pos = new GridPos(x, y);
                    var def = board.DefinitionAt(pos);
                    if (def == null)
                        continue;

                    if (!def.AffectedByGravity)
                    {
                        writeY = y - 1; // a blocker starts a new segment above it
                        continue;
                    }

                    if (y != writeY)
                    {
                        while (writeY >= 0 && IsBlocker(board, new GridPos(x, writeY)))
                            writeY--;

                        if (writeY < 0)
                            break;

                        var target = new GridPos(x, writeY);
                        moves?.Add(new FallMove(pos, target));
                        board.Move(pos, target);
                    }

                    writeY--;
                }
            }
        }

        private static bool IsBlocker(BoardState board, GridPos p)
        {
            var def = board.DefinitionAt(p);
            return def != null && !def.AffectedByGravity;
        }
    }
}
