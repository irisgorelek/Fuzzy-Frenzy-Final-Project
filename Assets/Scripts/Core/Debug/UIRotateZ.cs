using UnityEngine;

/// <summary>
/// Constant Z rotation for UI images (e.g. sun icon on main menu).
/// </summary>
public class UIRotateZ : MonoBehaviour
{
    [SerializeField] private RectTransform _target;
    [SerializeField] private float _degreesPerSecond = 12f;

    private void Reset()
    {
        _target = transform as RectTransform;
    }

    private void Update()
    {
        if (_target == null)
            return;

        _target.Rotate(0f, 0f, _degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
