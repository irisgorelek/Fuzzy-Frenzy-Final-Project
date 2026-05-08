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
    [SerializeField] private float _triggeredBubbleVisibleSeconds = 3f;

    public event System.Action<DialogueMode> DialogueStarted;
    public event System.Action<DialogueMode> DialogueFinished;

    private bool _normalBubbleActive;
    private bool _tutorialActive;
    private bool _triggeredBubbleActive;
    private readonly HashSet<int> _triggeredEntryIndicesShown = new();

    public async Task HandleLevelStartBubblesAsync(
    int levelIndex,
    IBoardDialogueContext context,
    int boardWidth)
    {
        if (_speechConfig == null)
            return;

        if (_speechConfig.TryGetTutorialLevel(levelIndex, out var tutorialLevel))
        {
            await ShowTutorialLevelAsync(tutorialLevel);
            return;
        }

        await ShowRandomNormalBubbleAsync(
            context.GetAllCells(),
            cell => context.GetAnimalAtCell(cell),
            cell => context.GetWorldPosition(cell),
            (cell, duration) => context.AnimateHighlight(cell, duration),
            boardWidth
        );
    }

    public async Task ShowRandomNormalBubbleAsync(
        IReadOnlyList<Vector2Int> speakerCells,
        Func<Vector2Int, Animal> getAnimalAtCell,
        Func<Vector2Int, Vector3> getWorldPosition,
        Func<Vector2Int, float, Task> animateHighlight,
        int boardWidth)
    {
        //Debug.Log("NEW ShowRandomNormalBubbleAsync CALLED - returning immediately");
        //return;

        if (_normalBubbleActive || _tutorialActive || _triggeredBubbleActive || _speechBubblePresenter == null)
            return;

        if (speakerCells == null || speakerCells.Count == 0)
            return;

        if (_tutorialActive || _triggeredBubbleActive)
        {
            _normalBubbleActive = false;
            return;
        }

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

        _normalBubbleActive = true;
        DialogueStarted?.Invoke(DialogueMode.Normal);

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
            _normalBubbleActive = false;
            DialogueFinished?.Invoke(DialogueMode.Tutorial);
        }
    }

    private bool IsMiddleColumn(Vector2Int cell, int boardWidth)
    {
        return boardWidth % 2 == 1 && cell.x == boardWidth / 2;
    }

    public async Task ShowTutorialLevelAsync(TutorialLevelEntry tutorialLevel)
    {
        if (_tutorialActive || _speechBubblePresenter == null)
            return;

        if (tutorialLevel.steps == null || tutorialLevel.steps.Count == 0)
            return;

        _tutorialActive = true;
        DialogueStarted?.Invoke(DialogueMode.Normal);

        try
        {
            for (int i = 0; i < tutorialLevel.steps.Count; i++)
            {
                var step = tutorialLevel.steps[i];

                if (step.lines == null || step.lines.Count == 0)
                    continue;

                Sprite speakerSprite = step.animal != null ? step.animal._sprite : null;
                bool useRightSide = step.side == SpeechSide.Right;

                await _speechBubblePresenter.ShowTutorialAsync(
                    speakerSprite,
                    step.lines,
                    useRightSide
                );
            }
        }
        finally
        {
            _tutorialActive = false;
            DialogueFinished?.Invoke(DialogueMode.Tutorial);
        }
    }

    public async Task TryShowTriggeredBubbleAsync(
    int levelIndex,
    IBoardDialogueContext context,
    int boardWidth)
    {
        //Debug.Log("Triggered dialogue disabled for test");
        //return;

        if (_tutorialActive || _triggeredBubbleActive || _speechConfig == null || _speechBubblePresenter == null)
            return;

        var entries = _speechConfig.GetTriggeredEntriesForLevel(levelIndex);
        if (entries.Count == 0)
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            if (_triggeredEntryIndicesShown.Contains(i))
                continue;

            var e = entries[i];
            if (e.triggerAnimal == null || e.lines == null || e.lines.Count == 0)
                continue;

            var triggerCells = context.FindCellsWithAnimal(e.triggerAnimal);
            if (triggerCells.Count == 0)
                continue;

            var triggerCell = triggerCells[UnityEngine.Random.Range(0, triggerCells.Count)];

            Vector2Int speakerCell;
            Animal speakerAnimal;
            if (e.speakerAnimal != null)
            {
                var speakerCells = context.FindCellsWithAnimal(e.speakerAnimal);
                if (speakerCells.Count == 0)
                    continue;

                speakerCell = speakerCells[UnityEngine.Random.Range(0, speakerCells.Count)];
                speakerAnimal = e.speakerAnimal;
            }
            else
            {
                var candidates = new List<Vector2Int>();

                foreach (var cell in context.GetAllCells())
                {
                    if (context.GetAnimalAtCell(cell) != null)
                        candidates.Add(cell);
                }

                if (candidates.Count == 0)
                    continue;

                speakerCell = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                speakerAnimal = context.GetAnimalAtCell(speakerCell);
                if (speakerAnimal == null)
                    continue;
            }

            int randomLineIndex = UnityEngine.Random.Range(0, e.lines.Count);
            var lines = new List<string> { e.lines[randomLineIndex] };

            if (triggerCell == speakerCell)
                await context.AnimateHighlight(triggerCell, _speechCellHighlightDuration);
            else
                await Task.WhenAll(
                    context.AnimateHighlight(triggerCell, _speechCellHighlightDuration),
                    context.AnimateHighlight(speakerCell, _speechCellHighlightDuration));

            bool useRightSide = UnityEngine.Random.Range(0, 2) == 1;

            _triggeredBubbleActive = true;
            DialogueStarted?.Invoke(DialogueMode.Normal);

            try
            {
                await context.PlayAnimalSpeakSfx(speakerAnimal); // Animal Sound
                await _speechBubblePresenter.ShowTriggeredAsync(speakerAnimal._sprite, lines, useRightSide, _triggeredBubbleVisibleSeconds);
                _triggeredEntryIndicesShown.Add(i);
            }
            finally
            {
                _triggeredBubbleActive = false;
                DialogueFinished?.Invoke(DialogueMode.Tutorial);
            }
            return;
        }
    }

    public void HideNormalBubbleIfActive()
    {
        if (!_normalBubbleActive || _speechBubblePresenter == null)
            return;

        _speechBubblePresenter.HideNormalImmediate();
        _normalBubbleActive = false;
    }
}