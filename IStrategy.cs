internal interface IStrategy
{
    string Name { get; }
    int GetChoice(Game game, Player currentPlayer);
}
