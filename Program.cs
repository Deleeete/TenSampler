Dictionary<int, int> choices = new()
{
    {1, 100},
    {2, 48},
    {3, 30},
    {4, 20},
    {5, 15},
    {10, 7},
};

int[] sortedChoices = choices.Keys.ToArray();
Array.Sort(sortedChoices);
Player1 p1 = new();
Player2 p2 = new();
Player3 p3 = new(sortedChoices);

Dictionary<string, IPlayer> players = new()
{
    { "1", p1 },
    { "2", p2 },
    { "3", p3 },
};

var playerA = players[args[0]];
var playerB = players[args[1]];

Console.WriteLine($"PlayerA: {args[0]} <{playerA.GetHashCode():X8}>");
Console.WriteLine($"PlayerB: {args[1]} <{playerB.GetHashCode():X8}>");

if (args.Length == 2)
    Test();
else
    Sample();


void Test()
{
    for (int i = 0; i < 10; i++)
    {
        Game game = new(choices, playerA, playerB);
        game = new(choices, playerA, playerB);
        game.Reset();
        game.RunToEnd();
        Console.WriteLine(game.GetHistoryString());
    }
}

void Sample()
{
    int count = Convert.ToInt32(args[2]);
    Console.WriteLine($"Session count: {count}");
    Console.WriteLine("Evaluating...");
    int counter = 0;
    bool end = false;
    _ = Task.Run(() =>
    {
        while (!end)
        {
            Console.Write($"{100.0 * counter / count:f3}%\t");
            Thread.Sleep(1000);
        }
        Console.WriteLine();
    });
    int aWinCounter = 0, bWinCounter = 0;
    Parallel.For(0, count, i =>
    {
        Game game = new(choices, playerA, playerB);
        game.Reset();
        game.RunToEnd();
        Interlocked.Increment(ref counter);
        if (game.IsPlayerALeading)
            Interlocked.Increment(ref aWinCounter);
        if (game.IsPlayerBLeading)
            Interlocked.Increment(ref bWinCounter);
    });
    end = true;
    Console.WriteLine("Done.");
    Console.WriteLine($"Winning rate [A/Draw/B]: {1.0 * aWinCounter / count:f4}/{1.0 * (count - aWinCounter - bWinCounter) / count:f4}/{1.0 * bWinCounter / count:f4}");
}
