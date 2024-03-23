const int DefaultSampleSize = 100_0000;
const int DefaultRunCount = 10;

if (args.Length == 1 && (args[0] == "help" || args[0] == "-h" || args[0] == "--help"))
{
    Console.WriteLine("Usage:");
    Console.WriteLine("sampler help|-h|--help   - Print this help");
    Console.WriteLine("sampler                  - Generate winning rate matrix");
    Console.WriteLine("sampler N                - Generate winning rate matrix (Sample size = N)");
    Console.WriteLine($"sampler A B              - Run {DefaultRunCount} game(s) between player A and player B with details");
    Console.WriteLine("sampler A B N            - Generate winning rate between player A and player B (Sample size = N)\n");
    return;
}

Dictionary<int, int> choices = new()
{
    {1, 100},
    {2, 48},
    {3, 30},
    {4, 20},
    {5, 15},
    {10, 7},
};
int[] sortedChoices = [.. choices.Keys];
Array.Sort(sortedChoices);


Dictionary<string, IPlayer> players = new()
{
    { "1", new Player1("1") },
    { "2", new Player2("2") },
    { "3", new Player3("3", sortedChoices) },
};

int sampleSize = args.Length switch
{
    1 => GetSampleSize(args[0]),
    3 => GetSampleSize(args[2]),
    _ => DefaultSampleSize,
};

if (args.Length <= 1)
{
    await GenerateMatrix(players, sampleSize);
}
else
{
    var playerA = GetPlayer(args[0]);
    var playerB = GetPlayer(args[1]);
    if (args.Length == 2)
        Sample(playerA, playerB);
    else
    {
        (double rateA, double rateDraw, double rateB) = WinningRate(playerA, playerB, sampleSize);
        PrintWinningRate(rateA, rateDraw, rateB);
    }
}

Environment.Exit(0);


//////////////////////////////////////////////////////////////////


async Task GenerateMatrix(Dictionary<string, IPlayer> players, int sampleSize)
{
    string[] playerNames = [.. players.Keys];
    Csv fullTable = new(playerNames, 2);
    Csv compactTable = new(playerNames);
    int counter = 0, totalCount = playerNames.Length * playerNames.Length;
    for (int i = 0; i < playerNames.Length; i++)
    {
        var playerNameA = playerNames[i];
        fullTable.NewRow(playerNameA);
        compactTable.NewRow(playerNameA);
        var playerA = players[playerNameA];
        for (int j = 0; j < playerNames.Length; j++)
        {
            Console.WriteLine("---------------------");
            var playerB = players[playerNames[j]];
            (double rateA, double rateDraw, double rateB) = WinningRate(playerA, playerB, sampleSize, false);
            PrintWinningRate(rateA, rateDraw, rateB);
            fullTable.AppendElements($"{rateA:f4}", $"{rateDraw:f4}", $"{rateB:f4}");
            compactTable.AppendElements($"{rateA:f4}");
            counter++;
            Console.WriteLine($"[{counter}/{totalCount}] {100.0 * counter / totalCount:f3}%");
            Console.WriteLine("---------------------\n");
        }
    }
    await File.WriteAllTextAsync("full.csv", fullTable.ToString());
    await File.WriteAllTextAsync("compact.csv", compactTable.ToString());
}

void Sample(IPlayer playerA, IPlayer playerB)
{
    for (int i = 0; i < DefaultRunCount; i++)
    {
        Game game = new(choices, playerA, playerB);
        game = new(choices, playerA, playerB);
        game.Reset();
        game.RunToEnd();
        Console.WriteLine($"Run #{i}:");
        Console.WriteLine(game.GetHistoryString());
    }
}

(double, double, double) WinningRate(IPlayer playerA, IPlayer playerB, int sampleSize, bool showProgress = true)
{
    Console.WriteLine($"Evaluating: [{playerA} vs {playerB}] Sample size: {sampleSize}");
    ProgressControl progress = new(0, sampleSize);
    int aWinCounter = 0, bWinCounter = 0, drawCounter = 0;
    if (showProgress)
        progress.Show();
    Parallel.For(0, sampleSize, i =>
    {
        Game game = new(choices, playerA, playerB);
        game.Reset();
        game.RunToEnd();
        progress.Increment();
        if (game.IsPlayerALeading)
            Interlocked.Increment(ref aWinCounter);
        if (game.IsPlayerBLeading)
            Interlocked.Increment(ref bWinCounter);
        if (game.IsDraw)
            Interlocked.Increment(ref drawCounter);
    });
    double rateA = 1.0 * aWinCounter / sampleSize;
    double rateB = 1.0 * bWinCounter / sampleSize;
    double rateDraw = 1.0 * drawCounter / sampleSize;
    return (rateA, rateDraw, rateB);
}

static int GetSampleSize(string s)
{
    s = s.Replace("_", "");
    if (!int.TryParse(s, out int sampleSize))
    {
        Console.WriteLine($"Invalid sample size \"{s}\"");
        Environment.Exit(-1);
    }
    return sampleSize;
}

IPlayer GetPlayer(string name)
{
    name = name.Trim();
    if (!players.TryGetValue(name, out IPlayer? player))
    {
        Console.WriteLine($"Unknown player \"{name}\"");
        Console.WriteLine("Player lists:");
        foreach (var knownPlayer in players)
            Console.WriteLine($" - Name:{knownPlayer.Key} (ID: {knownPlayer.Value})");
        Environment.Exit(-1);
    }
    return player;
}

static void PrintWinningRate(double rateA, double rateDraw, double rateB)
{
    Console.WriteLine($"\nWinning rate [A/Draw/B]: {rateA:f4}/{rateDraw:f4}/{rateB:f4}    Sum:{rateA + rateDraw + rateB}");
}
