using UnityEngine;

public class BootstrapperLocator : MonoBehaviour
{
    public GameBootstrapper Bootstrapper { get; private set; }

    private void Awake()
    {
        Bootstrapper = GameBootstrapper.Instance;
        if (Bootstrapper == null)
            Debug.LogError("No GameBootstrapper found. Make sure it exists and is DontDestroyOnLoad.");
    }
}
