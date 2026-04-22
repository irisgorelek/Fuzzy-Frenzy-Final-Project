using UnityEngine;

public class LevelDebugFlowCheats : MonoBehaviour
{
    [SerializeField] private BoardController _board;
    [SerializeField] private GameBootstrapper _bootstrapper;

    private void Reset()
    {
        if (_board == null)
            _board = FindFirstObjectByType<BoardController>();

        if (_bootstrapper == null)
            _bootstrapper = FindFirstObjectByType<GameBootstrapper>();
    }

    public void ForceWin()
    {
        if (_board == null)
        {
            Debug.LogWarning("LevelDebugFlowCheats: BoardController is not assigned.");
            return;
        }

        _board.DebugForceWin();
    }

    public void ForceLose()
    {
        if (_board == null)
        {
            Debug.LogWarning("LevelDebugFlowCheats: BoardController is not assigned.");
            return;
        }

        _board.DebugForceLose();
    }

    public void AddOneBombBooster()
    {
        if (!TryGetBootstrapper())
            return;

        _bootstrapper.Economy.AddBooster(BoosterEffectType.FuzzyBlast, 1);
        Debug.Log("Added 1 Bomb booster.");
    }

    public void AddOneTimerBooster()
    {
        if (!TryGetBootstrapper())
            return;

        _bootstrapper.Economy.AddBooster(BoosterEffectType.TimerBomb, 1);
        Debug.Log("Added 1 Timer booster.");
    }

    public void AddOneExtraMove()
    {
        if (!TryGetBootstrapper())
            return;

        _bootstrapper.Economy.AddExtraMove(1);
        Debug.Log("Added 1 Extra Move.");
    }

    private bool TryGetBootstrapper()
    {
        if (_bootstrapper == null)
            _bootstrapper = FindFirstObjectByType<GameBootstrapper>();

        if (_bootstrapper == null)
        {
            Debug.LogWarning("LevelDebugFlowCheats: GameBootstrapper is not assigned.");
            return false;
        }

        return true;
    }
}