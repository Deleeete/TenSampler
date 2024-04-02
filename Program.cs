using System.Collections.Concurrent;

const int DefaultSampleSize = 1000_0000;

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
    // { "a2", new StrategyA2() },
    // { "a3", new StrategyA3() },
    // { "b3", new StrategyB3() },
    { "R1", new StrategyR("1", choiceMap, GetChoice1, false) },
    { "R2", new StrategyR("2", choiceMap, GetChoice2, false) },
    { "R3", new StrategyR("3", choiceMap, GetChoice3, true) },
    { "P", new StrategyP() },
    //{ "8", new StrategyAny(8) },
    //{ "randomer", new StrategyRandom() }
};

//foreach (var choice in probs.Keys)
//{
//    if (choice == 1) // all-1 is already covered by type-1
//        continue;
//    var player = new StrategyAllN(choice);
//    strategies.Add(player.Name, player);
//}

// FixedStrategies(DefaultSampleSize);

try
{
    if (args.Length == 1 && (args[0] == "help" || args[0] == "-h" || args[0] == "--help"))
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("sampler help|-h|--help   - Print this help");
        Console.WriteLine("sampler                  - Generate winning rate matrix");
        Console.WriteLine("sampler N                - Generate winning rate matrix (Sample size = N)");
        Console.WriteLine($"sampler A B              - Run {Game.GameRounds} game(s) between player A and player B with details");
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
            (double rateA, double rateDraw, double rateB, var histA, var histB) = WinningRate(playerA, playerB, sampleSize, parallel:false);
            PrintWinningRate(rateA, rateDraw, rateB);
            PrintHist(histA, sampleSize);
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex);
}

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
            fullTable.AppendElements($"{rateA:f8}", $"{rateDraw:f8}", $"{rateB:f8}");
            compactTable.AppendElements($"{rateA:f8}");
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
    for (int i = 0; i < Game.GameRounds; i++)
    {
        Game game = new(choiceMap, playerA, playerB);
        game = new(choiceMap, playerA, playerB);
        game.Reset();
        game.RunToEnd();
        Console.WriteLine($"Run #{i} [{playerA} vs {playerB}]");
        Console.WriteLine(game.GetHistoryString());
    }
}

WinningRateResult WinningRate(Player playerA, Player playerB, int sampleSize, bool showProgress = true, bool parallel = true)
{
    Console.WriteLine($"Evaluating: [{playerA} vs {playerB}] Sample size: {sampleSize}");
    ProgressControl progress = new(0, sampleSize);
    if (showProgress)
        progress.Show();
    ConcurrentDictionary<int, int> histScoreA = new();
    ConcurrentDictionary<int, int> histScoreB = new();
    int aWinCounter = 0, bWinCounter = 0, drawCounter = 0;
    if (parallel)
    {
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
    }
    else
    {
        for (int i = 0; i < sampleSize; i++)
        {
            Game game = new(choiceMap, playerA, playerB);
            game.Reset();
            game.RunToEnd();
            histScoreA.AddOrUpdate(game.ScoreA, 1, (k, v) => v + 1);
            histScoreB.AddOrUpdate(game.ScoreB, 1, (k, v) => v + 1);
            progress.Increment();
            if (game.IsPlayerALeading)
                aWinCounter++;
            if (game.IsPlayerBLeading)
                bWinCounter++;
            if (game.IsDraw)
            {
                // Console.WriteLine(game.GetHistoryString());
                drawCounter++;
            }
        }
    }
    double rateA = Convert.ToDouble(aWinCounter) / sampleSize;
    double rateB =Convert.ToDouble(bWinCounter) / sampleSize;
    double rateDraw = Convert.ToDouble(drawCounter) / sampleSize;
    return new(rateA, rateDraw, rateB, histScoreA, histScoreB);
}



int GetChoice1(int _)
    => 1;

int GetChoice2(int diff)
    => diff > 0 ? 1 : 10;

int GetChoice3(int diff)
{
    if (diff >= 0)
        return 1;
    diff = Math.Abs(diff);
    foreach (var choice in choiceMap.Choices)
    {
        if (choice > diff)
            return choice;
    }
    return choiceMap.Choices.Last();
}

void FixedStrategies(int sampleSize)
{
    // 计算排列数
    int fixStrategyCount = (int)Math.Pow(Game.GameRounds, 6);
    Player type3 = new(strategies["3"]);
    (int, double)[] results = new (int, double)[fixStrategyCount];
    ProgressControl progress = new(0, fixStrategyCount);
    progress.Show();
    Parallel.For(0, fixStrategyCount, code =>
    {
        StrategyFixed fix = new(code);
        Player player = new(fix);
        int aWinCounter = 0;
        for (int i = 0; i < sampleSize; i++)
        {
            Game game = new(choiceMap, player, type3);
            game.RunToEnd();
            if (game.IsPlayerALeading)
                aWinCounter++;
        }
        results[code] = (code, Convert.ToDouble(aWinCounter) / sampleSize);
        progress.Increment();
    });

    Csv fixedCsv = new(["code", "choices", "rate"]);
    results.OrderByDescending(result => result.Item2).ToList().ForEach(ret =>
    {
        fixedCsv.NewRow(ret.Item1.ToString());
        // string choicesIndexes = Utils.ConvertBase(ret.Item1, 6);
        // Span<char> choices = stackalloc char[Game.GameRounds];
        // for (int i = 0; i < choices.Length; i++)
        // {
        //     int choiceIndex = choicesIndexes[i] - '0';
        //     int choice = choiceMap.Choices[choiceIndex]; 
        //     choices[i] = choice == 10 ? 'X' : choice.ToString()[0];
        // }
        // game5.AppendElements(choices);
        Span<char> base6 = Utils.ConvertBase(ret.Item1, 6).PadLeft(Game.GameRounds, '0').ToCharArray();
        for (int i = 0; i < base6.Length; i++)
            base6[i]++;
        fixedCsv.AppendElements(new string(base6), ret.Item2.ToString());
    });
    File.WriteAllText($"game{Game.GameRounds}.csv", fixedCsv.ToString());
    Environment.Exit(0);
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
    Console.WriteLine($"\nWinning rate [A/Draw/B]: {rateA:f6}/{rateDraw:f6}/{rateB:f6}    Sum:{rateA + rateDraw + rateB}");
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

record struct WinningRateResult(double RateA, double RateDraw, double RateB, IDictionary<int, int> ScoreBinsA, IDictionary<int, int> ScoreBinsB);
