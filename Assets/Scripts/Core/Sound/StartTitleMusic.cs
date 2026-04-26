using UnityEngine;

public class StartTitleMusic : MonoBehaviour
{
    private void OnEnable()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayTitle();
        }
    }
}
