using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemRowUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI ownedText;
    [SerializeField] private Button buyButton;

    private GameBootstrapper bootstrapper;
    private ShopItemDefinition item;

    public void Bind(GameBootstrapper bootstrapper, ShopItemDefinition item)
    {
        this.bootstrapper = bootstrapper;
        this.item = item;

        icon.sprite = item.Icon;
        nameText.text = item.DisplayName;
        descText.text = item.Description;
        priceText.text = item.Price.ToString();

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClicked);

        bootstrapper.Economy.OnChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (bootstrapper != null)
            bootstrapper.Economy.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        var s = bootstrapper.Economy.State;

        int owned = 0;
        switch (item.ItemType)
        {
            case ShopItemType.Booster:
                owned = s.GetBoosterCount(item.BoosterEffect);
                break;
            case ShopItemType.ExtraMoves:
                owned = s.extraMoveCount;
                break;
            case ShopItemType.Lives:
                owned = s.currentLives; // show current lives / energy
                break;
        }

        ownedText.text = $"Owned: {owned}";

        bool canBuy = bootstrapper.Shop.CanBuy(item, out _);
        buyButton.interactable = canBuy;
    }

    private void OnBuyClicked()
    {
        bool ok = bootstrapper.Shop.TryBuy(item, out var reason);
        if (!ok)
            Debug.Log($"Buy failed: {reason}");
        // Economy.OnChanged will refresh UI automatically
    }
}
