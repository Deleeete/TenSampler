internal struct Strategy2() : IStrategy
{
    public readonly string Name => "type2";
    public readonly int GetChoice(Game game, Player currentPlayer)
    {
        if (!game.IsPlayerLeading(currentPlayer))
            return 10;
        else
            return 1;
    }
}

internal struct Strategy3() : IStrategy
{
    public readonly string Name => "type3";
    public readonly int GetChoice(Game game, Player currentPlayer)
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

internal readonly struct StrategyAllN(int n) : IStrategy
{
    public int N { get; } = n;

    public string Name => $"all-{N}";
    public int GetChoice(Game game, Player currentPlayer)
        => N;
}

internal readonly struct StrategyRandom() : IStrategy
{
    public string Name => "randomer";
    public int GetChoice(Game game, Player currentPlayer)
    {
        return game.ChoiceMap.Choices[Random.Shared.Next(game.ChoiceMap.Choices.Length)];
    }
}

internal readonly struct StrategyA2() : IStrategy
{
    public string Name => "A2";

    public int GetChoice(Game game, Player currentPlayer)
    {
        if (game.CurrentRound < 9)
            return 10;
        else
            return 1;
    }
}

internal readonly struct StrategyA3() : IStrategy
{
    public string Name => "A3";

    public int GetChoice(Game game, Player currentPlayer)
    {
        if (game.CurrentRound == 0)
            return 1;
        if (game.CurrentRound < 9)
            return 10;
        else
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
}

internal readonly struct StrategyB3() : IStrategy
{
    public string Name => "B3";

    public int GetChoice(Game game, Player currentPlayer)
    {
        int diff = game.GetScoreDiff(currentPlayer);
        if (diff > 0)
            return 10;
        else if (diff == 0)
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

internal readonly struct StrategyC3() : IStrategy
{
    public string Name => "C3";
    public int GetChoice(Game game, Player currentPlayer)
    {
        if (game.CurrentRound == 0)
            return 2;
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

internal readonly struct StrategyFixed(int code) : IStrategy
{
    public string Name => $"fix-{code}";
    public int[] ChoiceIndexes { get; } = Decode(code);

    public int GetChoice(Game game, Player currentPlayer)
    {
        return game.ChoiceMap.Choices[ChoiceIndexes[game.CurrentRound]];
    }

    private static int[] Decode(int code)
    {
        // 转换为N位六进制数
        string strBase6 = new(Utils.ConvertBase(code, 6).PadLeft(Game.GameRounds, '0'));
        //Console.WriteLine($"Code: {code} -> {strBase6}");
        int[] choices = new int[strBase6.Length];
        for (int i = 0; i < strBase6.Length; i++)
            choices[i] = strBase6[i] - '0';
        return choices;
    }
}

internal readonly struct StrategyR((int, double)[,] R) : IStrategy
{
    public string Name => $"R";
    private readonly (int, double)[,] _R = R;

    public int GetChoice(Game game, Player currentPlayer)
    {
        int diff = game.GetScoreDiff(currentPlayer);
        (int bestChoice, double rate) = _R[Game.GameRounds - game.CurrentRound - 1, diff + 100];
        // Console.WriteLine($" * Round[{game.CurrentRound}] Diff = {diff}: The best choice is {bestChoice} with winning rate of {rate}");
        return bestChoice;
    }
}

internal readonly struct StrategyP : IStrategy
{
    public string Name => $"P";

    public int GetChoice(Game game, Player currentPlayer)
    {
        if (game.CurrentRound == 0)
            return 1;
        int diff = game.GetScoreDiff(currentPlayer);
        if (diff == 0)
            return 1;
        switch (game.CurrentRound)
        {
            case 0:
                return 1;
            case 1:
                return game.IsPlayerLeading(currentPlayer) ? 1 : 2;
            case 2:
                return game.IsPlayerLeading(currentPlayer) ? 1 : Choice3(game, diff);
            case 3:
            case 4:
            case 5:
            case 6:
                if (1 <= diff && diff <= 4)
                    return 1;
                if (5 <= diff && diff <= 6)
                    return 2;
                if (diff == -1 || diff == -2)
                    return Choice3(game, diff);
                if (diff == -3 || diff == -4)
                    return 5;
                if (diff == -5 || diff == -6)
                    return 10;
                return Choice3(game, diff);
            case 7:
                if (1 <= diff && diff <= 4)
                    return 1;
                if (5 <= diff && diff <= 7)
                    return 9 - diff;
                if (diff == -1 || diff == -2)
                    return Choice3(game, diff);
                if (diff == -3 || diff == -4)
                    return 5;
                if (-5 >= diff && diff >= -7)
                    return 10;
                return Choice3(game, diff);
            case 8:
                if (1 <= diff && diff <= 4)
                    return 1;
                if (5 <= diff && diff <= 8)
                    return 10 - diff;
                if (diff == -1 || diff == -2)
                    return Choice3(game, diff);
                if (diff == -3 || diff == -4)
                    return 5;
                if (-5 >= diff && diff >= -8)
                    return 10;
                return Choice3(game, diff);
            case 9:
                if (diff <= 2)
                    return 1;
                if (diff == 3 && diff == 4)
                    return 5 - diff;
                if (diff == 5)
                    return 10;
                if (6 <= diff && diff <= 9)
                    return 11 - diff;
                if (-1 >= diff && diff >= -4)
                    return Choice3(game, diff);
                if (-5 >= diff && diff >= -9)
                    return 10;
                return Choice3(game, diff);
            default:
                throw new Exception(game.CurrentRound.ToString());
        }
    }

    private static int Choice3(Game game, int diff)
    {
        diff = Math.Abs(diff);
        foreach (var choice in game.ChoiceMap.Choices)
        {
            if (choice > diff)
                return choice;
        }
        return game.ChoiceMap.Choices.Last();
    }
}
