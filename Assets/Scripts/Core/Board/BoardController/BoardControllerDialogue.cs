using System.Threading.Tasks;
using UnityEngine;

public sealed class BoardControllerDialogueFlow
{
    private readonly BoardController _controller;

    public BoardControllerDialogueFlow(BoardController controller)
    {
        _controller = controller;
    }

    public async Task TryStartLevelDialogueAsync()
    {
        if (_controller.IsLevelOver)
            return;

        var context = new BoardDialogueContext(_controller.Board, _controller.View, PlayAnimalSpeakSfx);

        await _controller.AnimalDialogueController.HandleLevelStartBubblesAsync(
            _controller.Cfg.levelIndex,
            context,
            _controller.Cfg.weidth
        );
    }

    public async Task TryShowTriggeredDialogueAsync()
    {
        var context = new BoardDialogueContext(_controller.Board, _controller.View, PlayAnimalSpeakSfx);

        await _controller.AnimalDialogueController.TryShowTriggeredBubbleAsync(
            _controller.Cfg.levelIndex,
            context,
            _controller.Cfg.weidth
        );
    }

    private async Task PlayAnimalSpeakSfx(Animal animal)
    {
        if (animal == null || animal._speakSfxId == 0) return;
        if (AudioManager.instance == null) return;

        AudioManager.instance.PlaySFX(animal._speakSfxId);

        if (_controller.SpeakSfxDuration > 0f)
            await Task.Delay(Mathf.CeilToInt(_controller.SpeakSfxDuration * 1000f));
    }
}
