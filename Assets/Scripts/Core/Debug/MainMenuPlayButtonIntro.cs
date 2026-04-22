using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Wire the main menu Play button here instead of opening level select directly.
/// Runs camera zoom + optional one-shot VFX, then shows the level select panel (UIPopupTween).
/// </summary>
public class MainMenuPlayButtonIntro : MonoBehaviour
{
    [SerializeField] private Camera _menuCamera;
    [SerializeField] private float _zoomDuration = 1.1f;
    [SerializeField] private float _orthoSizeFrom;
    [SerializeField] private float _orthoSizeTo;
    [SerializeField] private bool _useOrthoZoom = true;
    [SerializeField] private Transform _cameraMoveTarget;
    [SerializeField] private GameObject _introVfxPrefab;
    [SerializeField] private Transform _vfxSpawnParent;
    [SerializeField] private Vector3 _vfxLocalOffset = Vector3.zero;
    [SerializeField] private UIPopupTween _levelSelectPanelTween;

    private bool _running;

    public async void RunPlayIntro()
    {
        if (_running)
            return;

        _running = true;

        try
        {
            await RunIntroAsync();
        }
        finally
        {
            _running = false;
        }
    }

    private async Task RunIntroAsync()
    {
        if (_menuCamera == null)
            _menuCamera = Camera.main;

        var seq = DOTween.Sequence();

        if (_menuCamera != null && _useOrthoZoom && _menuCamera.orthographic)
        {
            if (_orthoSizeFrom <= 0f)
                _orthoSizeFrom = _menuCamera.orthographicSize;

            _menuCamera.orthographicSize = _orthoSizeFrom;
            seq.Join(_menuCamera.DOOrthoSize(_orthoSizeTo, _zoomDuration).SetEase(Ease.InOutQuad));
        }
        else if (_menuCamera != null && _cameraMoveTarget != null)
        {
            seq.Join(_menuCamera.transform.DOMove(_cameraMoveTarget.position, _zoomDuration).SetEase(Ease.InOutQuad));
        }
        else
        {
            seq.AppendInterval(Mathf.Max(0.05f, _zoomDuration * 0.35f));
        }

        if (_introVfxPrefab != null)
        {
            Transform parent = _vfxSpawnParent != null ? _vfxSpawnParent : transform;
            var fx = Instantiate(_introVfxPrefab, parent);
            fx.transform.localPosition += _vfxLocalOffset;

            foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>())
                ps.Play();

            float life = 2f;
            var psRoot = fx.GetComponentInChildren<ParticleSystem>();
            if (psRoot != null)
                life = psRoot.main.duration + psRoot.main.startLifetime.constantMax;

            Destroy(fx, Mathf.Max(0.5f, life));
        }

        var tcs = new TaskCompletionSource<bool>();
        seq.OnComplete(() => tcs.TrySetResult(true));
        seq.OnKill(() => tcs.TrySetResult(true));
        seq.Play();

        await tcs.Task;

        if (_levelSelectPanelTween != null)
            _levelSelectPanelTween.Show();
    }
}
