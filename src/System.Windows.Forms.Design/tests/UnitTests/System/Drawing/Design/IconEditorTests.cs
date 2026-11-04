// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms.Design;
using System.Windows.Forms.Design.Tests;
using System.Windows.Forms.TestUtilities;
using Moq;

namespace System.Drawing.Design.Tests;

public class IconEditorTests
{
    [Fact]
    public void IconEditor_Ctor_Default()
    {
        IconEditor editor = new();
        Assert.False(editor.IsDropDownResizable);
    }

    public static IEnumerable<object[]> CreateExtensionsString_TestData()
    {
        yield return new object[] { null, ",", null };
        yield return new object[] { Array.Empty<string>(), ",", null };
        yield return new object[] { new string[] { "ico" }, ",", "*.ico" };
        yield return new object[] { new string[] { "a", "b", "c" }, ",", "*.a,*.b,*.c" };
        yield return new object[] { new string[] { "a", "b", "c" }, "", "*.a*.b*.c" };
        yield return new object[] { new string[] { "a", "b", "c" }, null, "*.a*.b*.c" };
        yield return new object[] { new string[] { null, null, null }, ",", "" };
        yield return new object[] { new string[] { string.Empty, string.Empty, string.Empty }, ",", "" };
        yield return new object[] { new string[] { "  ", "\t", " " }, ",", "" };
        yield return new object[] { new string[] { "ico", null, "ico" }, ",", "*.ico,*.ico" };
    }

    [Theory]
    [MemberData(nameof(CreateExtensionsString_TestData))]
    public void IconEditor_CreateExtensionsString_Invoke_ReturnsExpected(string[] extensions, string sep, string expected)
    {
        Assert.Equal(expected, SubIconEditor.CreateExtensionsString(extensions, sep));
    }

    [Fact]
    public void IconEditor_CreateFilterEntry_Invoke_ReturnsExpected()
    {
        SubIconEditor editor = new();
        Assert.Equal("Icon files(*.ico)|*.ico", SubIconEditor.CreateFilterEntry(editor));
    }

