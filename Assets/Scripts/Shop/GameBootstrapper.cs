using UnityEngine;
using UnityEngine.InputSystem;

public class GameBootstrapper : MonoBehaviour
{
    public static GameBootstrapper Instance { get; private set; }

    public EconomyContext Economy { get; private set; }
    public ShopService Shop { get; private set; }

    public LeaderboardManager Leaderboard { get; private set; }
    public BoardConfig SelectedLevel { get; set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Economy = new EconomyContext();
        Economy.InitializeLivesIfNeeded();
        Shop = new ShopService(Economy);
        Leaderboard = new LeaderboardManager();
    }
}
