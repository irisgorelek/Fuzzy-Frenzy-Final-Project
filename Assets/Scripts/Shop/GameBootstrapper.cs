using UnityEngine;

public class GameBootstrapper : MonoBehaviour
{
    public EconomyContext Economy { get; private set; }
    public ShopService Shop { get; private set; }

    private void Awake()
    {
        Economy = new EconomyContext();
        Economy.InitializeLivesIfNeeded();
        Shop = new ShopService(Economy);
    }
}
