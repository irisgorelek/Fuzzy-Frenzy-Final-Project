using System.Threading.Tasks;
using UnityEngine;

/// <summary>The animals' speech bubbles for a level: the opening dialogue and bubbles triggered after a turn.</summary>
public sealed class LevelDialogue
{
    private readonly AnimalDialogueController _dialogue;
    private readonly BoardConfig _config;
    private readonly float _speakSfxSeconds;

    public LevelDialogue(AnimalDialogueController dialogue, BoardConfig config, float speakSfxSeconds)
    {
        _dialogue = dialogue;
        _config = config;
        _speakSfxSeconds = speakSfxSeconds;
    }

    /// <summary>True while a tutorial or a triggered bubble is showing.</summary>
    public bool IsBlockingInput => _dialogue != null && _dialogue.IsBlockingInput;

    public Task PlayLevelStartAsync(Board board, BoardView view)
    {
        if (_dialogue == null) return Task.CompletedTask;
        return _dialogue.HandleLevelStartBubblesAsync(_config.levelIndex, Context(board, view), _config.weidth);
    }

    public Task PlayTriggeredAsync(Board board, BoardView view)
    {
        if (_dialogue == null) return Task.CompletedTask;
        return _dialogue.TryShowTriggeredBubbleAsync(_config.levelIndex, Context(board, view), _config.weidth);
    }

    public void HideNormalBubble()
    {
        if (_dialogue != null)
            _dialogue.HideNormalBubbleIfActive();
    }

    private BoardDialogueContext Context(Board board, BoardView view) => new BoardDialogueContext(board, view, PlaySpeakSfxAsync);

    private async Task PlaySpeakSfxAsync(Animal animal)
    {
        if (animal == null || animal._speakSfxId == 0) return;
        if (AudioManager.instance == null) return;

        AudioManager.instance.PlaySFX(animal._speakSfxId);

        if (_speakSfxSeconds > 0f)
            await Task.Delay(Mathf.CeilToInt(_speakSfxSeconds * 1000f));
    }
}
