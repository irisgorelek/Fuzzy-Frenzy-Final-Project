using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface IBoardDialogueContext
{
    IReadOnlyList<Vector2Int> GetAllCells();
    Animal GetAnimalAtCell(Vector2Int cell);
    IReadOnlyList<Vector2Int> FindCellsWithAnimal(Animal animal);

    Vector3 GetWorldPosition(Vector2Int cell);

    Task AnimateHighlight(Vector2Int cell, float duration);

    Task PlayAnimalSpeakSfx(Animal animal);
}