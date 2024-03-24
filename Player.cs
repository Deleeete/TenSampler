internal class Player
{
    public string Id { get; }
    public IStrategy Strategy { get; }

    public Player(IStrategy strategy)
    {
        Strategy = strategy;
        Id = $"<{GetHashCode():X8}@{Strategy.Name}>";
    }

    public int GetChoice(Game game)
        => Strategy.GetChoice(game, this);
    public override string ToString()
        => Id;
}
