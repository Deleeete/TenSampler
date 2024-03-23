internal abstract class PlayerBase : IPlayer
{
    public string Id { get; }

    public PlayerBase(string name)
    {
        Id = $"{name} <{GetHashCode():X8}>";
    }
    public abstract int GetChoice(Game game);
    public override string ToString()
        => Id;
}

internal class Player1(string name) : PlayerBase(name)
{
    public override int GetChoice(Game game)
    {
        return 1;
    }
}

internal class Player2(string name) : PlayerBase(name)
{
    public override int GetChoice(Game game)
    {
        if (!game.IsPlayerLeading(this))
            return 10;
        else
            return 1;
    }
}

internal class Player3(string name, int[] sortedChoices) : PlayerBase(name)
{
    private readonly int[] _sortedChoices = sortedChoices;

    public override int GetChoice(Game game)
    {
        int diff = game.GetScoreDiff(this);
        if (diff >= 0)
            return 1;
        diff = Math.Abs(diff);
        foreach (var choice in _sortedChoices)
        {
            if (choice > diff)
                return choice;
        }
        return _sortedChoices.Last();
    }
}
