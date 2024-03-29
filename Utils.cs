internal static class Utils
{
    public static string ConvertBase(long decimalNumber, int radix)
    {
        const int BitsInLong = 64;
        const string Digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        if (radix < 2 || radix > Digits.Length)
            throw new ArgumentException("The radix must be >= 2 and <= " + Digits.Length.ToString());

        if (decimalNumber == 0)
            return "0";

        int index = BitsInLong - 1;
        long currentNumber = Math.Abs(decimalNumber);
        char[] charArray = new char[BitsInLong];

        while (currentNumber != 0)
        {
            int remainder = (int)(currentNumber % radix);
            charArray[index--] = Digits[remainder];
            currentNumber /= radix;
        }

        int start = index + 1;
        int length = charArray.Length - index - 1;
        if (decimalNumber < 0)
        {
            Array.Copy(charArray, start, charArray, start + 1, length);
            charArray[0] = '-';
            length++;
        }
        return new(charArray, index + 1, length);;
    }
}
