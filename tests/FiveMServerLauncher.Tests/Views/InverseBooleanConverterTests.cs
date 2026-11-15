using System.Globalization;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

public class InverseBooleanConverterTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Convert_ShouldNegateTheValue(bool value, bool expected)
    {
        // Given / When
        var result = new InverseBooleanConverter().Convert(value, typeof(bool), null!, CultureInfo.InvariantCulture);

        // Then
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_WhenNotABool_ShouldReturnTrue()
    {
        // Given / When / Then: a missing flag reads as false, so its inverse is true
        Assert.Equal(true, new InverseBooleanConverter().Convert(null!, typeof(bool), null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Converter_ShouldHaveATrueParameterlessConstructor()
    {
        // Given / When / Then
        Assert.NotNull(Activator.CreateInstance(typeof(InverseBooleanConverter)));
    }
}
