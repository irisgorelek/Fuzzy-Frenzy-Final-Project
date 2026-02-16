using UnityEngine;
using UnityEngine.InputSystem;

public class EconomyDebugPanel : MonoBehaviour
{
    [SerializeField] private GameBootstrapper bootstrapper;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            bootstrapper.Economy.AddCoins(100);
            Debug.Log($"Coins now: {bootstrapper.Economy.State.coins}");
        }

        //if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        //{
        //    bootstrapper.Economy.AddBooster(BoosterEffectType.FreeSwitch, 1);
        //    Debug.Log($"FreeSwitch now: {bootstrapper.Economy.State.GetBoosterCount(BoosterEffectType.FreeSwitch)}");
        //}

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            bootstrapper.Economy.Wipe();
            Debug.Log("Wiped + reloaded economy.");
        }

        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
        {
            Debug.Log($"ExtraMoves now: {bootstrapper.Economy.State.extraMoveCount}");
        }

        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
        {
            bool ok = bootstrapper.Economy.TrySpendLifeForLevelStart();
            Debug.Log(ok
                ? $"START LEVEL: Lives={bootstrapper.Economy.State.currentLives}/{bootstrapper.Economy.State.maxLives}"
                : $"NO LIVES: Lives={bootstrapper.Economy.State.currentLives}/{bootstrapper.Economy.State.maxLives}");
        }

        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            bootstrapper.Economy.ApplyLifeRegen();
            Debug.Log($"REGEN CHECK: Lives={bootstrapper.Economy.State.currentLives}/{bootstrapper.Economy.State.maxLives}");
        }

    }
}
