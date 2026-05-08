using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BoardControllerBlackSheepBlast
{
    public async Task AnimateFromCenter(Board board, BoardView view, BoardConfig cfg, Vector2Int center, bool swipedVertically)
    {
        // vertical swipe => ROW blast (left + right)
        // horizontal swipe => COLUMN blast (up + down)

        int maxWave = swipedVertically
            ? Mathf.Max(center.x, (cfg.weidth - 1) - center.x)
            : Mathf.Max(center.y, (cfg.height - 1) - center.y);

        if (AudioManager.instance != null && !swipedVertically) // PLay longer sound
        {
            AudioManager.instance.PlaySFXPitchAdjusted(17);
        }

        if (AudioManager.instance != null && swipedVertically) // Play shorter sound
        {
            AudioManager.instance.PlaySFXPitchAdjusted(18);
        }

        for (int wave = 0; wave <= maxWave; wave++)
        {
            var waveCells = new List<Vector2Int>();

            if (swipedVertically)
            {
                int y = center.y;

                int leftX = center.x - wave;
                int rightX = center.x + wave;

                if (leftX >= 0)
                {
                    var leftCell = new Vector2Int(leftX, y);
                    if (board.GetAnimalFromCell(leftCell) != cfg.boneBlock)
                        waveCells.Add(leftCell);
                }

                if (rightX < cfg.weidth && rightX != leftX)
                {
                    var rightCell = new Vector2Int(rightX, y);
                    if (board.GetAnimalFromCell(rightCell) != cfg.boneBlock)
                        waveCells.Add(rightCell);
                }
            }
            else
            {
                int x = center.x;

                int downY = center.y - wave;
                int upY = center.y + wave;

                if (downY >= 0)
                {
                    var downCell = new Vector2Int(x, downY);
                    if (board.GetAnimalFromCell(downCell) != cfg.boneBlock)
                        waveCells.Add(downCell);
                }

                if (upY < cfg.height && upY != downY)
                {
                    var upCell = new Vector2Int(x, upY);
                    if (board.GetAnimalFromCell(upCell) != cfg.boneBlock)
                        waveCells.Add(upCell);
                }
            }

            if (waveCells.Count == 0)
                continue;

            if (AudioManager.instance != null)
                AudioManager.instance.PlaySFXPitchAdjusted(8, 0.2f);

            await view.AnimateMatchPopFx(waveCells, 0.09f);
        }
    }
}
