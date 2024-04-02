using System.Collections.Concurrent;

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

internal readonly struct StrategyR() : IStrategy
{
    public string Name { get; }
    // R[r,d]: r = 剩余轮数；d = 差距diff; 值：(最大胜率选择, 最大胜率)
    public readonly (int, double)[,] R { get; }
    public readonly int MaxDiff { get; }

    public StrategyR(string against, GameChoiceMap choiceMap, Func<int, int> strategyGetChoice, bool isFirstMove) : this()
    {
        Name = $"R{against}";
        MaxDiff = choiceMap.Choices.Max() * Game.GameRounds;
        R = new (int, double)[Game.GameRounds, 2 * MaxDiff + 1];
        Dp(choiceMap, strategyGetChoice, isFirstMove);
    }

    public int GetChoice(Game game, Player currentPlayer)
    {
        int diff = game.GetScoreDiff(currentPlayer);
        (int bestChoice, _) = R[Game.GameRounds - game.CurrentRound - 1, diff + 100];
        return bestChoice;
    }

    // 状态转移函数。key: (diff, x, new_diff); value: prob(new_diff)
    private static Dictionary<(int, int), double> F(GameChoiceMap choiceMap, int d, Func<int, int> strategyGetChoice)
    {
        ConcurrentDictionary<(int, int), int> preRet = [];
        // 单轮情况可以用四元组 (a, b, hit_a, hit_b) 表示
        // 先手
        // 遍历所有情况
        //   给定先手选择
        foreach (var choiceA in choiceMap.Choices)
        {
            // 给定先手是否命中
            for (int hitA = 0; hitA <= 1; hitA++)
            {
                // 从策略获取选择
                int newDiff = d + choiceA * hitA;
                int choiceB = strategyGetChoice(-newDiff);
                // 后手未命中
                int prob = ProbRound(choiceMap, choiceA, choiceB, hitA, 0);
                preRet.AddOrUpdate((choiceA, newDiff), prob, (k, v) => v + prob);
                // 后手命中
                prob = ProbRound(choiceMap, choiceA, choiceB, hitA, 1);
                preRet.AddOrUpdate((choiceA, newDiff - choiceB), prob, (k, v) => v + prob);
            }
        }
        Dictionary<(int, int), double> ret = [];
        foreach (var kvp in preRet)
            ret.Add(kvp.Key, Convert.ToDouble(kvp.Value) / 100_00);
        return ret;
    }

    private void Dp(GameChoiceMap choiceMap, Func<int, int> strategyGetChoice, bool isFirstMove)
    {
        int diffCount = MaxDiff * 2 + 1;
        // 预计算f[x,d]
        Dictionary<(int, int), double>[] fs = new Dictionary<(int, int), double>[diffCount];
        // 准备一个字典用来统计所有可能的 x - <R> 值
        Dictionary<int, double> nextRateByChoices = [];
        for (int d = 0; d < diffCount; d++)
        {
            int diff = d - MaxDiff;
            fs[d] = F(choiceMap, diff, strategyGetChoice);
        }
        // 计算最后一轮的R
        CalcRateLastRound(choiceMap, fs, isFirstMove);
        // 倒推计算之前每一轮的R
        for (int r = 1; r < Game.GameRounds; r++)
        {
            // 考虑每一个可能的d
            for (int d = 0; d < diffCount; d++)
            {
                int diff = d - MaxDiff;
                // 考虑每一个可能的选择x'与对应的d'
                //  - 给定选择x，可以根据R[r-1, d']计算其期望胜率
                nextRateByChoices.Clear();
                foreach (var kvp in fs[d])
                {
                    (int choice, int nextDiff) = kvp.Key;
                    // 只考虑范围内的差距，超出范围拉倒。第n轮最大差距绝对值 = n * 10
                    int maxDiffAbs = r * 10;
                    if (nextDiff < -maxDiffAbs || nextDiff > maxDiffAbs)
                        continue;
                    // 查询达到此d'的概率 =currentProb
                    double currentProb = kvp.Value;
                    // 获取该d'在下一轮的胜率R[r-1,d']。应在之前计算过
                    (_, double nextRate) = R[r - 1, nextDiff + MaxDiff];
                    // 当前轮的R[r, d] = 对所有R[r-1,d_i]求加权平均，即R[r-1,d]的期望
                    // 直接累加currentProb * R[r-1,d_i]到给定choice
                    double addNextRate = currentProb * nextRate;
                    nextRateByChoices.TryGetValue(choice, out double oldNextRate);
                    nextRateByChoices.AddOrUpdate(choice, addNextRate, (k, v) => v + addNextRate);
                    // x = {choice}: 转移到{nextDiff}概率为{currentProb}. 下一轮{nextDiff}情况下胜率为{nextRate}. 现在选{choice}胜率更新为{oldNextRate} + {currentProb} x {nextRate} = {nextRateByChoices[choice]}
                }
                // 找到R最大的选择，作为最优策略并记录
                if (nextRateByChoices.Count == 0)
                    R[r, d] = (10, 0);
                else
                {
                    var bestKv = nextRateByChoices.MaxBy(kv => kv.Value);
                    R[r, d] = (bestKv.Key, bestKv.Value);
                }
            }
        }
        // 打印策略总表
        for (int r = 0; r < Game.GameRounds; r++)
        {
            int maxDiff = (9 - r) * 10;
            Console.WriteLine($"第{10 - r}轮：");
            for (int d = 0; d < diffCount; d++)
            {
                int diff = d - MaxDiff;
                if (Math.Abs(diff) > maxDiff)
                    continue;
                (int bestChoice, double rate) = R[r, d];
                if (rate < 0.0001)
                    continue;
                string verb = diff >= 0 ? "领先" : "落后";
                Console.WriteLine($" * {verb}{Math.Abs(diff)}分：选择{bestChoice,-2} - 胜率: {Math.Round(rate, 5)}");
            }
        }
    }
    private void CalcRateLastRound(GameChoiceMap choiceMap, Dictionary<(int, int), double>[] fs, bool isFirstMove)
    {
        int diffCount = MaxDiff * 2 + 1;
        if (isFirstMove)
        {
            // 准备一个字典用来统计所有可能的 x - <R> 值
            Dictionary<int, double> nextRateByChoices = [];
            // 计算最后一轮的R
            for (int d = 0; d < diffCount; d++)
            {
                int diff = d - MaxDiff;
                // 由于是最后一轮，选择x胜率 = 选择x的所有d'大于0概率之和
                // 考虑每一个选择x以及其转移到的d'
                nextRateByChoices.Clear();
                foreach (var kv in fs[d])
                {
                    (int choice, int nextDiff) = kv.Key;
                    // d' < 0，舍去
                    if (nextDiff <= 0)
                        continue;
                    // 选{choice}有{kv.Value}的概率转移到d'={nextDiff} > 0 ，选{choice}的胜率更新为{nextRateByChoices[choice]}
                    nextRateByChoices.AddOrUpdate(choice, kv.Value, (k, v) => v + kv.Value);
                }
                if (nextRateByChoices.Count == 0) // 没有任何可胜利选项，随便选一个拉倒
                {
                    R[0, d] = (10, 0);
                    continue;
                }
                var bestKv = nextRateByChoices.MaxBy(kv => kv.Value);
                // 最后一轮d={diff}时，最优选择为{bestKv.Key}, 胜率为{bestKv.Value}
                R[0, d] = (bestKv.Key, bestKv.Value);
            }
        }
        else
        {
            for (int d = 0; d < diffCount; d++)
            {
                int diff = d - MaxDiff;
                // 后手最后一轮最优策略是已知的，即策略3，直接填写
                if (diff >= 0) // 领先选1 必胜
                {
                    R[0, d] = (1, 1);
                    continue;
                }
                diff = Math.Abs(diff);
                foreach (var choice in choiceMap.Choices)
                {
                    if (choice > diff) // 否则若能反超选最小反超结果，胜率即该选择的命中率
                    {
                        R[0, d] = (choice, choiceMap.Probabilities[choice]);
                        continue;
                    }
                }
                // 无反超可能，随便选个10，胜率填0
                R[0, d] = (10, 0);
            }
        }
    }
    private static int ProbRound(GameChoiceMap choiceMap, int choiceA, int choiceB, int hitA, int hitB)
    {
        int probA = hitA == 1
            ? choiceMap.Probabilities[choiceA]
            : 100 - choiceMap.Probabilities[choiceA];
        if (probA == 0)
            return 0;
        int probB = hitB == 1
            ? choiceMap.Probabilities[choiceB]
            : 100 - choiceMap.Probabilities[choiceB];
        if (probB == 0)
            return 0;
        return probA * probB;
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
        int round = game.CurrentRound + 1;
        // 超出范围则抄策略3
        if (diff < -9 || diff > 10)
            return Choice3(game, diff);

        // 落后9-5
        if (-9 <= diff & diff <= -5)
            //  - 所有轮：10
            return 10;

        // 落后4-3
        if (-4 <= diff && diff <= -3)
        {
            //  - 第10轮：反超最小分
            if (round == 10)
                return Choice3(game, diff);
            //  - 其他：5
            else
                return 5;
        }

        // 落后2-1
        if (-2 <= diff && diff <= -1)
            // 所有轮：反超最小分
            return Choice3(game, diff);

        // 平
        if (diff == 0)
            return 1;

        // 领先1-4
        if (1 <= diff && diff <= 4)
        {
            // 第10轮：3和4特殊处理
            if (round == 10)
            {
                if (diff == 3)
                    return 2;
                if (diff == 4)
                    return 1;
            }
            // 其他：1
            return 1;
        }

        // 领先5-10分，投choices[index]，其中index = round - diff
        // 例如，第7轮领先5分，投choices[7-5] = 3；第7轮领先7分，投choices[7-7] = 0
        // 第9轮领先6分，投choices[9-6] = 4
        if (5 <= diff && diff <= 10)
            return game.ChoiceMap.Choices[round - diff];
        // 不应该有漏过的情况
        throw new Exception(diff.ToString());
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
