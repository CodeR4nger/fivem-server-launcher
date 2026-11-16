using System.Globalization;
using System.Windows.Data;
using FiveMServerLauncher.ViewModels;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

// One radio chip per enum value: checked when the bound value equals the chip's parameter,
// and checking a chip writes its parameter back.
public class EnumEqualityConverterTests
{
    private static readonly EnumEqualityConverter Converter = new();

    [Theory]
    [InlineData(SavedServerFilter.Online, true)]
    [InlineData(SavedServerFilter.All, false)]
    public void Convert_ShouldBeTrueOnlyForTheParameter(SavedServerFilter value, bool expected)
    {
        // Given / When / Then
        Assert.Equal(expected, Converter.Convert(value, typeof(bool?), SavedServerFilter.Online, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ConvertBack_WhenChecked_ShouldWriteTheParameter()
    {
        // Given / When / Then
        Assert.Equal(SavedServerFilter.Offline,
            Converter.ConvertBack(true, typeof(SavedServerFilter), SavedServerFilter.Offline, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ConvertBack_WhenUnchecked_ShouldWriteNothing()
    {
        // Given / When / Then — the newly checked chip writes; the unchecked one must not
        Assert.Same(Binding.DoNothing,
            Converter.ConvertBack(false, typeof(SavedServerFilter), SavedServerFilter.Offline, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Ctor_ShouldBeTrulyParameterless()
    {
        // Given / When / Then: XAML-instantiated
        Assert.NotNull(Activator.CreateInstance(typeof(EnumEqualityConverter)));
    }
}
