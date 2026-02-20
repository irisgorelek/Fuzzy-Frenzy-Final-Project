using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public enum PointsOrMatches { points, matches }

[CreateAssetMenu(fileName = "BoardConfig", menuName = "Scriptable Objects/BoardConfig")]

public class BoardConfig : ScriptableObject
{
    public int levelIndex;
    public int weidth; 
    public int height;
    public PointsOrMatches goalType = PointsOrMatches.points;
    public int goal;
    public List<Animal> animals;
    public int maxMoves;

    [Header("Special Pieces")]
    public Animal wolf;
    public Animal sheep;
    public Animal boneBlock;
}
