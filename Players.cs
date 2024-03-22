internal class Player1 : IPlayer
{
    public int GetChoice(Game game)
    {
        return 1;
    }
}

internal class Player2 : IPlayer
{
    public int GetChoice(Game game)
    {
        if (!game.IsPlayerLeading(this))
            return 10;
        else
            return 1;
    }
}

internal class Player3(int[] sortedChoices) : IPlayer
{
    private readonly int[] _sortedChoices = sortedChoices;

    public int GetChoice(Game game)
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
