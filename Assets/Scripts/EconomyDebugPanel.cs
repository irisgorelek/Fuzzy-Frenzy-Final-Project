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

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            bootstrapper.Economy.AddBooster(BoosterEffectType.FreeSwitch, 1);
            Debug.Log($"FreeSwitch now: {bootstrapper.Economy.State.GetBoosterCount(BoosterEffectType.FreeSwitch)}");
        }

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            bootstrapper.Economy.Wipe();
            Debug.Log("Wiped + reloaded economy.");
        }

        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
        {
            Debug.Log($"ExtraMoves now: {bootstrapper.Economy.State.powerNapCount}");
        }

        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
        {
            bootstrapper.Economy.TrySpendLifeForLevelStart();
            Debug.Log($"Last life timestamp: {bootstrapper.Economy.State.lastLifeTimestampUtcSeconds}");
        }

        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            bootstrapper.Economy.ApplyLifeRegen();
            Debug.Log($"Currnt lives now: {bootstrapper.Economy.State.currentLives}");
        }
    }
}
