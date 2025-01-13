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
    private readonly StringComparison _stringComparison;
    private readonly bool _preferNumbersWithoutPrependingZeros;

    public LogicalStringComparer() : this(StringComparison.CurrentCulture, LogicalStringComparerOptions.None)
    {
    }

    public LogicalStringComparer(StringComparison stringComparison) : this(stringComparison, LogicalStringComparerOptions.None)
    {
    }

    public LogicalStringComparer(StringComparison stringComparison, LogicalStringComparerOptions options)
    {
        _stringComparison = stringComparison;
        _preferNumbersWithoutPrependingZeros = options.HasFlag(LogicalStringComparerOptions.PreferNumbersWithoutPrependingZeros);
    }

    public int Compare(string? x, string? y)
    {
        return (x, y) switch
        {
            (not null, not null) when !x.Equals(y) => ComplexCompare(x, y),
            _ => StringComparer.FromComparison(_stringComparison).Compare(x, y)
        };
    }

    private int ComplexCompare(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
    {
        bool isXShortest = x.Length < y.Length;

        // Determine which of the input strings is the longest and shortest.
        ReadOnlySpan<char> shortest = isXShortest ? x : y;
        ReadOnlySpan<char> longest = isXShortest ? y : x;

        int index = 0;

        do
        {
            if (shortest.IsEmpty || longest.IsEmpty)
            {
                int comparerResult = shortest.CompareTo(longest, _stringComparison);

                return isXShortest
                    ? comparerResult
                    : -comparerResult;
            }

            int readChars;

            if (IsDigit(shortest[0]) && IsDigit(longest[0]))
            {
                int numberCompareResult = CompareNumerics(shortest, longest, out readChars);
                if (numberCompareResult != 0)
                {
                    return isXShortest
                        ? numberCompareResult
                        : -numberCompareResult;
                }
            }
            else
            {
                int shortestNonDigitCount = shortest.IndexOfAnyInRange('0', '9');
                int longestNonDigitCount = longest.IndexOfAnyInRange('0', '9');

                // Create a slice of each span that only contains non-digit characters.
                ReadOnlySpan<char> nonDigitsOnlyShortest = shortestNonDigitCount == -1
                    ? shortest
                    : shortest[..shortestNonDigitCount];

                ReadOnlySpan<char> nonDigitsOnlyLongest = longestNonDigitCount == -1
                    ? longest
                    : longest[..longestNonDigitCount];

                int nonDigitComparison = nonDigitsOnlyShortest.CompareTo(nonDigitsOnlyLongest, _stringComparison);

                if (nonDigitComparison != 0)
                {
                    return isXShortest
                        ? nonDigitComparison
                        : -nonDigitComparison;
                }

                readChars = shortestNonDigitCount;
            }

            shortest = shortest[readChars..];
            longest = longest[readChars..];
        }
        while (index < shortest.Length);

        throw new InvalidOperationException();
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
            (_, _) => x.SequenceCompareTo(y), // Neither sequence are parsable, compare their sequences.
        };

        // Determine if both x and y are valid ulongs, and if they compare as being numerically equal.
        // As we already checked if the two sequences are equal, this can only happen due to an uneven number of prepending zeros.
        if (canParseX && canParseY && compareValue == 0)
        {
            readChars = default;

            // Determines if numbers with prepending zeros should be preferred.
            int unevenZeroComparison = x.CompareTo(y, StringComparison.Ordinal);

            return (x.Length > y.Length) != _preferNumbersWithoutPrependingZeros
                ? -unevenZeroComparison
                : unevenZeroComparison;
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
