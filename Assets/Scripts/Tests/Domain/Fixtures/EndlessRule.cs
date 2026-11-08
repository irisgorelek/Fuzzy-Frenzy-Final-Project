using FuzzyFrenzy.Domain;

internal sealed class EndlessRule : ActionRule<BoardSettledAction>
{
    protected override void React(TurnRun run, BoardSettledAction applied) => run.Enqueue(new BoardSettledAction());
}
