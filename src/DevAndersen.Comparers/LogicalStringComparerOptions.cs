namespace DevAndersen.Comparers;

[Flags]
public enum LogicalStringComparerOptions
{
    None = 0,

    /// <summary>
    /// Specifies if numbers with prepending zeros should be preferred to the same numeric values without prepending zeros.
    /// </summary>
    PreferNumbersWithoutPrependingZeros = 1
}
