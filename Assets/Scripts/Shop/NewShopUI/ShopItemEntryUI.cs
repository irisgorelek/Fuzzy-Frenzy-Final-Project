using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemEntryUI : MonoBehaviour
{
    [SerializeField] private ShopItemDefinition itemDefinition;

    [Header("UI")]
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI ownedText;

    private GameBootstrapper bootstrapper;

    private void Awake()
    {
        bootstrapper = GameBootstrapper.Instance;
    }

    private void OnEnable()
    {
        if (bootstrapper != null)
            bootstrapper.Economy.OnChanged += RefreshUI;

        if (buyButton != null)
            buyButton.onClick.AddListener(BuyOnce);

        RefreshUI();
    }

    private void OnDisable()
    {
        if (bootstrapper != null)
            bootstrapper.Economy.OnChanged -= RefreshUI;

        if (buyButton != null)
            buyButton.onClick.RemoveListener(BuyOnce);
    }

    private void BuyOnce()
    {
        if (bootstrapper == null || itemDefinition == null)
            return;

        bool success = bootstrapper.Shop.TryBuy(itemDefinition, out var reason);

        if (!success)
            Debug.Log($"Could not buy {itemDefinition.DisplayName}: {reason}");

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (bootstrapper == null || itemDefinition == null)
            return;

        if (priceText != null)
            priceText.text = itemDefinition.Price.ToString();

        if (ownedText != null)
            ownedText.text = GetOwnedCount().ToString();

        if (buyButton != null)
            buyButton.interactable = bootstrapper.Shop.CanBuy(itemDefinition, out _);
    }

    private int GetOwnedCount()
    {
        var state = bootstrapper.Economy.State;

        switch (itemDefinition.ItemType)
        {
            case ShopItemType.Booster:
                return state.GetBoosterCount(itemDefinition.BoosterEffect);

            case ShopItemType.ExtraMoves:
                return state.extraMoveCount;

            case ShopItemType.Lives:
                return state.currentLives;

            case ShopItemType.AvatarItem:
                if (itemDefinition.AvatarItem != null &&
                    state.unlockedAvatarItems.Contains(itemDefinition.AvatarItem.ItemId))
                    return 1;
                return 0;

            default:
                return 0;
        }
    }
}