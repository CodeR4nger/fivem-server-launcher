using System.Globalization;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

public class DropdownOpenStateConverterTests
{
    private static bool Convert(params object?[] values)
    {
        return (bool)new DropdownOpenStateConverter().Convert(
            values, typeof(bool), null!, CultureInfo.InvariantCulture);
    }

    [Fact]
    public void Convert_WhenDropDownOpen_ShouldKeepPopupOpen()
    {
        // Given the combo is open
        // When the state is evaluated
        // Then the popup exists
        Assert.True(Convert(true, 0.0));
    }

    [Fact]
    public void Convert_WhenClosedButHolding_ShouldKeepPopupOpenForTheCollapse()
    {
        // Given the combo requested close but the collapse is still animating
        // When the state is evaluated
        // Then the popup still exists so the motion can render
        Assert.True(Convert(false, 1.0));
    }

    [Fact]
    public void Convert_WhenClosedAndNotHolding_ShouldHidePopup()
    {
        // Given the collapse finished
        // When the state is evaluated
        // Then the popup is gone
        Assert.False(Convert(false, 0.0));
    }

    [Fact]
    public void Convert_WhenInputsAreMalformed_ShouldHidePopup()
    {
        // Given missing or mistyped values
        // When the state is evaluated
        // Then the popup never lingers open on bad data
        Assert.False(Convert());
        Assert.False(Convert(null, null));
        Assert.False(Convert("true", "1"));
    }

    [Fact]
    public void ConvertBack_ShouldThrowBecauseTheBindingIsOneWay()
    {
        // Given the one-way Popup.IsOpen binding
        // When something tries to convert back
        // Then it fails loudly instead of corrupting state
        Assert.Throws<NotSupportedException>(() =>
            new DropdownOpenStateConverter().ConvertBack(
                false, new[] { typeof(bool), typeof(double) }, null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Converter_WhenInstantiatedByXaml_ShouldHaveParameterlessCtor()
    {
        // Given XAML instantiates converters through a parameterless ctor
        // When the type is created via Activator
        // Then it succeeds (BAML MissingMethodException regression pin)
        Assert.NotNull(Activator.CreateInstance(typeof(DropdownOpenStateConverter)));
    }
}
