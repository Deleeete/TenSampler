internal class ProgressControl(int value, int Max)
{
    private int _value = value;

    public int Value => _value;
    public bool End => Value == Max;

    public void Increment()
    {
        Interlocked.Increment(ref _value);
    }
    public void EndNow()
    {
        Interlocked.Exchange(ref _value, Max);
    }
    public void Show()
    {
        _ = Task.Run(() =>
        {
            const int ProgressPrintWidth = 8;
            string progressEraser = new(' ', ProgressPrintWidth);
            while (!End)
            {
                int cursorLeft = Console.CursorLeft;
                Console.Write($"{Convert.ToDouble(Value) / Max * 100:f3}%".PadLeft(ProgressPrintWidth));
                Thread.Sleep(1000);
                Console.CursorLeft = cursorLeft;
                Console.Write(progressEraser);
                Console.CursorLeft = cursorLeft;
            }
            Console.WriteLine();
        });
    }
}
