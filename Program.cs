using System.Collections.Concurrent;

const int DefaultSampleSize = 100_0000;
const int DefaultRunCount = 10;

Dictionary<int, int> probs = new()
{
    {1, 100},
    {2, 48},
    {3, 30},
    {4, 20},
    {5, 15},
    {10, 7},
};
GameChoiceMap choiceMap = new(probs);

Dictionary<string, IStrategy> strategies = new()
{
    { "1", new StrategyAllN(1) },
    { "2", new Strategy2() },
    { "3", new Strategy3() },
    { "randomer", new StrategyRandom() }
};

foreach (var choice in probs.Keys)
{
    if (choice == 1) // all-1 is already covered by type-1
        continue;
    var player = new StrategyAllN(choice);
    strategies.Add(player.Name, player);
}


try
{
    if (args.Length == 1 && (args[0] == "help" || args[0] == "-h" || args[0] == "--help"))
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("sampler help|-h|--help   - Print this help");
        Console.WriteLine("sampler                  - Generate winning rate matrix");
        Console.WriteLine("sampler N                - Generate winning rate matrix (Sample size = N)");
        Console.WriteLine($"sampler A B              - Run {DefaultRunCount} game(s) between player A and player B with details");
        Console.WriteLine("sampler A B N            - Generate winning rate between player A and player B (Sample size = N)\n");
        PrintStrategies();
        return;
    }

    int sampleSize = args.Length switch
    {
        1 => GetSampleSize(args[0]),
        3 => GetSampleSize(args[2]),
        _ => DefaultSampleSize,
    };

    if (args.Length <= 1)
    {
        await GenerateMatrix(strategies, sampleSize);
    }
    else
    {
        Player playerA = new(GetStrategy(args[0]));
        Player playerB = new(GetStrategy(args[1]));
        if (args.Length == 2)
            Run(playerA, playerB);
        else
        {
            (double rateA, double rateDraw, double rateB, var histA, var histB) = WinningRate(playerA, playerB, sampleSize);
            PrintWinningRate(rateA, rateDraw, rateB);
            PrintHist(histA, sampleSize);
        }
    }
}
catch (Exception ex)
{
    Environment.FailFast(ex.ToString());
}

Environment.Exit(0);

//////////////////////////////////////////////////////////////////


async Task GenerateMatrix(Dictionary<string, IStrategy> strategies, int sampleSize)
{
    string[] strategyNames = strategies.Values.Select(s => s.Name).ToArray();
    string[] namePairs = new string[strategyNames.Length * strategyNames.Length];
    int namePairsIndex = 0;
    foreach (var strategyName1 in strategyNames)
    {
        foreach (var strategyName2 in strategyNames)
        {
            namePairs[namePairsIndex] = $"{strategyName1} VS {strategyName2}";
        }
    }
    Csv fullTable = new(strategyNames, 3);
    Csv compactTable = new(strategyNames);
    Csv histTable = new();
    int counter = 0, totalCount = strategyNames.Length * strategyNames.Length;
    foreach (var kvpStrategyA in strategies)
    {
        fullTable.NewRow(kvpStrategyA.Value.Name);
        compactTable.NewRow(kvpStrategyA.Value.Name);
        Player playerA = new(kvpStrategyA.Value);
        foreach (var kvpStrategyB in strategies)
        {
            Console.WriteLine("---------------------");
            Player playerB = new(kvpStrategyB.Value);
            (double rateA, double rateDraw, double rateB, var histA, var histB) = WinningRate(playerA, playerB, sampleSize, false);
            PrintWinningRate(rateA, rateDraw, rateB);
            fullTable.AppendElements($"{rateA:f4}", $"{rateDraw:f4}", $"{rateB:f4}");
            compactTable.AppendElements($"{rateA:f4}");
            histTable.NewRow($"{kvpStrategyA.Key} VS {kvpStrategyB.Key}: ScoreA");
            var sortedKeysA = histA.Keys.ToArray();
            Array.Sort(sortedKeysA);
            foreach (var key in sortedKeysA)
            {
                histTable.NewRow($"{key}");
                histTable.AppendElements($"{Convert.ToDouble(histA[key]) / sampleSize}");
            }
            histTable.NewRow($"{kvpStrategyA.Key} VS {kvpStrategyB.Key}: ScoreB");
            var sortedKeysB = histB.Keys.ToArray();
            Array.Sort(sortedKeysB);
            foreach (var key in sortedKeysB)
            {
                histTable.NewRow($"{key}");
                histTable.AppendElements($"{Convert.ToDouble(histB[key]) / sampleSize}");
            }
            counter++;
            Console.WriteLine($"[{counter}/{totalCount}] {Convert.ToDouble(counter) / totalCount * 100:f3}%");
            Console.WriteLine("---------------------\n");
        }
    }
    await File.WriteAllTextAsync("full.csv", fullTable.ToString());
    await File.WriteAllTextAsync("compact.csv", compactTable.ToString());
    await File.WriteAllTextAsync("hist.csv", histTable.ToString());
}

