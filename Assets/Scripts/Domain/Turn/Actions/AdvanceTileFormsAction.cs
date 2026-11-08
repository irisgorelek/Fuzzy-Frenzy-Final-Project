using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>
    /// Advances every configured turn-driven tile form.
    /// </summary>
    public sealed class AdvanceTileFormsAction : IBoardAction
    {
        public void Apply(TurnRun run)
        {
            var changes = new List<TileFormChange>();

            for (int x = 0; x < run.Board.Width; x++)
            {
                for (int y = 0; y < run.Board.Height; y++)
                {
                    var cell = new GridPos(x, y);

                    TileDefinition current = run.Board.DefinitionAt(cell);

                    if (current == null || !current.Has(TileTraits.AdvancesFormEachTurn))
                    {
                        continue;
                    }

                    int nextType = run.Board.Catalog.TypeOfId(current.NextTurnFormId);

                    if (nextType < 0)
                        continue;

                    int oldType = current.Type;

                    if (!run.Board.ChangeType(cell, nextType))
                    {
                        continue;
                    }

                    changes.Add(new TileFormChange(cell, oldType, nextType));
                }
            }

            if (changes.Count > 0)
            {
                run.Record(new TileFormsChangedStep(changes.ToArray()));
            }
        }
    }
}
