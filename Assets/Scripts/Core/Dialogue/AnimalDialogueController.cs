using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AnimalDialogueController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private AnimalSpeechConfig _speechConfig;

    [Header("Presenter")]
    [SerializeField] private SpeechBubblePresenter _speechBubblePresenter;

    [Header("Timing")]
    [SerializeField] private float _normalBubbleDelaySeconds = 2.5f;
    [SerializeField] private float _normalBubbleVisibleSeconds = 3f;
    [SerializeField] private float _speechCellHighlightDuration = 0.48f;

    private bool _bubbleActive;

    public async Task ShowRandomNormalBubbleAsync(
        IReadOnlyList<Vector2Int> speakerCells,
        Func<Vector2Int, Animal> getAnimalAtCell,
        Func<Vector2Int, Vector3> getWorldPosition,
        Func<Vector2Int, float, Task> animateHighlight,
        int boardWidth)
    {
        if (_bubbleActive || _speechConfig == null || _speechBubblePresenter == null)
            return;

        if (speakerCells == null || speakerCells.Count == 0)
            return;

        await Task.Delay(TimeSpan.FromSeconds(_normalBubbleDelaySeconds));

        var validSpeakers = new List<(Vector2Int cell, Animal animal, List<string> lines)>();

        foreach (var cell in speakerCells)
        {
            if (IsMiddleColumn(cell, boardWidth))
                continue;

            Animal animal = getAnimalAtCell(cell);
            if (animal == null)
                continue;

            if (!_speechConfig.TryGetRandomNormalLines(animal, out List<string> lines))
                continue;

            validSpeakers.Add((cell, animal, lines));
        }

        if (validSpeakers.Count == 0)
            return;

        var chosen = validSpeakers[UnityEngine.Random.Range(0, validSpeakers.Count)];
        string line = chosen.lines[UnityEngine.Random.Range(0, chosen.lines.Count)];
        bool useRightSide = chosen.cell.x >= boardWidth / 2f;

        _bubbleActive = true;

        try
        {
            if (animateHighlight != null)
                await animateHighlight(chosen.cell, _speechCellHighlightDuration);

            Vector3 worldPosition = getWorldPosition(chosen.cell);

            await _speechBubblePresenter.ShowNormalAsync(
                new List<string> { line },
                worldPosition,
                useRightSide,
                _normalBubbleVisibleSeconds
            );
        }
        finally
        {
            _bubbleActive = false;
        }
    }

    private bool IsMiddleColumn(Vector2Int cell, int boardWidth)
    {
        return boardWidth % 2 == 1 && cell.x == boardWidth / 2;
    }
}