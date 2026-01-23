using UnityEngine;

public enum CurrencyType
{
    Coins
}

public enum ShopItemType
{
    Booster,
    Lives,
    ExtraMoves
}

public enum BoosterEffectType
{
    FreeSwitch,
    SniffCandy,
    FuzzyBlast
}


[CreateAssetMenu(menuName = "Game/Shop/Shop Item", fileName = "ShopItem_")]
public class ShopItemDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string itemId; // unique string like "booster_free_switch"
    [SerializeField] private string displayName;
    [TextArea][SerializeField] private string description;
    [SerializeField] private Sprite icon;

    [Header("Purchase")]
    [SerializeField] private CurrencyType currency = CurrencyType.Coins;
    [Min(0)][SerializeField] private int price = 0;

    [Header("Type")]
    [SerializeField] private ShopItemType itemType = ShopItemType.Booster;

    [Header("Booster Data (only if itemType = Booster)")]
    [SerializeField] private BoosterEffectType boosterEffect;
    [Min(1)][SerializeField] private int boosterAmountGranted = 1;

    [Header("Lives Data (only if itemType = Lives)")]
    [Min(1)][SerializeField] private int livesAmountGranted = 1;

    [Header("Extra Moves Data (only if itemType = ExtraMoves)")]
    [Min(1)][SerializeField] private int extraMovesGranted = 3;

    // Read-only accessors (so other code can read but not modify runtime)
    public string ItemId => itemId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;

    public CurrencyType Currency => currency;
    public int Price => price;

    public ShopItemType ItemType => itemType;

    public BoosterEffectType BoosterEffect => boosterEffect;
    public int BoosterAmountGranted => boosterAmountGranted;

    public int LivesAmountGranted => livesAmountGranted;

    public int ExtraMovesGranted => extraMovesGranted;
}