void Run(Player playerA, Player playerB)
{
    for (int i = 0; i < DefaultRunCount; i++)
    {
        Game game = new(choiceMap, playerA, playerB);
        game = new(choiceMap, playerA, playerB);
        game.Reset();
        game.RunToEnd();
        Console.WriteLine($"Run #{i} [{playerA} vs {playerB}]");
        Console.WriteLine(game.GetHistoryString());
    }
}

(double, double, double, ConcurrentDictionary<int, int>, ConcurrentDictionary<int, int>) WinningRate(Player playerA, Player playerB, int sampleSize, bool showProgress = true)
{
    Console.WriteLine($"Evaluating: [{playerA} vs {playerB}] Sample size: {sampleSize}");
    ProgressControl progress = new(0, sampleSize);
    if (showProgress)
        progress.Show();
    ConcurrentDictionary<int, int> histScoreA = new();
    ConcurrentDictionary<int, int> histScoreB = new();
    int aWinCounter = 0, bWinCounter = 0, drawCounter = 0;
    Parallel.For(0, sampleSize, i =>
    {
        Game game = new(choiceMap, playerA, playerB);
        game.Reset();
        game.RunToEnd();
        histScoreA.AddOrUpdate(game.ScoreA, 1, (k, v) => v + 1);
        histScoreB.AddOrUpdate(game.ScoreB, 1, (k, v) => v + 1);
        progress.Increment();
        if (game.IsPlayerALeading)
            Interlocked.Increment(ref aWinCounter);
        if (game.IsPlayerBLeading)
            Interlocked.Increment(ref bWinCounter);
        if (game.IsDraw)
            Interlocked.Increment(ref drawCounter);
    });
    double rateA = Convert.ToDouble(aWinCounter) / sampleSize;
    double rateB =Convert.ToDouble(bWinCounter) / sampleSize;
    double rateDraw = Convert.ToDouble(drawCounter) / sampleSize;
    return (rateA, rateDraw, rateB, histScoreA, histScoreB);
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

IStrategy GetStrategy(string name)
{
    name = name.Trim();
    if (!strategies.TryGetValue(name, out IStrategy? strategy))
    {
        Console.WriteLine($"Unknown strategy \"{name}\"");
        PrintStrategies();
        Environment.Exit(-1);
    }
    return strategy;
}

void PrintStrategies()
{
    Console.WriteLine("Player lists:");
    foreach (var knownPlayer in strategies)
        Console.WriteLine($" - Name:{knownPlayer.Key} (ID: {knownPlayer.Value})");
}

static void PrintWinningRate(double rateA, double rateDraw, double rateB)
{
    Console.WriteLine($"\nWinning rate [A/Draw/B]: {rateA:f4}/{rateDraw:f4}/{rateB:f4}    Sum:{rateA + rateDraw + rateB}");
}

static void PrintHist(IDictionary<int, int> hist, int total)
{
    Console.WriteLine($"{hist.Count} possible scores:");
    var sortedKeys = hist.Keys.ToArray();
    Array.Sort(sortedKeys);
    foreach (var key in sortedKeys)
    {
        Console.WriteLine($" - {key,2} : {Convert.ToDouble(hist[key]) / total * 100:f4}%");
    }
}
