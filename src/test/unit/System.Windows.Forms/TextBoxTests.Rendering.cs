// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Drawing;
using System.Windows.Forms.Metafiles;

namespace System.Windows.Forms.Tests;

public partial class TextBoxTests
{
    [WinFormsFact]
    public void TextBox_Disabled_PlaceholderText_RendersBackgroundCorrectly()
    {
        // Regression test for https://github.com/dotnet/winforms/issues/3706

        using Form form = new();
        using TextBox textBox = new TextBox
        {
            Size = new Size(80, 23),
            PlaceholderText = "abc",
            Multiline = true,
            Enabled = false
        };
        form.Controls.Add(textBox);

        // Force the handle creation
        Assert.NotEqual(IntPtr.Zero, form.Handle);
        Assert.NotEqual(IntPtr.Zero, textBox.Handle);

        using EmfScope emf = new();
        DeviceContextState state = new(emf);

        textBox.TestAccessor.Dynamic.DrawPlaceholderText(emf.HDC);

        emf.Validate(
            state,
            Validate.TextOut(
                "abc",
                bounds: null,                                   // Don't care about the bounds for this test
                State.BackgroundMode(BACKGROUND_MODE.TRANSPARENT)));
    }

    [WinFormsFact]
    public void TextBox_Multiline_PlaceholderText_UsesFallbackFontAndColorWithoutEllipsis()
    {
        const string Placeholder = "This placeholder is wider than the text box";

        using Form form = new();
        using TextBox textBox = new()
        {
            Size = new Size(40, 40),
            PlaceholderText = Placeholder,
            Multiline = true
        };
        form.Controls.Add(textBox);

        Assert.NotEqual(IntPtr.Zero, form.Handle);
        Assert.NotEqual(IntPtr.Zero, textBox.Handle);

        using EmfScope emf = new();
        DeviceContextState state = new(emf);

        textBox.TestAccessor.Dynamic.DrawPlaceholderText(emf.HDC);

        emf.Validate(
            state,
            Validate.TextOut(
                Placeholder,
                bounds: null,
                State.FontFace(textBox.Font.Name),
                State.TextColor(SystemColors.GrayText),
                State.BackgroundMode(BACKGROUND_MODE.TRANSPARENT)));
    }

    [WinFormsTheory]
    [InlineData(BorderStyle.None)]
    [InlineData(BorderStyle.FixedSingle)]
    [InlineData(BorderStyle.Fixed3D)]
    public unsafe void TextBox_Multiline_PlaceholderText_UsesNativeFormattingRectangle(BorderStyle borderStyle)
    {
        using TextBox textBox = new()
        {
            Size = new Size(200, 80),
            Multiline = true,
            BorderStyle = borderStyle,
            PlaceholderText = "Placeholder"
        };
        Assert.NotEqual(IntPtr.Zero, textBox.Handle);
        RECT rectangle = new(10, 12, 150, 60);
        PInvokeCore.SendMessage(textBox, PInvokeCore.EM_SETRECT, 0, ref rectangle);
        PInvokeCore.SendMessage(textBox, PInvokeCore.EM_GETRECT, 0, ref rectangle);

        using EmfScope emf = new();
        DeviceContextState state = new(emf);

        textBox.TestAccessor.Dynamic.DrawPlaceholderText(emf.HDC);

        emf.Validate(
            state,
            Validate.Action(
                ENHANCED_METAFILE_RECORD_TYPE.EMR_EXTTEXTOUTW,
                (ref EmfRecord record) =>
                {
                    Assert.Equal(new Point(rectangle.left, rectangle.top), record.ExtTextOutWRecord->emrtext.ptlReference);
                    Assert.Equal(textBox.PlaceholderText, record.ExtTextOutWRecord->emrtext.GetText().ToString());

                    return true;
                }));
    }
}
