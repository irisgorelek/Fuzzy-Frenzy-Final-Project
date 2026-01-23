using UnityEngine;
using UnityEngine.InputSystem;

public class ShopDebugPurchase : MonoBehaviour
{
    [SerializeField] private GameBootstrapper bootstrapper;
    [SerializeField] private ShopItemDefinition freeSwitchItem;
    [SerializeField] private ShopItemDefinition powerNapItem;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            bool ok = bootstrapper.Shop.TryBuy(freeSwitchItem, out var reason);

            var s = bootstrapper.Economy.State;
            Debug.Log(ok
                ? $"BOUGHT! Coins={s.coins}, FreeSwitch={s.GetBoosterCount(BoosterEffectType.FreeSwitch)}"
                : $"BUY FAILED: {reason} (Coins={s.coins})");
        }

        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
        {
            bool ok = bootstrapper.Shop.TryBuy(powerNapItem, out var reason);

            var s = bootstrapper.Economy.State;
            Debug.Log(ok
                ? $"BOUGHT POWER NAP! Coins={s.coins}, ExtraMovesConsumables={s.powerNapCount}"
                : $"BUY POWER NAP FAILED: {reason} (Coins={s.coins})");
        }

        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            bootstrapper.Shop.IsBeforeLevel = !bootstrapper.Shop.IsBeforeLevel;
            Debug.Log($"IsBeforeLevel = {bootstrapper.Shop.IsBeforeLevel}");
        }
    }
}
