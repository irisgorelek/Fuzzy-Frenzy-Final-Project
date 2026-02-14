using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelsData", menuName = "Scriptable Objects/LevelsData")]
public class LevelsData : ScriptableObject
{
    [SerializeField] private List<BoardConfig> _levelsList;

    public List<BoardConfig> Levels => _levelsList;
}
