// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms.TestUtilities;
using Moq;

namespace System.Windows.Forms.Design.Tests;

public class FileNameEditorTests
{
    [Fact]
    public void FileNameEditor_Ctor_Default()
    {
        FileNameEditor editor = new();
        Assert.False(editor.IsDropDownResizable);
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetEditValueInvalidProviderTestData))]
    public void FileNameEditor_EditValue_InvalidProvider_ReturnsValue(IServiceProvider provider, object value)
    {
        FileNameEditor editor = new();
        Assert.Same(value, editor.EditValue(null, provider, value));
        Assert.Null((OpenFileDialog)editor.TestAccessor.Dynamic._openFileDialog);
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void FileNameEditor_GetEditStyle_Invoke_ReturnsModal(ITypeDescriptorContext context)
    {
        FileNameEditor editor = new();
        Assert.Equal(UITypeEditorEditStyle.Modal, editor.GetEditStyle(context));
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void FileNameEditor_GetPaintValueSupported_Invoke_ReturnsFalse(ITypeDescriptorContext context)
    {
        FileNameEditor editor = new();
        Assert.False(editor.GetPaintValueSupported(context));
    }

    [Fact]
    public void FileNameEditor_InitializeDialog_Invoke_Success()
    {
        SubFileNameEditor editor = new();
        using OpenFileDialog dialog = new();
        editor.InitializeDialog(dialog);
        Assert.Equal(SR.GenericFileFilter, dialog.Filter);
        Assert.Equal(SR.GenericOpenFile, dialog.Title);
    }

    [Fact]
    public void FileNameEditor_InitializeDialog_NullOpenFileDialog_ThrowsArgumentNullException()
    {
        SubFileNameEditor editor = new();
        Assert.Throws<ArgumentNullException>("openFileDialog", () => editor.InitializeDialog(null));
    }

    [WinFormsTheory]
    [InlineData(null)]
    [InlineData("initialFile.txt")]
    [InlineData(42)]
    public void FileNameEditor_EditValue_Cancel_ReturnsOriginalValue(object value)
    {
        FileNameEditor editor = new();
        using EditorDialogTestHost host = new(() => GetDialog(editor));

        object result = editor.EditValue(null, CreateProvider(), value);

        Assert.Same(value, result);
        Assert.Equal(1, host.DialogCount);
        OpenFileDialog dialog = GetDialog(editor);
        Assert.Equal(value as string ?? string.Empty, dialog.FileName);
        Assert.Equal(SR.GenericFileFilter, dialog.Filter);
        Assert.Equal(SR.GenericOpenFile, dialog.Title);
    }

    [WinFormsFact]
    public void FileNameEditor_EditValue_CalledTwice_ReusesAndConfiguresDialog()
    {
        FileNameEditor editor = new();
        IServiceProvider provider = CreateProvider();
        using EditorDialogTestHost host = new(() => GetDialog(editor));

        Assert.Equal("first.txt", editor.EditValue(null, provider, "first.txt"));
        OpenFileDialog firstDialog = GetDialog(editor);
        Assert.Equal("first.txt", firstDialog.FileName);

        Assert.Equal("second.txt", editor.EditValue(null, provider, "second.txt"));

        Assert.Same(firstDialog, GetDialog(editor));
        Assert.Equal("second.txt", firstDialog.FileName);
        Assert.Equal(2, host.DialogCount);
    }

    [WinFormsFact]
    public void FileNameEditor_EditValue_Accept_ReturnsSelectedFile()
    {
        using TempFile file = TempFile.Create(length: 0);
        FileNameEditor editor = new();
        using EditorDialogTestHost host = new(
            () =>
            {
                OpenFileDialog dialog = GetDialog(editor);
                dialog.InitialDirectory = Path.GetDirectoryName(file.Path);
                dialog.FileName = file.Path;
                return dialog;
            },
            accept: true);

        object result = editor.EditValue(null, CreateProvider(), "original.txt");

        Assert.Equal(file.Path, result);
        Assert.Equal(1, host.DialogCount);
    }

    [Fact]
    public void FileNameEditor_InitializeDialog_Override_AppliesCustomFilterAndTitle()
    {
        CustomInitializeDialogFileNameEditor editor = new();
        using OpenFileDialog dialog = new();

        editor.InvokeInitializeDialog(dialog);

        Assert.Equal("Custom Filter|*.custom", dialog.Filter);
        Assert.Equal("Custom Title", dialog.Title);
    }

    private static OpenFileDialog GetDialog(FileNameEditor editor)
        => (OpenFileDialog)editor.TestAccessor.Dynamic._openFileDialog;

    private static IServiceProvider CreateProvider()
    {
        Mock<IServiceProvider> provider = new(MockBehavior.Strict);
        provider.Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(new Mock<IWindowsFormsEditorService>(MockBehavior.Strict).Object);
        return provider.Object;
    }

    /// <summary>
    ///  Exposes the production dialog initializer.
    /// </summary>
    private class SubFileNameEditor : FileNameEditor
    {
        public new void InitializeDialog(OpenFileDialog dialog)
            => base.InitializeDialog(dialog);
    }

    /// <summary>
    ///  Supplies a derived editor's dialog configuration.
    /// </summary>
    private class CustomInitializeDialogFileNameEditor : FileNameEditor
    {
        public void InvokeInitializeDialog(OpenFileDialog dialog)
            => InitializeDialog(dialog);

        protected override void InitializeDialog(OpenFileDialog dialog)
        {
            dialog.Filter = "Custom Filter|*.custom";
            dialog.Title = "Custom Title";
        }
    }
}
