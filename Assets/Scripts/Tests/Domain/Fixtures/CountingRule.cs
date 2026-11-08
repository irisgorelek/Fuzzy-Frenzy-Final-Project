using FuzzyFrenzy.Domain;

internal sealed class CountingRule<TAction> : ActionRule<TAction> where TAction : IBoardAction
{
    public int Count;
    protected override void React(TurnRun run, TAction applied) => Count++;
}
