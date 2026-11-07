using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BoardTests
{
    private static List<Vector2Int> CallMatchesFound(Board board)
    {
        var method = typeof(Board).GetMethod("MatchesFound", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, "Could not find MatchesFound() via reflection. Did you rename it?");

        return (List<Vector2Int>)method.Invoke(board, null);
    }

    private static Animal NewAnimal()
    {
        return ScriptableObject.CreateInstance<Animal>();
    }

    private static BoardConfig MakeConfig(int width, int height, List<Animal> animals)
    {
        var cfg = ScriptableObject.CreateInstance<BoardConfig>();

        cfg.weidth = width;
        cfg.height = height;
        cfg.animals = animals;

        return cfg;
    }

    [Test]
    public void Initialize_ValidConfig_NoStartingMatches()
    {
        //setup
        UnityEngine.Random.InitState(12345);

        var animals = new List<Animal>
        {
            NewAnimal(), NewAnimal(), NewAnimal(),
            NewAnimal(), NewAnimal(), NewAnimal()
        };

        var config = MakeConfig(width: 8, height: 8, animals: animals);
        var board = new Board(config);

        //act
        board.Initialize();
        var matches = CallMatchesFound(board);

        //assert
        Assert.That(matches, Is.Empty, "Board.Initialize() should not create starting matches.");
    }

    [Test]
    public void MatchesFound_KnownVerticalAndHorizontalRuns_ReturnsAllMatchedCells()
    {
        //setup
        var a = NewAnimal();
        var b = NewAnimal();

        var animals = new List<Animal> { a, b };
        var config = MakeConfig(width: 5, height: 5, animals: animals);
        var board = new Board(config);

        // Fill with 'b' so only the matches we create will be detected
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                board.SetAnimalInCell(new Vector2Int(x, y), b);
            }
        }

        // Vertical run at x=2, y=0..2
        board.SetAnimalInCell(new Vector2Int(2, 0), a);
        board.SetAnimalInCell(new Vector2Int(2, 1), a);
        board.SetAnimalInCell(new Vector2Int(2, 2), a);

        // Horizontal run at y=4, x=1..3
        board.SetAnimalInCell(new Vector2Int(1, 4), a);
        board.SetAnimalInCell(new Vector2Int(2, 4), a);
        board.SetAnimalInCell(new Vector2Int(3, 4), a);

        //act
        var matches = CallMatchesFound(board);

        //assert
        Assert.That(matches, Does.Contain(new Vector2Int(2, 0)));
        Assert.That(matches, Does.Contain(new Vector2Int(2, 1)));
        Assert.That(matches, Does.Contain(new Vector2Int(2, 2)));

        Assert.That(matches, Does.Contain(new Vector2Int(1, 4)));
        Assert.That(matches, Does.Contain(new Vector2Int(2, 4)));
        Assert.That(matches, Does.Contain(new Vector2Int(3, 4)));
    }
}
