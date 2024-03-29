using System.Text;

internal class Game(GameChoiceMap Choices, Player playerA, Player playerB)
{
    public GameChoiceMap ChoiceMap { get; } = Choices;
    public Player PlayerA { get; } = playerA;
    public Player PlayerB { get; } = playerB;
    public int ScoreA { get; private set; }
    public int ScoreB { get; private set; }
    public bool IsPlayerALeading => ScoreA > ScoreB;
    public bool IsPlayerBLeading => ScoreB > ScoreA;
    public bool IsDraw => ScoreA == ScoreB;
    public const int GameRounds = 10;
    private const int ChoiceIndex = 0, ScoreIndex = 1, GainIndex = 2;
    private const int PlayerAIndex = 0, PlayerBIndex = 1;
    public int[,,] History { get; } = new int[GameRounds, 2, 3];
    public int CurrentStep { get; private set; } = 0;
    public int CurrentRound { get; private set; } = 0;

    public void Reset()
    {
        History.Initialize();
        CurrentStep = 0;
        CurrentRound = 0;
    }

    public int Choose(int choice)
    {
        if (!ChoiceMap.Probabilities.TryGetValue(choice, out int prob))
            throw new Exception($"Invalid choice [{choice}]");
        if (Random.Shared.Next(100) < prob)
            return choice;
        return 0;
    }

    public int GetScore(Player player)
    {
        if (player == PlayerA)
            return ScoreA;
        else if (player == PlayerB)
            return ScoreB;
        else
            throw new Exception("Player not in game");
    }

    public int GetRivalScore(Player player)
    {
        if (player == PlayerA)
            return ScoreB;
        else if (player == PlayerB)
            return ScoreA;
        else
            throw new Exception("Player not in game");
    }

    public int GetScoreDiff(Player player)
    {
        if (player == PlayerA)
            return ScoreA - ScoreB;
        else if (player == PlayerB)
            return ScoreB - ScoreA;
        else
            throw new Exception("Player not in game");
    }

    public bool IsPlayerLeading(Player player)
    {
        if (player == PlayerA)
            return ScoreA > ScoreB;
        else if (player == PlayerB)
            return ScoreB > ScoreA;
        else
            throw new Exception("Player not in game");
    }

    public void RunOne()
    {
        int choice = PlayerA.GetChoice(this);
        int gain = Choose(choice);
        ScoreA += gain;
        History[CurrentRound, PlayerAIndex, ChoiceIndex] = choice;
        History[CurrentRound, PlayerAIndex, ScoreIndex] = ScoreA;
        History[CurrentRound, PlayerAIndex, GainIndex] = gain;
        CurrentStep++;

        choice = PlayerB.GetChoice(this);
        gain = Choose(choice);
        ScoreB += gain;
        History[CurrentRound, PlayerBIndex, ChoiceIndex] = choice;
        History[CurrentRound, PlayerBIndex, ScoreIndex] = ScoreB;
        History[CurrentRound, PlayerBIndex, GainIndex] = gain;
        CurrentStep++;
        CurrentRound++;
    }

    public void RunToEnd()
    {
        for (int i = 0; i < GameRounds; i++)
        {
            RunOne();
        }
    }

    public string GetHistoryStringA()
    {
        return GetHistoryString(PlayerAIndex);
    }

    public string GetHistoryStringB()
    {
        return GetHistoryString(PlayerBIndex);
    }

    public string GetHistoryString()
    {
        StringBuilder sb = new();
        for (int round = 0; round < CurrentRound; round++)
        {
            int choiceA = History[round, PlayerAIndex, ChoiceIndex];
            int scoreA = History[round, PlayerAIndex, ScoreIndex];
            int gainA = History[round, PlayerAIndex, GainIndex];
            int choiceB = History[round, PlayerBIndex, ChoiceIndex];
            int scoreB = History[round, PlayerBIndex, ScoreIndex];
            int gainB = History[round, PlayerBIndex, GainIndex];
            sb.AppendFormat($"[{round:d2}] ChoiceA={choiceA,-2} GainA={gainA,-2}  ScoreA={scoreA,-2}  | ChoiceB={choiceB,-2}  GainB={gainB,-2}  ScoreB={scoreB,-2}\n");
        }
        return sb.ToString();
    }

    private string GetHistoryString(int playerIndex)
    {
        StringBuilder sb = new();
        for (int round = 0; round < CurrentRound; round++)
        {
            int score = History[playerIndex, ScoreIndex, round];
            int choice = History[playerIndex, ChoiceIndex, round];
            int gain = History[playerIndex, GainIndex, round];
            sb.AppendFormat("[{0}] Choice={2:d2}  Gain={3:d2}  Score={1:d2} \n", round, score, choice, gain);
        }
        return sb.ToString();
    }
}
