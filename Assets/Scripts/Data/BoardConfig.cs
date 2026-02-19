using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public enum PointsOrMatches { points, matches }

[CreateAssetMenu(menuName = "BoardConfig")]
public class BoardConfig : ScriptableObject
{
    [SerializeField] private bool enableRain;
    public bool EnableRain => enableRain;
    public int levelIndex;
    public int weidth; 
    public int height;
    public PointsOrMatches goalType = PointsOrMatches.points;
    public int goal;
    public List<Animal> animals;
    public int maxMoves;
}
