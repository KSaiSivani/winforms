// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Windows.Forms.Design;
using System.Windows.Forms.Design.Tests;
using System.Windows.Forms.TestUtilities;
using Moq;

namespace System.Drawing.Design.Tests;

public class FontEditorTests
{
    [Fact]
    public void FontEditor_Ctor_Default()
    {
        FontEditor editor = new();
        Assert.False(editor.IsDropDownResizable);
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetEditValueInvalidProviderTestData))]
    public void FontEditor_EditValue_InvalidProvider_ReturnsValue(IServiceProvider provider, object value)
    {
        FontEditor editor = new();
        Assert.Same(value, editor.EditValue(null, provider, value));
        Assert.Null(editor.TestAccessor.Dynamic._fontDialog);
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void FontEditor_GetEditStyle_Invoke_ReturnsModal(ITypeDescriptorContext context)
    {
        FontEditor editor = new();
        Assert.Equal(UITypeEditorEditStyle.Modal, editor.GetEditStyle(context));
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void FontEditor_GetPaintValueSupported_Invoke_ReturnsFalse(ITypeDescriptorContext context)
    {
        FontEditor editor = new();
        Assert.False(editor.GetPaintValueSupported(context));
    }

    [WinFormsTheory]
    [InlineData(null)]
    [InlineData("not-a-font")]
    public void FontEditor_EditValue_Cancel_InitializesAndReusesFontDialog(object value)
        => VerifyCanceledEdit(value);

    [WinFormsFact]
    public void FontEditor_EditValue_Font_Cancel_PreservesValue()
    {
        using Font font = new("Arial", 12);
        VerifyCanceledEdit(font);
    }

    private static void VerifyCanceledEdit(object value)
    {
        FontEditor editor = new();
        Mock<IWindowsFormsEditorService> service = new(MockBehavior.Strict);
        Mock<IServiceProvider> provider = new(MockBehavior.Strict);
        provider.Setup(p => p.GetService(typeof(IWindowsFormsEditorService))).Returns(service.Object);
        using EditorDialogTestHost owner = new(() => (FontDialog)editor.TestAccessor.Dynamic._fontDialog);
        Assert.Same(value, editor.EditValue(null, provider.Object, value));
        FontDialog dialog = editor.TestAccessor.Dynamic._fontDialog;
        Assert.NotNull(dialog);
        Assert.False(dialog.ShowApply);
        Assert.False(dialog.ShowColor);
        Assert.False(dialog.AllowVerticalFonts);
        if (value is Font font)
        {
            Assert.Equal(font, dialog.Font);
        }

        Assert.Same(value, editor.EditValue(null, provider.Object, value));
        Assert.Same(dialog, (FontDialog)editor.TestAccessor.Dynamic._fontDialog);
        Assert.Equal(2, owner.DialogCount);
    }
}
