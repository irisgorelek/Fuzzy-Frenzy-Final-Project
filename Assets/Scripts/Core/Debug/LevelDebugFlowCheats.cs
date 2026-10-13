using UnityEngine;

public class LevelDebugFlowCheats : MonoBehaviour
{
    [SerializeField] private BoardController _board;
    [SerializeField] private Animal _blackSheep;
    [SerializeField] private Animal _boneBlock;

    private void Reset()
    {
        if (_board == null)
            _board = FindFirstObjectByType<BoardController>();
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
        if (GameBootstrapper.Instance == null) return;
        GameBootstrapper.Instance.Economy.AddBooster(BoosterEffectType.FuzzyBlast, 1);
        Debug.Log("Added 1 Bomb booster.");
    }

    public void AddBlackSheepToCenter() => PlaceAnimalInCenter(_blackSheep);

    public void AddBoneToCenter() => PlaceAnimalInCenter(_boneBlock);

    private void PlaceAnimalInCenter(Animal animal)
    {
        if (_board == null)
        {
            Debug.LogWarning("LevelDebugFlowCheats: BoardController is not assigned.", this);
            return;
        }

        _board.DebugPlaceAnimalAtCenter(animal, _blackSheep, _boneBlock);
    }

    public void AddOneTimerBooster()
    {
        if (GameBootstrapper.Instance == null) return;
        GameBootstrapper.Instance.Economy.AddBooster(BoosterEffectType.TimerBomb, 1);
        Debug.Log("Added 1 Timer booster.");
    }

    public void AddOneExtraMove()
    {
        if (GameBootstrapper.Instance == null) return;
        GameBootstrapper.Instance.Economy.AddExtraMove(1);
        Debug.Log("Added 1 Extra Move.");
    }
}
