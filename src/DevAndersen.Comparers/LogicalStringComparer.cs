namespace DevAndersen.Comparers;

/// <summary>
/// An implementation of <see cref="IComparer{string}"/>, which performs a logical comparison that considers sequences of digits as numeric values, and sorts them accordingly.
/// This is intended to be an approximation of the behavior of the <see href="https://learn.microsoft.com/en-us/windows/win32/api/shlwapi/nf-shlwapi-strcmplogicalw">StrCmpLogicalW</see> WinAPI function in regards of sorting numbers.
/// </summary>
/// <remarks>
/// This method performs a case insensitive comparison.
/// </remarks>
public class LogicalStringComparer : IComparer<string?>
{
    private readonly bool _preferNumbersWithoutPrependingZeros;

    public LogicalStringComparer() : this(LogicalStringComparerOptions.None)
    {
    }

    public LogicalStringComparer(LogicalStringComparerOptions options)
    {
        _preferNumbersWithoutPrependingZeros = options.HasFlag(LogicalStringComparerOptions.PreferNumbersWithoutPrependingZeros);
    }

    public int Compare(string? x, string? y)
    {
        return (x, y) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            _ when x == y => 0,
            _ => ComplexCompare(x, y)
        };
    }

    private int ComplexCompare(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
    {
        bool isXShortest = x.Length < y.Length;

        // Determine which of the input strings is the longest and shortest.
        ReadOnlySpan<char> shortest = isXShortest ? x : y;
        ReadOnlySpan<char> longest = isXShortest ? y : x;

        do
        {
            char shortChar = shortest[0];
            char longChar = longest[0];

            // If the first char of both spans are digits, compare the spans as numbers.
            if (IsDigit(shortChar) && IsDigit(longChar))
            {
                int numberCompareResult = CompareNumerics(shortest, longest, out int readChars);
                if (numberCompareResult != 0)
                {
                    return isXShortest
                        ? numberCompareResult
                        : -numberCompareResult;
                }

                shortest = shortest[readChars..];
                longest = longest[readChars..];
            }
            else
            {
                if (shortChar != longChar)
                {
                    char shortLower = char.ToLowerInvariant(shortChar);
                    char longLower = char.ToLowerInvariant(longChar);

                    // Perform a case insensitive comparison.
                    int charCompareResult = shortLower.CompareTo(longLower);
                    if (charCompareResult != 0)
                    {
                        return isXShortest
                            ? charCompareResult
                            : -charCompareResult;
                    }

                    // Slice the spans by one char.
                    shortest = shortest[1..];
                    longest = longest[1..];
                }
                // Slice the spans by one char.
                shortest = shortest[1..];
                longest = longest[1..];
            }
        }
        while (shortest.Length > 0);

        // The longest string begins with the exact same sequence as all of the shortest string.
        return isXShortest
            ? -1
            : 1;
    }

    private int CompareNumerics(ReadOnlySpan<char> x, ReadOnlySpan<char> y, out int readChars)
    {
        // Determine how far into each span can be looked before a non-digit character is found.
        int xDigitCount = x.IndexOfAnyExceptInRange('0', '9');
        int yDigitCount = y.IndexOfAnyExceptInRange('0', '9');

        // Create a slice of each span that only contains digits.
        ReadOnlySpan<char> digitsOnlyX = xDigitCount == -1
            ? x
            : x[..xDigitCount];

        ReadOnlySpan<char> digitsOnlyY = yDigitCount == -1
            ? y
            : y[..yDigitCount];

        // If the sequences are identical, return 0.
        if (digitsOnlyX.SequenceEqual(digitsOnlyY))
        {
            readChars = xDigitCount;
            return 0;
        }

        // Attempt to parse each digit-only slice as a ulong.
        bool canParseX = ulong.TryParse(digitsOnlyX, out ulong xValue);
        bool canParseY = ulong.TryParse(digitsOnlyY, out ulong yValue);

        // Compare the parsability of x and y, and if both are parsable, compare their values.
        int compareValue = (canParseX, canParseY) switch
        {
            (true, true) => xValue.CompareTo(yValue), // Both sequences are parsable, compare their parsed values.
            (false, false) => x.SequenceCompareTo(y), // Neither sequence are parsable, compare their sequences.
            (true, false) => -1,
            (false, true) => 1
        };

        // Determine if both x and y are valid ulongs, and if they compare as being numerically equal.
        // As we already checked if the two sequences are equal, this can only happen due to an uneven number of prepending zeros.
        if (canParseX && canParseY && compareValue == 0)
        {
            readChars = default;

            // Determines if numbers with prepending zeros should be preferred.
            return (x.Length > y.Length) != _preferNumbersWithoutPrependingZeros
                ? -1
                : 1;
        }

        readChars = xDigitCount;
        return compareValue;
    }

    /// <summary>
    /// Determines if <paramref name="c"/> is a digit from 0 to 9.
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    private static bool IsDigit(char c) => c >= '0' && c <= '9';
}
