using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CellView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private Image _image;

    public Sprite CurrentSprite => _image.sprite;
    public Color CurrentColor => _image.color;

    public Vector2Int Coord { get; private set; }

    private bool _highlighted;

    public event Action<Vector2Int, Vector2> PointerDown;
    public event Action<Vector2Int, Vector2> Drag;
    public event Action<Vector2Int, Vector2> PointerUp;

    public void Init(Vector2Int coord)
    {
        Coord = coord;
        name = $"Cell ({coord.x},{coord.y})";
    }
    public void SetSprite(Sprite sprite, Color color)
    {
        _image.sprite = sprite;
        _image.color = color;
    }

    public void SetHighlighted(bool on)
    {
        // TODO: Highlight
    }

    public void OnPointerDown(PointerEventData eventData) => PointerDown?.Invoke(Coord, eventData.position);
    public void OnDrag(PointerEventData eventData) => Drag?.Invoke(Coord, eventData.position);
    public void OnPointerUp(PointerEventData eventData) => PointerUp?.Invoke(Coord, eventData.position);
}
