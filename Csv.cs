using System.Text;

internal class Csv
{
    private readonly StringBuilder _sb = new();

    public int Cols { get; private set; }

    public Csv(IEnumerable<string> cols, int pad)
    {
        NewRow("\\");
        int colCount = 0;
        foreach (var col in cols)
        {
            AppendElements(col);
            for (int i = 1; i < pad; i++)
                AppendElements(" ");
            colCount++;
        }
        Cols = colCount * pad;
    }
    public Csv(IEnumerable<string> cols)
    {
        NewRow("\\");
        Cols = AppendElements(cols);
    }

    public int AppendElements(IEnumerable<string> cols)
    {
        int colCount = 0;
        foreach (var col in cols)
        {
            _sb.Append(col);
            _sb.Append(',');
        }
        return colCount;
    }

    public void AppendElements(params string[] cols)
    {
        foreach (var col in cols)
        {
            _sb.Append(col);
            _sb.Append(',');
        }
    }

    public void NewRow(string head)
    {
        Trim();
        _sb.Append(head);
        _sb.Append(',');
    }

    public void Trim()
    {
        if (_sb.Length != 0 && _sb[^1] == ',')
        {
            _sb.Length--;
            _sb.AppendLine();
        }
    }

    public override string ToString()
    {
        Trim();
        return _sb.ToString().Trim();
    }
}
