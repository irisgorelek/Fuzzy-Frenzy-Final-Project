using UnityEngine;
using UnityEngine.UI;

public class SFXToggleBinder : MonoBehaviour
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
                _toggle.SetIsOnWithoutNotify(!AudioManager.instance.SfxOn);
            else
                _toggle.SetIsOnWithoutNotify(AudioManager.instance.SfxOn);
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
            AudioManager.instance.SetSfxMuted(value);
        else
            AudioManager.instance.SetSfxOn(value);
    }
}