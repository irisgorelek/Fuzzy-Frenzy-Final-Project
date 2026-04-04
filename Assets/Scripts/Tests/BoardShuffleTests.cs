using NUnit.Framework;
using UnityEngine;

public class BoardShuffleTests
{
    private Animal MakeAnimal(string id)
    {
        var a = ScriptableObject.CreateInstance<Animal>();
        a._id = id;
        a._canSwap = true;
        a._canMatch = true;
        a._affectedByGravity = true;
        a._spawnWeight = 1f;
        a._points = 1;
        return a;
    }

    private Animal MakeBlocker(string id)
    {
        var a = ScriptableObject.CreateInstance<Animal>();
        a._id = id;
        a._canSwap = false;
        a._canMatch = false;
        a._affectedByGravity = false;
        a._spawnWeight = 0f;
        a._points = 0;
        return a;
    }

    private Board CreateBoard(int width, int height, params Animal[] animals)
    {
        var cfg = ScriptableObject.CreateInstance<BoardConfig>();
        cfg.weidth = width;
        cfg.height = height;
        cfg.goal = 10;
        cfg.goalType = PointsOrMatches.points;
        cfg.animals = new System.Collections.Generic.List<Animal>(animals);

        return new Board(cfg);
    }

    [Test]
    public void ShuffleUntilPlayable_DeadBoard_BecomesPlayableWithoutImmediateMatches()
    {
        var a = MakeAnimal("A");
        var b = MakeAnimal("B");
        var c = MakeAnimal("C");
        var d = MakeAnimal("D");

        var board = CreateBoard(4, 4, a, b, c, d);
        board.Initialize();

        // Overwrite with a known dead board pattern.
        // Replace with a pattern you verified has:
        // 1) no current matches
        // 2) no valid swaps that create a match
        Animal[,] dead =
        {
            { a, b, c, d },
            { c, d, a, b },
            { b, a, d, c },
            { d, c, b, a }
        };

        for (int x = 0; x < 4; x++)
            for (int y = 0; y < 4; y++)
                board.SetAnimalInCell(new Vector2Int(x, y), dead[x, y]);

        var hintFinder = new BoardHintFinder();

        Assert.That(board.FindMatches().Count, Is.EqualTo(0), "Dead board should start without auto matches.");
        Assert.That(hintFinder.TryFindHint(board, out _), Is.False, "Dead board should start without legal moves.");

        bool success = board.ShuffleUntilPlayable(hintFinder, 200);

        Assert.That(success, Is.True, "Shuffle should eventually produce a playable board.");
        Assert.That(board.FindMatches().Count, Is.EqualTo(0), "Shuffled board should not auto-resolve immediately.");
        Assert.That(hintFinder.TryFindHint(board, out _), Is.True, "Shuffled board should have at least one legal move.");
    }

    [Test]
    public void ShuffleSwappablePieces_DoesNotMoveBlockers()
    {
        var a = MakeAnimal("A");
        var b = MakeAnimal("B");
        var c = MakeAnimal("C");
        var blocker = MakeBlocker("Bone");

        var board = CreateBoard(4, 4, a, b, c, blocker);
        board.Initialize();

        var blockerCell = new Vector2Int(1, 1);
        board.SetAnimalInCell(blockerCell, blocker);

        board.ShuffleSwappablePieces();

        Assert.That(board.GetAnimalFromCell(blockerCell), Is.SameAs(blocker));
    }
}