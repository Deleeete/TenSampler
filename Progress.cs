using System.Diagnostics;

internal class ProgressControl(int value, int Max)
{
    private int _value = value;
    private readonly Stopwatch _watch = new();
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
            const int ProgressPrintWidth = 42;
            string progressEraser = new(' ', ProgressPrintWidth);
            double speed = 0.01;
            _watch.Restart();
            while (!End)
            {
                int valA = Value;
                long msA = _watch.ElapsedMilliseconds;
                double percentage = Convert.ToDouble(Value) / Max * 100;
                double etaMs = (Max - Value) / speed;
                TimeSpan eta = TimeSpan.FromMilliseconds(etaMs);
                Console.CursorLeft = 0;
                Console.Write(progressEraser);
                Console.CursorLeft = 0;
                Console.Write($"{percentage:f4}%\t{speed * 1000:f2} it/ms\tETA: {eta:dd\\.hh\\:mm\\:ss}".PadLeft(ProgressPrintWidth));
                Thread.Sleep(3000);
                int valB = Value;
                long msB = _watch.ElapsedMilliseconds;
                int dVal = valB - valA;
                speed = dVal == 0 ? 0.01 : Convert.ToDouble(dVal) / (msB - msA);
            }
            Console.WriteLine();
        });
    }
}
