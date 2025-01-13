using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace DevAndersen.Comparers.Tests;

public class LogicalStringComparerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("abc")]
    [InlineData("abc_0_def")]
    [InlineData("abc_1_def")]
    [InlineData("abc_2_def")]
    public void Compare_SameValue_ReturnsZero(string? value)
    {
        // Arrange
        IComparer<string?> logicalComparer = new LogicalStringComparer(StringComparison.Ordinal);

        // Act
        int result = logicalComparer.Compare(value, value);

        // Assert
        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("abc_123_def")]
    [InlineData("abc_2147483647_def")]
    [InlineData("abc_4294967295_def")]
    [InlineData("abc_9223372036854775807_def")]
    [InlineData("abc_18446744073709551615_def")]
    [InlineData("abc_18446744073709551616_def")]
    public void Compare_ValidAndNull_ReturnsExpected(string? value)
    {
        // Arrange
        IComparer<string?> logicalComparer = new LogicalStringComparer(StringComparison.Ordinal);

        // Act
        int valueAndNull = logicalComparer.Compare(value, null);
        int nullAndValue = logicalComparer.Compare(null, value);

        // Assert
        Assert.Equal(1, valueAndNull);
        Assert.Equal(-1, nullAndValue);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("abc", null)]
    [InlineData("abc", "def")]
    public void Compare_NonNumericString_ReturnsSameAsStringComparer(string? x, string? y)
    {
        // Arrange
        IComparer<string?> logicalComparer = new LogicalStringComparer(StringComparison.Ordinal);
        IComparer<string?> stringComparer = StringComparer.Ordinal;

        // Act
        int logicalComparerResultXAndY = logicalComparer.Compare(x, y);
        int logicalComparerResultYAndX = logicalComparer.Compare(y, x);

        int stringComparerResultXAndY = stringComparer.Compare(x, y);
        int stringComparerResultYAndX = stringComparer.Compare(y, x);

        // Assert
        Assert.Equal(stringComparerResultXAndY, logicalComparerResultXAndY);
        Assert.Equal(stringComparerResultYAndX, logicalComparerResultYAndX);
    }

    [Fact]
    public void Order_LargeDataSet_DoesNotThrow()
    {
        IComparer<string?> logicalComparer = new LogicalStringComparer(StringComparison.Ordinal);

        int count = 1_000_000;
        string[] strings = new string[count];

        Span<byte> hashBuffer = stackalloc byte[MD5.HashSizeInBytes];
        Guid guid;

        for (int i = 0; i < count; i++)
        {
            BinaryPrimitives.WriteInt32BigEndian(hashBuffer, i);

            guid = new Guid(hashBuffer);
            strings[i] = guid.ToString().Replace("-", string.Empty);
        }

        string[] orderedStrings = strings
            .Order(logicalComparer)
            .ToArray();
    }
}
