using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BoardDialogueContext : IBoardDialogueContext
{
    private readonly Board _board;
    private readonly BoardView _view;
    private readonly System.Func<Animal, Task> _playAnimalSpeakSfx;

    public BoardDialogueContext(
        Board board,
        BoardView view,
        System.Func<Animal, Task> playAnimalSpeakSfx)
    {
        _board = board;
        _view = view;
        _playAnimalSpeakSfx = playAnimalSpeakSfx;
    }

    public IReadOnlyList<Vector2Int> GetAllCells()
        => _board.GetAllCells();

    public Animal GetAnimalAtCell(Vector2Int cell)
        => _board.GetAnimalFromCell(cell);

    public IReadOnlyList<Vector2Int> FindCellsWithAnimal(Animal animal)
        => _board.FindCellsWithAnimal(animal);

    public Vector3 GetWorldPosition(Vector2Int cell)
        => _view.GetCellWorldPosition(cell);

    public Task AnimateHighlight(Vector2Int cell, float duration)
        => _view.AnimateBlockedTap(cell, duration);

    public Task PlayAnimalSpeakSfx(Animal animal)
        => _playAnimalSpeakSfx?.Invoke(animal) ?? Task.CompletedTask;
}