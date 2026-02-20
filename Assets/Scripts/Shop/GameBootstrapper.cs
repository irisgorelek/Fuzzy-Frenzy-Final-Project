using UnityEngine;
using UnityEngine.InputSystem;

public class GameBootstrapper : MonoBehaviour
{
    public EconomyContext Economy { get; private set; }
    public ShopService Shop { get; private set; }

    public LeaderboardManager Leaderboard { get; private set; }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);      // for next scenes
        //Debug.Log("Bootstrapper Awake - instance: " + GetInstanceID()); // DEBUG: check that instace is not duplicated

        Economy = new EconomyContext();
        Economy.InitializeLivesIfNeeded();
        Shop = new ShopService(Economy);
        Leaderboard = new LeaderboardManager();
    }
}
