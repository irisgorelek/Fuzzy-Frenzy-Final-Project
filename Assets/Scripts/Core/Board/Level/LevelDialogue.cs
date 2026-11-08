using System.Threading.Tasks;
using UnityEngine;

/// <summary>The animals' speech bubbles for a level: the opening dialogue and bubbles triggered after a turn.</summary>
public sealed class LevelDialogue
{
    private readonly AnimalDialogueController _dialogue;
    private readonly BoardConfig _config;
    private readonly float _speakSfxSeconds;
    private readonly IGameAudio _audio;

    public LevelDialogue(AnimalDialogueController dialogue, BoardConfig config, float speakSfxSeconds, IGameAudio audio)
    {
        _audio = audio;
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
        // 0 means the animal has no voice.
        if (animal == null || animal._speakSfxId == 0) return;

        _audio.PlaySfx((SfxId)animal._speakSfxId);

        if (_speakSfxSeconds > 0f)
            await Task.Delay(Mathf.CeilToInt(_speakSfxSeconds * 1000f));
    }
}
