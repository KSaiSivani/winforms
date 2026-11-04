// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Imaging;
using System.Windows.Forms.TestUtilities;
using Moq;

namespace System.Windows.Forms.Design.Tests;

public class ImageListImageEditorTests
{
    [Fact]
    public void ImageListImageEditor_Ctor_Default()
    {
        ImageListImageEditor editor = new();
        Assert.False(editor.IsDropDownResizable);
    }

    [Theory]
    [BoolData]
    public void ImageCollectionEditor_GetObjectsFromInstance_PreservesImageList(bool useGenericList)
    {
        ImageCollectionEditor editor = new(typeof(ImageList.ImageCollection));
        var getObjectsFromInstance = editor.TestAccessor.CreateDelegate<Func<object, IList>>("GetObjectsFromInstance");
        using Bitmap bitmap = new(10, 10);
        ImageListImage image = new(bitmap);
        IList images = useGenericList ? new List<ImageListImage> { image } : new ArrayList { image };

        Assert.Same(images, getObjectsFromInstance(images));
        Assert.Same(image, Assert.Single(images));
    }

    [Fact]
    public void ImageListImageEditor_LoadImageFromStream_BitmapStream_ReturnsExpected()
    {
        ImageListImageEditor editor = new();
        var loadImage = editor.TestAccessor.CreateDelegate<Func<Stream, bool, ImageListImage>>("LoadImageFromStream");
        using MemoryStream stream = new();
        using Bitmap source = new(10, 10);
        source.SetPixel(4, 4, Color.Red);
        source.Save(stream, ImageFormat.Bmp);
        stream.Position = 0;

        ImageListImage result = loadImage(stream, false);
        using Image image = result.Image;

        Assert.Equal(source.Size, result.Size);
        Assert.Equal(Color.Red.ToArgb(), ((Bitmap)image).GetPixel(4, 4).ToArgb());
    }

    [Fact]
    public void ImageListImageEditor_GetImageExtenders_Invoke_ReturnsExpected()
    {
        SubImageListImageEditor editor = new();
        Type[] extenders = editor.GetImageExtenders();
        Assert.Equal([typeof(BitmapEditor)], extenders);
        Assert.Same(extenders, editor.GetImageExtenders());
    }

    [Fact]
    public void ImageListImageEditor_GetFileDialogDescription_Invoke_ReturnsExpected()
    {
        SubImageListImageEditor editor = new();
        Assert.Equal("All image files", editor.GetFileDialogDescription());
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void ImageListImageEditor_GetPaintValueSupported_Invoke_ReturnsTrue(ITypeDescriptorContext context)
    {
        Assert.True(new ImageListImageEditor().GetPaintValueSupported(context));
    }

    [Theory]
    [BoolData]
    public void ImageListImageEditor_PaintValue_PaintsInnerImage(bool wrapImage)
    {
        ImageListImageEditor editor = new();
        using Bitmap image = new(10, 10);
        using (Graphics imageGraphics = Graphics.FromImage(image))
        {
            imageGraphics.Clear(Color.Red);
        }

        using Bitmap surface = new(20, 20);
        using Graphics graphics = Graphics.FromImage(surface);
        graphics.Clear(Color.Lime);
        object value = wrapImage ? new ImageListImage(image) : image;

        editor.PaintValue(new PaintValueEventArgs(null, value, graphics, new Rectangle(2, 2, 10, 10)));

        Assert.Equal(Color.Red.ToArgb(), surface.GetPixel(6, 6).ToArgb());
        Assert.Equal(Color.Lime.ToArgb(), surface.GetPixel(0, 0).ToArgb());
    }

    [Fact]
    public void ImageListImageEditor_EditValue_NullProvider_ReturnsValue()
    {
        ImageListImageEditor editor = new();
        object value = new();
        Assert.Same(value, editor.EditValue(null, null, value));
        Assert.Null(GetDialog(editor));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not an editor service")]
    public void ImageListImageEditor_EditValue_InvalidEditorService_ReturnsEmptyList(object service)
    {
        ImageListImageEditor editor = new();
        Mock<IServiceProvider> provider = new(MockBehavior.Strict);
        provider.Setup(p => p.GetService(typeof(IWindowsFormsEditorService))).Returns(service);

        Assert.Empty(Assert.IsType<ArrayList>(editor.EditValue(null, provider.Object, null)));
        Assert.Null(GetDialog(editor));
    }

    [WinFormsTheory]
    [InlineData(null)]
    [InlineData("original value")]
    public void ImageListImageEditor_EditValue_Cancel_InitializesAndReusesDialog(object value)
    {
        ImageListImageEditor editor = new();
        IServiceProvider provider = CreateProvider();
        using EditorDialogTestHost host = new(() => GetDialog(editor));

        Assert.Empty(Assert.IsType<ArrayList>(editor.EditValue(null, provider, value)));
        OpenFileDialog dialog = GetDialog(editor);
        Assert.True(dialog.Multiselect);
        Assert.False(string.IsNullOrEmpty(dialog.Filter));

        Assert.Empty(Assert.IsType<ArrayList>(editor.EditValue(null, provider, value)));

        Assert.Same(dialog, GetDialog(editor));
        Assert.Equal(2, host.DialogCount);
    }

    [WinFormsFact]
    public void ImageListImageEditor_EditValue_Accept_LoadsSelectedImage()
    {
        using TempFile file = TempFile.Create(length: 0);
        using Bitmap source = new(10, 10);
        source.SetPixel(4, 4, Color.Red);
        source.Save(file.Path, ImageFormat.Bmp);
        ImageListImageEditor editor = new();
        using EditorDialogTestHost host = new(
            getDialog: () =>
            {
                OpenFileDialog dialog = GetDialog(editor);
                dialog.InitialDirectory = Path.GetDirectoryName(file.Path);
                dialog.FileName = file.Path;
                return dialog;
            },
            accept: true);

        ArrayList images = Assert.IsType<ArrayList>(editor.EditValue(null, CreateProvider(), null));
        try
        {
            ImageListImage result = Assert.IsType<ImageListImage>(Assert.Single(images.Cast<object>()));
            Assert.Equal(Path.GetFileName(file.Path), result.Name);
            Assert.Equal(source.Size, result.Size);
            Assert.Equal(Color.Red.ToArgb(), ((Bitmap)result.Image).GetPixel(4, 4).ToArgb());
            Assert.Equal(1, host.DialogCount);
        }
        finally
        {
            foreach (ImageListImage image in images)
            {
                image.Image.Dispose();
            }
        }
    }

    private static OpenFileDialog GetDialog(ImageListImageEditor editor)
        => (OpenFileDialog)editor.TestAccessor.Dynamic._fileDialog;

    private static IServiceProvider CreateProvider()
    {
        Mock<IServiceProvider> provider = new(MockBehavior.Strict);
        provider.Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(new Mock<IWindowsFormsEditorService>(MockBehavior.Strict).Object);
        return provider.Object;
    }

    /// <summary>
    ///  Exposes the production image filter configuration.
    /// </summary>
    private class SubImageListImageEditor : ImageListImageEditor
    {
        public new string GetFileDialogDescription()
            => base.GetFileDialogDescription();

        public new Type[] GetImageExtenders()
            => base.GetImageExtenders();
    }
}
