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

    [Test]
    public void TrySwapCells_SwapCreatesMatch_ReturnsTrue()
    {
        //setup
        var a = NewAnimal();
        var b = NewAnimal();

        var animals = new List<Animal> { a, b };
        var config = MakeConfig(width: 3, height: 3, animals: animals);
        var board = new Board(config);

        // Make a stable board (no matches), then arrange a near-match that will become a match after one swap.
        //
        // Grid (x across, y down), y=0 top:
        // y=0: A  B  A
        // y=1: B  A  B
        // y=2: B  A  B
        
        board.SetAnimalInCell(new Vector2Int(0, 0), a);
        board.SetAnimalInCell(new Vector2Int(1, 0), b);
        board.SetAnimalInCell(new Vector2Int(2, 0), a);

        board.SetAnimalInCell(new Vector2Int(0, 1), b);
        board.SetAnimalInCell(new Vector2Int(1, 1), a);
        board.SetAnimalInCell(new Vector2Int(2, 1), b);

        board.SetAnimalInCell(new Vector2Int(0, 2), b);
        board.SetAnimalInCell(new Vector2Int(1, 2), a);
        board.SetAnimalInCell(new Vector2Int(2, 2), b);

        //act
        // Swap (0,0) B with (0,1) A => columns x=0 and x=1 become A,A,A and B,B,B (matches)

        bool didSwap = board.TrySwapCells(new Vector2Int(0, 0), new Vector2Int(1, 0));

        //assert
        Assert.That(didSwap, Is.True, "Swap should be accepted when it creates a match.");
    }

    [Test]
    public void TrySwapCells_SwapDoesNotCreateMatch_ReturnsFalseAndReverts()
    {
        //setup
        var a = NewAnimal();
        var b = NewAnimal();
        var c = NewAnimal();

        var animals = new List<Animal> { a, b, c };
        var config = MakeConfig(width: 3, height: 3, animals: animals);
        var board = new Board(config);

        // A stable board with no matches.
        //
        // y=0: A  B  C
        // y=1: B  C  A
        // y=2: C  A  B
        board.SetAnimalInCell(new Vector2Int(0, 0), a);
        board.SetAnimalInCell(new Vector2Int(1, 0), b);
        board.SetAnimalInCell(new Vector2Int(2, 0), c);

        board.SetAnimalInCell(new Vector2Int(0, 1), b);
        board.SetAnimalInCell(new Vector2Int(1, 1), c);
        board.SetAnimalInCell(new Vector2Int(2, 1), a);

        board.SetAnimalInCell(new Vector2Int(0, 2), c);
        board.SetAnimalInCell(new Vector2Int(1, 2), a);
        board.SetAnimalInCell(new Vector2Int(2, 2), b);

        var beforeCell1 = board.GetAnimalFromCell(new Vector2Int(0, 0));
        var beforeCell2 = board.GetAnimalFromCell(new Vector2Int(1, 0));

        //act
        bool didSwap = board.TrySwapCells(new Vector2Int(0, 0), new Vector2Int(1, 0));

        //assert
        Assert.That(didSwap, Is.False, "Swap should be rejected when it does not create a match.");
        Assert.That(board.GetAnimalFromCell(new Vector2Int(0, 0)), Is.EqualTo(beforeCell1), "Rejected swap should revert cell1.");
        Assert.That(board.GetAnimalFromCell(new Vector2Int(1, 0)), Is.EqualTo(beforeCell2), "Rejected swap should revert cell2.");
    }
}
