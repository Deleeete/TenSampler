internal class Strategy2() : IStrategy
{
    public string Name => "type2";
    public int GetChoice(Game game, Player currentPlayer)
    {
        if (!game.IsPlayerLeading(currentPlayer))
            return 10;
        else
            return 1;
    }
}

internal class Strategy3() : IStrategy
{
    public string Name => "type3";
    public int GetChoice(Game game, Player currentPlayer)
    {
        int diff = game.GetScoreDiff(currentPlayer);
        if (diff >= 0)
            return 1;
        diff = Math.Abs(diff);
        foreach (var choice in game.ChoiceMap.Choices)
        {
            if (choice > diff)
                return choice;
        }
        return game.ChoiceMap.Choices.Last();
    }
}

internal class StrategyAllN(int n) : IStrategy
{
    public int N { get; } = n;

    public string Name => $"all-{N}";
    public int GetChoice(Game game, Player currentPlayer)
        => N;
}

internal class StrategyRandom() : IStrategy
{
    public string Name => "randomer";
    public int GetChoice(Game game, Player currentPlayer)
    {
        return game.ChoiceMap.Choices[Random.Shared.Next(game.ChoiceMap.Choices.Length)];
    }
}
