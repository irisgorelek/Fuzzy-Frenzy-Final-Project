using UnityEngine;
using UnityEngine.InputSystem;

public class EconomyDebugPanel : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            GameBootstrapper.Instance.Economy.AddCoins(100);
            Debug.Log($"Coins now: {GameBootstrapper.Instance.Economy.State.coins}");
        }

        //if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        //{
        //    GameBootstrapper.Instance.Economy.AddBooster(BoosterEffectType.FreeSwitch, 1);
        //    Debug.Log($"FreeSwitch now: {GameBootstrapper.Instance.Economy.State.GetBoosterCount(BoosterEffectType.FreeSwitch)}");
        //}

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            GameBootstrapper.Instance.Economy.Wipe();
            Debug.Log("Wiped + reloaded economy.");
        }

        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
        {
            Debug.Log($"ExtraMoves now: {GameBootstrapper.Instance.Economy.State.extraMoveCount}");
        }

        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
        {
            bool ok = GameBootstrapper.Instance.Economy.TrySpendLifeForLevelStart();
            Debug.Log(ok
                ? $"START LEVEL: Lives={GameBootstrapper.Instance.Economy.State.currentLives}/{GameBootstrapper.Instance.Economy.State.maxLives}"
                : $"NO LIVES: Lives={GameBootstrapper.Instance.Economy.State.currentLives}/{GameBootstrapper.Instance.Economy.State.maxLives}");
        }

        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            GameBootstrapper.Instance.Economy.ApplyLifeRegen();
            Debug.Log($"REGEN CHECK: Lives={GameBootstrapper.Instance.Economy.State.currentLives}/{GameBootstrapper.Instance.Economy.State.maxLives}");
        }

    }
}
