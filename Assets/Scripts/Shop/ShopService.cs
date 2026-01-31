public class ShopService
{
    private readonly EconomyContext economy;

    public bool IsBeforeLevel { get; set; } = true;

    public ShopService(EconomyContext economy)
    {
        this.economy = economy;
    }

    public bool CanBuy(ShopItemDefinition item, out PurchaseFailReason reason)
    {
        reason = PurchaseFailReason.None;

        if (item == null) 
        { reason = PurchaseFailReason.ItemNotConfigured; return false; }
        if (!IsBeforeLevel) 
        { reason = PurchaseFailReason.NotAllowedRightNow; return false; }
        if (item.Currency != CurrencyType.Coins) 
        { reason = PurchaseFailReason.ItemNotConfigured; return false; }

        if (economy.State.coins < item.Price)
        {
            reason = PurchaseFailReason.NotEnoughCoins;
            return false;
        }

        if (item.ItemType == ShopItemType.Lives && !economy.CanBuyLives())
        {
            reason = PurchaseFailReason.NotAllowedRightNow;
            return false;
        }


        return true;
    }

    public bool TryBuy(ShopItemDefinition item, out PurchaseFailReason reason)
    {
        if (!CanBuy(item, out reason))
            return false;

        if (!economy.SpendCoins(item.Price))
        {
            reason = PurchaseFailReason.NotEnoughCoins;
            return false;
        }

        switch (item.ItemType)
        {
            case ShopItemType.Booster:
                economy.AddBooster(item.BoosterEffect, item.BoosterAmountGranted);
                break;

            case ShopItemType.ExtraMoves:
                // Buying gives you +1 Power Nap item (a consumable)
                economy.AddPowerNap(1);
                break;

            case ShopItemType.Lives:
                economy.AddLives(item.LivesAmountGranted);
                break;
        }

        reason = PurchaseFailReason.None;
        return true;
    }
}
