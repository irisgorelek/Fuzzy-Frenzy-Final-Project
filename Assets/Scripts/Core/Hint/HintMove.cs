using UnityEngine;
public struct HintMove
{
    public Vector2Int From;
    public Vector2Int To;

    public HintMove(Vector2Int from, Vector2Int to)
    {
        From = from;
        To = to;
    }
}
