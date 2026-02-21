using UnityEngine;

public class LevelVFXToggle : MonoBehaviour
{
    [SerializeField] private BoardController boardController;
    [SerializeField] private GameObject rainRoot;

    private void Start()
    {
        if (boardController == null || rainRoot == null)
        {
            Debug.LogError("LevelVFXToggle: Missing references.");
            return;
        }

        var cfg = boardController.Config; // we’ll add this property
        rainRoot.SetActive(cfg.EnableRain);
    }
}