    [Fact]
    public void IconEditor_CreateFilterEntry_CustomEditor_ReturnsExpected()
    {
        CustomGetExtensionsIconEditor editor = new();
        Assert.Equal("CustomDescription(*.custom)|*.custom", SubIconEditor.CreateFilterEntry(editor));
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetEditValueInvalidProviderTestData))]
    public void IconEditor_EditValue_InvalidProvider_ReturnsValue(IServiceProvider provider, object value)
    {
        IconEditor editor = new();
        Assert.Same(value, editor.EditValue(null, provider, value));
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void IconEditor_GetEditStyle_Invoke_ReturnsModal(ITypeDescriptorContext context)
    {
        IconEditor editor = new();
        Assert.Equal(UITypeEditorEditStyle.Modal, editor.GetEditStyle(context));
    }

    [Fact]
    public void IconEditor_GetExtensions_InvokeDefault_ReturnsExpected()
    {
        SubIconEditor editor = new();
        string[] extensions = editor.GetExtensions();
        Assert.Equal(new string[] { "ico" }, extensions);
        Assert.NotSame(extensions, editor.GetExtensions());
    }

    [Fact]
    public void IconEditor_GetFileDialogDescription_Invoke_ReturnsExpected()
    {
        SubIconEditor editor = new();
        Assert.Equal("Icon files", editor.GetFileDialogDescription());
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void IconEditor_GetPaintValueSupported_Invoke_ReturnsTrue(ITypeDescriptorContext context)
    {
        IconEditor editor = new();
        Assert.True(editor.GetPaintValueSupported(context));
    }

    [Fact]
    public void IconEditor_LoadFromStream_IconStream_ReturnsExpected()
    {
        SubIconEditor editor = new();
        using FileStream stream = File.OpenRead("Resources/Icon1.ico");
        using Icon result = editor.LoadFromStream(stream);
        Assert.NotNull(result);
        Assert.True(result.Width > 0);
        Assert.True(result.Height > 0);
    }

    [Fact]
    public void IconEditor_LoadFromStream_NullStream_ThrowsArgumentNullException()
    {
        SubIconEditor editor = new();
        Assert.Throws<ArgumentNullException>("stream", () => editor.LoadFromStream(null));
    }

    [Fact]
    public void IconEditor_PaintValue_Invoke_Success()
    {
        IconEditor editor = new();
        using Stream stream = File.OpenRead("Resources/Icon1.ico");
        using Icon icon = new(stream);
        using Bitmap image = new(32, 32);
        using Graphics graphics = Graphics.FromImage(image);
        graphics.Clear(Color.Magenta);
        using Bitmap expected = new(32, 32);
        using Graphics expectedGraphics = Graphics.FromImage(expected);
        expectedGraphics.Clear(Color.Magenta);
        expectedGraphics.DrawIcon(icon, new Rectangle(0, 0, 32, 32));

        PaintValueEventArgs e = new(null, icon, graphics, new Rectangle(0, 0, 32, 32));
        editor.PaintValue(e);
        Assert.Equal(expected.GetPixel(16, 16), image.GetPixel(16, 16));
        Assert.NotEqual(Color.Magenta.ToArgb(), image.GetPixel(16, 16).ToArgb());
    }

    [Fact]
    public void IconEditor_PaintValue_IconSmallerThanBounds_Centers()
    {
        IconEditor editor = new();
        using Stream stream = File.OpenRead("Resources/Icon1.ico");
        using Icon icon = new(stream, new Size(16, 16));
        Assert.Equal(new Size(16, 16), icon.Size);
        using Bitmap image = new(32, 32);
        using Graphics graphics = Graphics.FromImage(image);
        graphics.Clear(Color.Magenta);
        using Bitmap expected = new(32, 32);
        using Graphics expectedGraphics = Graphics.FromImage(expected);
        expectedGraphics.Clear(Color.Magenta);
        expectedGraphics.DrawIcon(icon, new Rectangle(8, 8, 16, 16));

        PaintValueEventArgs e = new(null, icon, graphics, new Rectangle(0, 0, 32, 32));
        editor.PaintValue(e);
        bool painted = false;
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Assert.Equal(expected.GetPixel(x, y), image.GetPixel(x, y));
                painted |= image.GetPixel(x, y).ToArgb() != Color.Magenta.ToArgb();
            }
        }

        Assert.True(painted);
        Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(7, 8).ToArgb());
        Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(24, 8).ToArgb());
    }

    [Fact]
    public void IconEditor_PaintValue_IconLargerThanBounds_NoShrink()
    {
        // Icon larger than bounds: the if-conditions are false, so the bounds are unchanged
        // and DrawIcon will scale the icon down to fit.
        IconEditor editor = new();
        using Stream stream = File.OpenRead("Resources/Icon1.ico");
        using Icon icon = new(stream, new Size(32, 32));
        using Bitmap image = new(8, 8);
        using Graphics graphics = Graphics.FromImage(image);
        graphics.Clear(Color.Magenta);
        using Bitmap expected = new(8, 8);
        using Graphics expectedGraphics = Graphics.FromImage(expected);
        expectedGraphics.Clear(Color.Magenta);
        expectedGraphics.DrawIcon(icon, new Rectangle(0, 0, 4, 4));

        PaintValueEventArgs e = new(null, icon, graphics, new Rectangle(0, 0, 4, 4));
        editor.PaintValue(e);
        bool painted = false;
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Assert.Equal(expected.GetPixel(x, y), image.GetPixel(x, y));
                painted |= image.GetPixel(x, y).ToArgb() != Color.Magenta.ToArgb();
            }
        }

        Assert.True(painted);
    }

    public static IEnumerable<object[]> PaintValue_InvalidArgsValue_TestData()
    {
        yield return new object[] { null };
        yield return new object[] { new() };
        yield return new object[] { "not-an-icon" };
    }

    [Theory]
    [MemberData(nameof(PaintValue_InvalidArgsValue_TestData))]
    public void IconEditor_PaintValue_InvalidArgsValue_Nop(object value)
    {
        IconEditor editor = new();
        using Bitmap image = new(10, 10);
        using Graphics graphics = Graphics.FromImage(image);
        PaintValueEventArgs e = new(null, value, graphics, new Rectangle(1, 2, 3, 4));
        editor.PaintValue(e);
        Assert.Equal(0, image.GetPixel(2, 3).ToArgb());
    }

    [Fact]
    public void IconEditor_PaintValue_NullE_Nop()
    {
        IconEditor editor = new();
        editor.PaintValue(null);
    }

    [WinFormsTheory]
    [InlineData(null)]
    [InlineData("not-an-icon")]
    public void IconEditor_EditValue_Cancel_InitializesAndReusesFileDialog(object value)
    {
        IconEditor editor = new();
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(mockEditorService.Object);

        using EditorDialogTestHost owner = new(() => GetFileDialog(editor));
        Assert.Same(value, editor.EditValue(null, mockServiceProvider.Object, value));
        FileDialog dialog = GetFileDialog(editor);
        Assert.NotNull(dialog);
        Assert.Equal("Icon files(*.ico)|*.ico", dialog.Filter);
        Assert.Same(value, editor.EditValue(null, mockServiceProvider.Object, value));
        Assert.Same(dialog, GetFileDialog(editor));
        Assert.Equal(2, owner.DialogCount);
    }

    [Fact]
    public void IconEditor_HasFileDialogField()
    {
        FieldInfo field = typeof(IconEditor).GetField("_fileDialog",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        Assert.NotNull(field);
        Assert.Equal(typeof(FileDialog), field.FieldType);
    }

    [Fact]
    public void IconEditor_FileDialogField_InitialValue_IsNull()
    {
        // Verifies that a freshly constructed IconEditor has _fileDialog == null,
        // which is required for the lazy initialization in EditValue.
        IconEditor editor = new();
        FieldInfo field = typeof(IconEditor).GetField("_fileDialog",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        object value = field.GetValue(editor);
        Assert.Null(value);
    }

    [Fact]
    public void IconEditor_FileDialogField_AfterInvalidProviderEditValue_RemainsNull()
    {
        // Confirms that the invalid-provider early-return path in EditValue
        // does NOT initialize the _fileDialog field.
        IconEditor editor = new();
        _ = editor.EditValue(null, null, "test");

        FieldInfo field = typeof(IconEditor).GetField("_fileDialog",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        object value = field.GetValue(editor);
        Assert.Null(value);
    }

    private static FileDialog GetFileDialog(IconEditor editor)
    {
        FieldInfo field = typeof(IconEditor).GetField("_fileDialog",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (FileDialog)field.GetValue(editor)!;
    }

    private class SubIconEditor : IconEditor
    {
        public static new string CreateExtensionsString(string[] extensions, string sep)
        {
            return IconEditor.CreateExtensionsString(extensions, sep);
        }

        public static new string CreateFilterEntry(IconEditor e)
        {
            return IconEditor.CreateFilterEntry(e);
        }

        public new string[] GetExtensions() => base.GetExtensions();

        public new string GetFileDialogDescription() => base.GetFileDialogDescription();

        public new Icon LoadFromStream(Stream stream) => base.LoadFromStream(stream);
    }

    private class CustomGetExtensionsIconEditor : IconEditor
    {
        protected override string GetFileDialogDescription() => "CustomDescription";

        protected override string[] GetExtensions() => ["custom"];
    }
}
