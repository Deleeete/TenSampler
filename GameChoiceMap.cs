using System.Collections.ObjectModel;

internal class GameChoiceMap
{
    public ReadOnlyDictionary<int, int> Probabilities;
    public int[] Choices { get; }

    public GameChoiceMap(IDictionary<int, int> probabilities)
    {
        Probabilities = new(probabilities);
        Choices = [.. probabilities.Keys];
        Array.Sort(Choices);
    }
}
