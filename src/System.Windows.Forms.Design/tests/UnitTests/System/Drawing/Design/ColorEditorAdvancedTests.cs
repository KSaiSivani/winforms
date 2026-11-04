// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Windows.Forms.Design;
using System.Windows.Forms.TestUtilities;
using Moq;

namespace System.Drawing.Design.Tests;

/// <summary>
/// Additional comprehensive tests for ColorEditor edge cases and scenarios
/// Ensures maximum code coverage
/// </summary>
public class ColorEditorAdvancedTests
{
    #region TypeDescriptorContext Tests

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void ColorEditor_GetEditStyle_WithVariousContexts_ReturnsConsistent(ITypeDescriptorContext context)
    {
        ColorEditor editor1 = new();
        ColorEditor editor2 = new();

        UITypeEditorEditStyle style1 = editor1.GetEditStyle(context);
        UITypeEditorEditStyle style2 = editor2.GetEditStyle(context);

        Assert.Equal(style1, style2);
        Assert.Equal(UITypeEditorEditStyle.DropDown, style1);
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void ColorEditor_GetPaintValueSupported_WithVariousContexts_AlwaysReturnsTrue(ITypeDescriptorContext context)
    {
        ColorEditor editor = new();

        bool supported = editor.GetPaintValueSupported(context);

        Assert.True(supported);
    }

    #endregion

    #region Color Value Tests

    public static IEnumerable<object[]> PaintValue_VariousColors_TestData()
    {
        // Predefined colors
        yield return new object[] { Color.Red };
        yield return new object[] { Color.Green };
        yield return new object[] { Color.Blue };
        yield return new object[] { Color.Yellow };
        yield return new object[] { Color.Cyan };
        yield return new object[] { Color.Magenta };
        yield return new object[] { Color.White };
        yield return new object[] { Color.Black };
        yield return new object[] { Color.Gray };
        yield return new object[] { Color.LightGray };
        yield return new object[] { Color.DarkGray };

        // System colors
        yield return new object[] { SystemColors.ActiveCaption };
        yield return new object[] { SystemColors.ActiveBorder };
        yield return new object[] { SystemColors.Control };
        yield return new object[] { SystemColors.ControlDark };
        yield return new object[] { SystemColors.ControlLight };

        // Custom colors with specific ARGB values
        yield return new object[] { Color.FromArgb(128, 64, 32) };
        yield return new object[] { Color.FromArgb(255, 192, 203) };

        // Transparency tests
        yield return new object[] { Color.FromArgb(0, 255, 0, 0) };
        yield return new object[] { Color.FromArgb(128, 255, 0, 0) };
        yield return new object[] { Color.FromArgb(255, 0, 0, 0) };
    }

    [Theory]
    [MemberData(nameof(PaintValue_VariousColors_TestData))]
    public void ColorEditor_PaintValue_WithVariousColors_Succeeds(Color color)
    {
        ColorEditor editor = new();
        using Bitmap bitmap = new(20, 20);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Lime);
        using Bitmap expected = new(20, 20);
        using Graphics expectedGraphics = Graphics.FromImage(expected);
        expectedGraphics.Clear(Color.Lime);
        using SolidBrush brush = new(color);
        expectedGraphics.FillRectangle(brush, new Rectangle(2, 2, 16, 16));
        var paintEventArgs = new PaintValueEventArgs(null, color, graphics, new Rectangle(2, 2, 16, 16));

        editor.PaintValue(paintEventArgs);
        Assert.Equal(expected.GetPixel(10, 10), bitmap.GetPixel(10, 10));
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(1, 1).ToArgb());
    }

    #endregion

    #region Rectangle Bounds Tests

    public static IEnumerable<object[]> PaintValue_VariousRectangles_TestData()
    {
        // Small rectangles
        yield return new object[] { new Rectangle(0, 0, 1, 1) };
        yield return new object[] { new Rectangle(0, 0, 5, 5) };

        // Normal rectangles
        yield return new object[] { new Rectangle(0, 0, 16, 16) };
        yield return new object[] { new Rectangle(0, 0, 32, 32) };

        // Large rectangles
        yield return new object[] { new Rectangle(0, 0, 100, 100) };
        yield return new object[] { new Rectangle(0, 0, 256, 256) };

        // Offset rectangles
        yield return new object[] { new Rectangle(10, 10, 20, 20) };
        yield return new object[] { new Rectangle(50, 50, 30, 30) };

        // Wide rectangles
        yield return new object[] { new Rectangle(0, 0, 100, 10) };

        // Tall rectangles
        yield return new object[] { new Rectangle(0, 0, 10, 100) };
    }

    [Theory]
    [MemberData(nameof(PaintValue_VariousRectangles_TestData))]
    public void ColorEditor_PaintValue_WithVariousRectangles_Succeeds(Rectangle rectangle)
    {
        ColorEditor editor = new();
        using Bitmap bitmap = new(300, 300);
        using Graphics graphics = Graphics.FromImage(bitmap);

        var paintEventArgs = new PaintValueEventArgs(null, Color.Red, graphics, rectangle);

        editor.PaintValue(paintEventArgs);
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(rectangle.Left, rectangle.Top).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(rectangle.Right - 1, rectangle.Bottom - 1).ToArgb());
        Assert.Equal(0, bitmap.GetPixel(rectangle.Right, rectangle.Bottom).ToArgb());
    }

    #endregion

    #region Multiple Instances Tests

    [Fact]
    public void ColorEditor_MultipleInstances_AreIndependent()
    {
        ColorEditor editor1 = new();
        ColorEditor editor2 = new();
        ColorEditor editor3 = new();

        Assert.NotSame(editor1, editor2);
        Assert.NotSame(editor2, editor3);
        Assert.NotSame(editor1, editor3);

        Assert.Equal(UITypeEditorEditStyle.DropDown, editor1.GetEditStyle(null));
        Assert.Equal(UITypeEditorEditStyle.DropDown, editor2.GetEditStyle(null));
        Assert.Equal(UITypeEditorEditStyle.DropDown, editor3.GetEditStyle(null));
    }

    #endregion

    #region Service Provider Edge Cases

    [Fact]
    public void ColorEditor_EditValue_WithNullEditorService_ReturnsOriginal()
    {
        ColorEditor editor = new();
        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);

        mockServiceProvider
            .Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(null)
            .Verifiable();

        Color inputColor = Color.Red;
        object result = editor.EditValue(null, mockServiceProvider.Object, inputColor);

        Assert.Equal(inputColor, result);
        mockServiceProvider.Verify(p => p.GetService(typeof(IWindowsFormsEditorService)), Times.Once());
    }

    [Fact]
    public void ColorEditor_EditValue_WithDifferentValueTypes_AllReturned()
    {
        ColorEditor editor = new();
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);

        mockServiceProvider
            .Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(mockEditorService.Object)
            .Verifiable();
        mockEditorService
            .Setup(e => e.DropDownControl(It.IsAny<Control>()))
            .Verifiable();

        object[] testValues = [null, Color.Red, "string", 123, new object()];

        foreach (object testValue in testValues)
        {
            object result = editor.EditValue(null, mockServiceProvider.Object, testValue);
            Assert.Equal(testValue, result);
        }
    }

    #endregion

    #region IsDropDownResizable Tests

    [Fact]
    public void ColorEditor_IsDropDownResizable_IsFalse()
    {
        ColorEditor editor = new();

        Assert.False(editor.IsDropDownResizable);
    }

    [Fact]
    public void ColorEditor_IsDropDownResizable_MultipleInstances_Consistent()
    {
        ColorEditor editor1 = new();
        ColorEditor editor2 = new();

        Assert.False(editor1.IsDropDownResizable);
        Assert.False(editor2.IsDropDownResizable);
        Assert.Equal(editor1.IsDropDownResizable, editor2.IsDropDownResizable);
    }

    #endregion

    #region Paint Value Event Args Tests

    [Fact]
    public void ColorEditor_PaintValue_WithCustomContext_Succeeds()
    {
        ColorEditor editor = new();
        Mock<ITypeDescriptorContext> mockContext = new();

        using Bitmap bitmap = new(10, 10);
        using Graphics graphics = Graphics.FromImage(bitmap);

        var paintEventArgs = new PaintValueEventArgs(mockContext.Object, Color.Red, graphics, new Rectangle(0, 0, 10, 10));

        // Should not throw
        editor.PaintValue(paintEventArgs);
    }

    [Fact]
    public void ColorEditor_PaintValue_RepeatedCalls_AllSucceed()
    {
        ColorEditor editor = new();
        using Bitmap bitmap = new(10, 10);
        using Graphics graphics = Graphics.FromImage(bitmap);

        // Call PaintValue multiple times
        for (int i = 0; i < 10; i++)
        {
            var paintEventArgs = new PaintValueEventArgs(null, Color.Red, graphics, new Rectangle(0, 0, 10, 10));
            editor.PaintValue(paintEventArgs);
        }
    }

    #endregion

    #region Edit Value Additional Tests

    [Fact]
    public void ColorEditor_EditValue_SequentialCalls_Consistent()
    {
        ColorEditor editor = new();
        Mock<IWindowsFormsEditorService> mockEditorService1 = new(MockBehavior.Strict);
        Mock<IServiceProvider> mockServiceProvider1 = new(MockBehavior.Strict);

        mockServiceProvider1
            .Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(mockEditorService1.Object);
        mockEditorService1
            .Setup(e => e.DropDownControl(It.IsAny<Control>()));

        Color testColor = Color.Red;

        // First call
        object result1 = editor.EditValue(null, mockServiceProvider1.Object, testColor);
        Assert.Equal(testColor, result1);

        // Second call with same editor
        object result2 = editor.EditValue(null, mockServiceProvider1.Object, testColor);
        Assert.Equal(testColor, result2);

        // Results should be equal
        Assert.Equal(result1, result2);
    }

    #endregion

    #region CLSCompliant Tests

    [Fact]
    public void ColorEditor_IsCLSCompliant_False()
    {
        Type colorEditorType = typeof(ColorEditor);
        CLSCompliantAttribute clsCompliantAttr = colorEditorType.GetCustomAttributes(typeof(CLSCompliantAttribute), false)
            .FirstOrDefault() as CLSCompliantAttribute;

        if (clsCompliantAttr is not null)
        {
            Assert.False(clsCompliantAttr.IsCompliant);
        }
    }

    #endregion
}
