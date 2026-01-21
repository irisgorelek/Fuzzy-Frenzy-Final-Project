using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BoardConfig")]
public class BoardConfig : ScriptableObject
{
    public int _weidth { get; private set; }
    public int _height { get; private set; }
    public List<Animal> _animals;
}
