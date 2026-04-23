using UnityEngine;
using UnityEngine.InputSystem;

public class ShopDebugPurchase : MonoBehaviour
{
    [SerializeField] private ShopItemDefinition freeSwitchItem;
    [SerializeField] private ShopItemDefinition powerNapItem;
    [SerializeField] private ShopItemDefinition lifeItem;

    private void Update()
    {
        //if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        //{
        //    bool ok = GameBootstrapper.Instance.Shop.TryBuy(freeSwitchItem, out var reason);

        //    var s = GameBootstrapper.Instance.Economy.State;
        //    Debug.Log(ok
        //        ? $"BOUGHT! Coins={s.coins}, FreeSwitch={s.GetBoosterCount(BoosterEffectType.FreeSwitch)}"
        //        : $"BUY FAILED: {reason} (Coins={s.coins})");
        //}

        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
        {
            bool ok = GameBootstrapper.Instance.Shop.TryBuy(powerNapItem, out var reason);

            var s = GameBootstrapper.Instance.Economy.State;
            Debug.Log(ok
                ? $"BOUGHT POWER NAP! Coins={s.coins}, ExtraMovesConsumables={s.extraMoveCount}"
                : $"BUY POWER NAP FAILED: {reason} (Coins={s.coins})");
        }

        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            GameBootstrapper.Instance.Shop.IsBeforeLevel = !GameBootstrapper.Instance.Shop.IsBeforeLevel;
            Debug.Log($"IsBeforeLevel = {GameBootstrapper.Instance.Shop.IsBeforeLevel}");
        }

        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            bool ok = GameBootstrapper.Instance.Shop.TryBuy(lifeItem, out var reason);
            var s = GameBootstrapper.Instance.Economy.State;
            Debug.Log(ok
                ? $"BOUGHT LIFE! Lives={s.currentLives}/{s.maxLives}, Coins={s.coins}"
                : $"BUY LIFE FAILED: {reason} (Lives={s.currentLives}/{s.maxLives}, Coins={s.coins})");
        }
    }
}
