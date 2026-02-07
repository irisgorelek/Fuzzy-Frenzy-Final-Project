using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public enum PointsOrMatches { points, matches }

[CreateAssetMenu(menuName = "BoardConfig")]
public class BoardConfig : ScriptableObject
{
    public int level;
    public int weidth; 
    public int height;
    public PointsOrMatches goalType = PointsOrMatches.points;
    public int goal;
    public List<Animal> animals;
    public int maxMoves;
}
