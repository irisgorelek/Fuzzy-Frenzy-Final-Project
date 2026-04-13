using TMPro;
using UnityEngine;

public class ShopItemPriceUI : MonoBehaviour
{
    [SerializeField] private ShopItemDefinition itemDefinition;
    [SerializeField] private TextMeshProUGUI priceText;

    private void Awake()
    {
        Refresh();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (priceText != null && itemDefinition != null)
            priceText.text = itemDefinition.Price.ToString();
    }
#endif

    public void Refresh()
    {
        if (itemDefinition == null)
        {
            Debug.LogError($"ShopItemPriceUI on '{name}' is missing itemDefinition.");
            return;
        }

        if (priceText == null)
        {
            Debug.LogError($"ShopItemPriceUI on '{name}' is missing priceText.");
            return;
        }

        priceText.text = itemDefinition.Price.ToString();
    }
}