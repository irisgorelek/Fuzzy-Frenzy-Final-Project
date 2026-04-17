using UnityEngine;
using UnityEngine.UI;

public class MusicToggleBinder : MonoBehaviour
{
    [SerializeField] private Toggle _toggle;
    [SerializeField] private bool _toggleMeansMuted = true;

    private void Awake()
    {
        if (_toggle == null)
            _toggle = GetComponent<Toggle>();
    }

    private void OnEnable()
    {
        if (_toggle == null)
            return;

        _toggle.onValueChanged.RemoveListener(OnToggleChanged);
        _toggle.onValueChanged.AddListener(OnToggleChanged);

        if (AudioManager.instance != null)
        {
            if (_toggleMeansMuted)
                _toggle.SetIsOnWithoutNotify(!AudioManager.instance.MusicOn);
            else
                _toggle.SetIsOnWithoutNotify(AudioManager.instance.MusicOn);
        }
    }

    private void OnDisable()
    {
        if (_toggle != null)
            _toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool value)
    {
        if (AudioManager.instance == null)
            return;

        if (_toggleMeansMuted)
            AudioManager.instance.SetMusicMuted(value);
        else
            AudioManager.instance.SetMusicOn(value);
    }
}