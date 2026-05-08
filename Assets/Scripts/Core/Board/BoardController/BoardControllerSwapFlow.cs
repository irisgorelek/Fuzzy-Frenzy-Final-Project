using System.Threading.Tasks;
using UnityEngine;

public sealed class BoardControllerSwapFlow
{
    private readonly BoardController _controller;

    public BoardControllerSwapFlow(BoardController controller)
    {
        _controller = controller;
    }

    public async Task HandleSwapRequestedAsync(Vector2Int from, Vector2Int to)
    {
        if (_controller.IsBusy || _controller.IsLevelOver || _controller.IsSpeechBubbleInputBlocked) return;
        if (!_controller.IsTimerBombActive && _controller.MoveCounter.MovesLeft <= 0) return;

        var a = _controller.Board.GetAnimalFromCell(from);
        var b = _controller.Board.GetAnimalFromCell(to);

        if (a == null || b == null)
            return;

        if (!a._canSwap || !b._canSwap)
        {
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySFX(4); // can't swap sound

            await _controller.View.AnimateInvalidSwap(from, to, to - from);
            return;
        }

        // Do if activated timer power up
        if (_controller.IsTimerBombActive)
        {
            bool swapped = _controller.Board.SwapCellsRaw(from, to);
            if (!swapped) return;

            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFXPitchAdjusted(12, 0.2f); // Play swap sound.
            }
            await _controller.View.AnimateSwap(from, to, 0.18f);
            return;
        }

        // Do if normal gameplay
        _controller.IsBusy = true;

        bool sheepSwiped = _controller.IsAnySheep(a); // started swipe on a sheep
        bool otherIsSheep = _controller.IsAnySheep(b); // or you swiped into a sheep
        bool sheepInvolved = sheepSwiped || otherIsSheep;

        bool swipeVertical = (from.x == to.x);

        // where the sheep ends up after the swap
        Vector2Int sheepPosAfterSwap = sheepSwiped ? to : otherIsSheep ? from : from;

        if (!_controller.Board.SwapCellsRaw(from, to))
        {
            _controller.IsBusy = false;

            if (AudioManager.instance != null)
                AudioManager.instance.PlaySFX(4);

            await _controller.View.AnimateInvalidSwap(from, to, to - from);
            return;
        }

        // Show the swap immediately even if invalid
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFXPitchAdjusted(12, 0.2f); // Play swap sound.
        }
        await _controller.View.AnimateSwap(from, to, 0.18f);

        // Let the swap show in unity
        // await WaitFrames(framesBetweenSteps);

        if (sheepInvolved) // Black sheep
        {
            _controller.MoveCounter.UseMove();

            _controller.TryRollBlackSheep();

            // Animate the blast first, using the same pop FX as normal matches
            await _controller.AnimateBlackSheepBlastFromCenter(sheepPosAfterSwap, swipeVertical);

            // Then actually clear the model and show the result
            _controller.Board.TriggerSheepSwipeBlast(sheepPosAfterSwap, swipeVertical);

            _controller.UpdateGoalUI();
            _controller.View.AssignSprites(_controller.Board);

            await _controller.ResolveCascadesAsync();
            _controller.HideNormalBubbleIfActive();

            if (_controller.IsLevelOver)
            {
                _controller.IsBusy = false;
                return;
            }

            if (_controller.TryHandleLevelFailed())
            {
                _controller.IsBusy = false;
                return;
            }

            await _controller.TryShowTriggeredDialogueAsync();

            _controller.IsBusy = false;
            return;
        }

        // Check if the swap didn't produce any match
        if (!_controller.Board.HasAnyMatch())
        {
            // The swap was invalid. Swap back in model and animate back
            _controller.Board.SwapCellsRaw(from, to);
            await _controller.View.AnimateSwap(from, to, 0.18f); // animate back

            _controller.IsBusy = false;
            return;
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFXPitchAdjusted(8, 0.2f); // Play pop sound.
        }
        _controller.MoveCounter.UseMove();
        _controller.TryRollBlackSheep(); // Roll the black sheep spawn

        // The swap was valid. Resolve cascades with pacing
        _controller.View.AssignSprites(_controller.Board); // Sync view to model after swap

        await _controller.ResolveCascadesAsync();
        _controller.HideNormalBubbleIfActive();

        if (_controller.IsLevelOver)
        {
            _controller.IsBusy = false;
            return;
        }

        if (_controller.TryHandleLevelFailed())
        {
            _controller.IsBusy = false;
            return;
        }

        await _controller.TryShowTriggeredDialogueAsync();

        _controller.IsBusy = false;
    }
}
