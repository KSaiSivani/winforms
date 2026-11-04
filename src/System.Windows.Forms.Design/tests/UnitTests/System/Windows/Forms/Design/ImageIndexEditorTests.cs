// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using Moq;

namespace System.Windows.Forms.Design.Tests;

public class ImageIndexEditorTests
{
    private readonly ImageIndexEditor _editor = new();
    private ITypeDescriptorContext? _context = new Mock<ITypeDescriptorContext>().Object;

    [Theory]
    [BoolData]
    public void GetPaintValueSupported_WhenContextIsNullOrNot_ReturnsTrue(bool hasContext)
    {
        _context = hasContext ? _context : null;

        bool result = _editor.GetPaintValueSupported(_context);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Text")]
    [InlineData(1)]
    public void PaintValue_WithStringOrIntOrNull_DoesNotThrow(object? value)
    {
        using Bitmap bitmap = new(10, 10);
        using var graphics = Graphics.FromImage(bitmap);
        PaintValueEventArgs paintValueEventArgs = new(_context, value, graphics, new Rectangle(0, 0, 10, 10));

        Action action = () => _editor.PaintValue(paintValueEventArgs);

        action.Should().NotThrow();
    }

    [Fact]
    public void ImageIndexEditor_Ctor_InitializesImageEditor()
    {
        ImageIndexEditor editor = new();
        editor.ImageEditor.Should().NotBeNull();
    }

    [Fact]
    public void ImageIndexEditor_ParentImageListProperty_ReturnsParent()
    {
        ImageIndexEditor editor = new();
        editor.ParentImageListProperty.Should().Be("Parent");
    }

    [Fact]
    public void GetImage_WhenInstanceIsObjectArray_ReturnsNull()
    {
        SubImageIndexEditor editor = new();
        ITypeDescriptorContext context = CreateContextWithInstance(new object[] { new object(), new object() });

        Image? result = editor.GetImagePublic(context, 0, null, true);

        result.Should().BeNull();
    }

    [Fact]
    public void GetImage_WhenIndexNegativeAndKeyNull_ReturnsNull()
    {
        SubImageIndexEditor editor = new();
        ITypeDescriptorContext context = CreateContextWithInstance(new object());

        Image? result = editor.GetImagePublic(context, -1, null, true);

        result.Should().BeNull();
    }

    [Fact]
    public void GetImage_WhenPropertyHasNoRelatedImageListAttributeAndNoImageListProperty_ReturnsNull()
    {
        SubImageIndexEditor editor = new();
        NonAttributedPropertyComponent component = new();
        PropertyDescriptor anyProperty = TypeDescriptor.GetProperties(component)["Value"]!;
        ITypeDescriptorContext context = CreateContextWithInstanceAndProperty(component, anyProperty);

        Image? result = editor.GetImagePublic(context, 0, null, true);

        result.Should().BeNull();
    }

    [Fact]
    public void GetImage_WhenContextHasRelatedImageListAttribute_ReturnsImage()
    {
        SubImageIndexEditor editor = new();
        using ImageList imageList = new();
        using Bitmap image = new(10, 10);
        imageList.Images.Add(image);

        RelatedImageListComponent component = new(imageList);
        PropertyDescriptor imageIndexProperty = TypeDescriptor.GetProperties(component)["ImageIndex"]!;
        ITypeDescriptorContext context = CreateContextWithInstanceAndProperty(component, imageIndexProperty);

        Image? result = editor.GetImagePublic(context, 0, null, true);

        result.Should().NotBeNull();
    }

    [Fact]
    public void GetImage_WhenContextHasRelatedImageListAttribute_ReturnsImageByKey()
    {
        SubImageIndexEditor editor = new();
        using ImageList imageList = new();
        using Bitmap image = new(10, 10);
        imageList.Images.Add("myKey", image);

        RelatedImageListComponent component = new(imageList);
        PropertyDescriptor imageKeyProperty = TypeDescriptor.GetProperties(component)["ImageKey"]!;
        ITypeDescriptorContext context = CreateContextWithInstanceAndProperty(component, imageKeyProperty);

        Image? result = editor.GetImagePublic(context, -1, "myKey", false);

        result.Should().NotBeNull();
    }

    [Fact]
    public void GetImage_WhenUseIntIndexAndIndexOutOfRange_ReturnsNull()
    {
        SubImageIndexEditor editor = new();
        using ImageList imageList = new();
        using Bitmap image = new(10, 10);
        imageList.Images.Add(image);

        RelatedImageListComponent component = new(imageList);
        PropertyDescriptor imageIndexProperty = TypeDescriptor.GetProperties(component)["ImageIndex"]!;
        ITypeDescriptorContext context = CreateContextWithInstanceAndProperty(component, imageIndexProperty);

        Image? result = editor.GetImagePublic(context, 5, null, true);

        result.Should().BeNull();
    }

    [Fact]
    public void GetImage_WhenInstanceChanges_ReacquiresImageList()
    {
        SubImageIndexEditor editor = new();
        using ImageList imageList1 = new() { ImageSize = new Size(10, 10) };
        using Bitmap image1 = new(10, 10);
        imageList1.Images.Add(image1);
        RelatedImageListComponent component1 = new(imageList1);

        using ImageList imageList2 = new() { ImageSize = new Size(20, 20) };
        using Bitmap image2 = new(20, 20);
        imageList2.Images.Add(image2);
        RelatedImageListComponent component2 = new(imageList2);

        PropertyDescriptor imageIndexProperty = TypeDescriptor.GetProperties(component1)["ImageIndex"]!;

        ITypeDescriptorContext context1 = CreateContextWithInstanceAndProperty(component1, imageIndexProperty);
        using Image? firstResult = editor.GetImagePublic(context1, 0, null, true);
        firstResult.Should().NotBeNull();
        firstResult.Size.Should().Be(imageList1.ImageSize);

        ITypeDescriptorContext context2 = CreateContextWithInstanceAndProperty(component2, imageIndexProperty);
        using Image? secondResult = editor.GetImagePublic(context2, 0, null, true);
        secondResult.Should().NotBeNull();
        secondResult.Size.Should().Be(imageList2.ImageSize);
    }

    [Fact]
    public void GetImage_WhenInstanceSame_UsesCachedImageList()
    {
        SubImageIndexEditor editor = new();
        using ImageList imageList = new();
        using Bitmap image = new(10, 10);
        imageList.Images.Add(image);
        RelatedImageListComponent component = new(imageList);

        PropertyDescriptor imageIndexProperty = TypeDescriptor.GetProperties(component)["ImageIndex"]!;
        ITypeDescriptorContext context = CreateContextWithInstanceAndProperty(component, imageIndexProperty);

        using Image? firstResult = editor.GetImagePublic(context, 0, null, true);
        object cachedProperty = editor.TestAccessor.Dynamic._currentImageListPropertyReference;
        using Image? secondResult = editor.GetImagePublic(context, 0, null, true);

        firstResult.Should().NotBeNull();
        secondResult.Should().NotBeNull();
        Assert.Same(cachedProperty, (object)editor.TestAccessor.Dynamic._currentImageListPropertyReference);
    }

    [Fact]
    public void GetImage_WhenSameInstanceButImageListPropertyChanged_ReacquiresImageList()
    {
        SubImageIndexEditor editor = new();
        using ImageList imageList1 = new() { ImageSize = new Size(10, 10) };
        using Bitmap image1 = new(10, 10);
        imageList1.Images.Add(image1);

        using ImageList imageList2 = new() { ImageSize = new Size(20, 20) };
        using Bitmap image2 = new(20, 20);
        imageList2.Images.Add(image2);

        RelatedImageListComponent component = new(imageList1);
        PropertyDescriptor imageIndexProperty = TypeDescriptor.GetProperties(component)["ImageIndex"]!;
        ITypeDescriptorContext context = CreateContextWithInstanceAndProperty(component, imageIndexProperty);

        using Image? firstResult = editor.GetImagePublic(context, 0, null, true);
        firstResult.Should().NotBeNull();
        firstResult.Size.Should().Be(imageList1.ImageSize);

        // Swap the ImageList property on the same component. The cached weak reference
        // path detects the value change and forces a re-acquisition of the image list.
        component.ImageList = imageList2;

        using Image? secondResult = editor.GetImagePublic(context, 0, null, true);
        secondResult.Should().NotBeNull();
        secondResult.Size.Should().Be(imageList2.ImageSize);
    }

    [Fact]
    public void GetImage_WhenPropertyAttributePathNavigatesToImageList_ReturnsImage()
    {
        SubImageIndexEditor editor = new();
        using ImageList imageList = new();
        using Bitmap image = new(10, 10);
        imageList.Images.Add(image);

        NestedRelatedImageListComponent component = new(imageList);
        PropertyDescriptor imageIndexProperty = TypeDescriptor.GetProperties(component)["ImageIndex"]!;
        ITypeDescriptorContext context = CreateContextWithInstanceAndProperty(component, imageIndexProperty);

        Image? result = editor.GetImagePublic(context, 0, null, true);

        result.Should().NotBeNull();
    }

    [Fact]
    public void GetImage_WhenComponentHasNoImageListPropertyAttributeButHasImageListProperty_ReturnsImage()
    {
        SubImageIndexEditor editor = new();
        using ImageList imageList = new();
        using Bitmap image = new(10, 10);
        imageList.Images.Add(image);

        ComponentWithImageListProperty component = new(imageList);
        PropertyDescriptor anyProperty = TypeDescriptor.GetProperties(component)["Value"]!;
        ITypeDescriptorContext context = CreateContextWithInstanceAndProperty(component, anyProperty);

        Image? result = editor.GetImagePublic(context, 0, null, true);

        result.Should().NotBeNull();
    }

    [Fact]
    public void GetImage_WhenUseIntIndexFalse_ReturnsImageByKey()
    {
        SubImageIndexEditor editor = new();
        using ImageList imageList = new();
        using Bitmap image = new(10, 10);
        imageList.Images.Add("key1", image);
        imageList.Images.Add("key2", new Bitmap(20, 20));

        RelatedImageListComponent component = new(imageList);
        PropertyDescriptor imageKeyProperty = TypeDescriptor.GetProperties(component)["ImageKey"]!;
        ITypeDescriptorContext context = CreateContextWithInstanceAndProperty(component, imageKeyProperty);

        Image? result = editor.GetImagePublic(context, -1, "key1", false);

        result.Should().NotBeNull();
    }

    [Fact]
    public void GetImageListProperty_WhenInstanceIsObjectArray_ReturnsNull()
    {
        PropertyDescriptor descriptor = TypeDescriptor.GetProperties(new NonAttributedPropertyComponent())["Value"]!;
        object[] array = [new()];
        object? instance = array;

        PropertyDescriptor? result = ImageIndexEditor.GetImageListProperty(descriptor, ref instance);

        result.Should().BeNull();
    }

    [Fact]
    public void GetImageListProperty_WhenPropertyHasNoAttribute_ReturnsNull()
    {
        PropertyDescriptor descriptor = TypeDescriptor.GetProperties(new NonAttributedPropertyComponent())["Value"]!;
        object? instance = new NonAttributedPropertyComponent();

        PropertyDescriptor? result = ImageIndexEditor.GetImageListProperty(descriptor, ref instance);

        result.Should().BeNull();
    }

    [Fact]
    public void GetImageListProperty_WhenRelatedImageListIsNull_ReturnsNull()
    {
        NullRelatedImageListComponent component = new();
        PropertyDescriptor descriptor = TypeDescriptor.GetProperties(component)["Value"]!;
        object? instance = component;

        PropertyDescriptor? result = ImageIndexEditor.GetImageListProperty(descriptor, ref instance);

        result.Should().BeNull();
    }

    [Fact]
    public void GetImageListProperty_WhenRelatedImageListPointsToImageList_ReturnsProperty()
    {
        RelatedImageListComponent component = new(new ImageList());
        PropertyDescriptor descriptor = TypeDescriptor.GetProperties(component)["ImageIndex"]!;
        object? instance = component;

        PropertyDescriptor? result = ImageIndexEditor.GetImageListProperty(descriptor, ref instance);

        result.Should().NotBeNull();
        result.Name.Should().Be("ImageList");
    }

    [Fact]
    public void GetImageListProperty_WhenRelatedImageListPathNavigates_ReturnsProperty()
    {
        NestedRelatedImageListComponent component = new(new ImageList());
        PropertyDescriptor descriptor = TypeDescriptor.GetProperties(component)["ImageIndex"]!;
        object? instance = component;

        PropertyDescriptor? result = ImageIndexEditor.GetImageListProperty(descriptor, ref instance);

        result.Should().NotBeNull();
        result.Name.Should().Be("ImageList");
    }

    [Fact]
    public void GetImageListProperty_WhenPathLeafIsNotImageList_ReturnsNull()
    {
        NonImageListPathComponent component = new();
        PropertyDescriptor descriptor = TypeDescriptor.GetProperties(component)["Value"]!;
        object? instance = component;

        PropertyDescriptor? result = ImageIndexEditor.GetImageListProperty(descriptor, ref instance);

        result.Should().BeNull();
    }

    [Theory]
    [BoolData]
    public void PaintValue_WithImageList_PaintsImageInsideBounds(bool useKey)
    {
        using ImageList imageList = new();
        using Bitmap image = new(10, 10);
        using (Graphics imageGraphics = Graphics.FromImage(image))
        {
            imageGraphics.Clear(Color.Red);
        }

        imageList.Images.Add("myKey", image);

        RelatedImageListComponent component = new(imageList);
        PropertyDescriptor property = TypeDescriptor.GetProperties(component)[useKey ? "ImageKey" : "ImageIndex"]!;
        ITypeDescriptorContext context = CreateContextWithInstanceAndProperty(component, property);

        using Bitmap surface = new(20, 20);
        using Graphics graphics = Graphics.FromImage(surface);
        graphics.Clear(Color.Lime);
        PaintValueEventArgs paintValueEventArgs = new(context, useKey ? "myKey" : 0, graphics, new Rectangle(2, 2, 10, 10));

        _editor.PaintValue(paintValueEventArgs);

        Assert.Equal(Color.Red.ToArgb(), surface.GetPixel(6, 6).ToArgb());
        Assert.Equal(Color.Lime.ToArgb(), surface.GetPixel(19, 19).ToArgb());
    }

    [Fact]
    public void PaintValue_WithIntValueIndexOutOfRange_DoesNotThrow()
    {
        using ImageList imageList = new();
        using Bitmap image = new(10, 10);
        imageList.Images.Add(image);

        RelatedImageListComponent component = new(imageList);
        PropertyDescriptor imageIndexProperty = TypeDescriptor.GetProperties(component)["ImageIndex"]!;
        ITypeDescriptorContext context = CreateContextWithInstanceAndProperty(component, imageIndexProperty);

        using Bitmap surface = new(20, 20);
        using Graphics graphics = Graphics.FromImage(surface);
        PaintValueEventArgs paintValueEventArgs = new(context, 99, graphics, new Rectangle(0, 0, 10, 10));

        Action action = () => _editor.PaintValue(paintValueEventArgs);

        action.Should().NotThrow();
    }

    [Fact]
    public void PaintValue_WithStringValueKeyNotFound_DoesNotThrow()
    {
        using ImageList imageList = new();
        using Bitmap image = new(10, 10);
        imageList.Images.Add("myKey", image);

        RelatedImageListComponent component = new(imageList);
        PropertyDescriptor imageKeyProperty = TypeDescriptor.GetProperties(component)["ImageKey"]!;
        ITypeDescriptorContext context = CreateContextWithInstanceAndProperty(component, imageKeyProperty);

        using Bitmap surface = new(20, 20);
        using Graphics graphics = Graphics.FromImage(surface);
        PaintValueEventArgs paintValueEventArgs = new(context, "missingKey", graphics, new Rectangle(0, 0, 10, 10));

        Action action = () => _editor.PaintValue(paintValueEventArgs);

        action.Should().NotThrow();
    }

    [Fact]
    public void PaintValue_WithObjectValue_DoesNotThrow()
    {
        using Bitmap bitmap = new(10, 10);
        using Graphics graphics = Graphics.FromImage(bitmap);
        PaintValueEventArgs paintValueEventArgs = new(_context, new object(), graphics, new Rectangle(0, 0, 10, 10));

        Action action = () => _editor.PaintValue(paintValueEventArgs);

        action.Should().NotThrow();
    }

    private static ITypeDescriptorContext CreateContextWithInstance(object instance)
    {
        Mock<ITypeDescriptorContext> contextMock = new();
        contextMock.Setup(c => c.Instance).Returns(instance);
        return contextMock.Object;
    }

    private static ITypeDescriptorContext CreateContextWithInstanceAndProperty(object instance, PropertyDescriptor? property)
    {
        Mock<ITypeDescriptorContext> contextMock = new();
        contextMock.Setup(c => c.Instance).Returns(instance);
        contextMock.Setup(c => c.PropertyDescriptor).Returns(property);
        return contextMock.Object;
    }

    // A SubImageIndexEditor derived type that exposes the protected GetImage method
    // so the unit test can exercise it directly.
    private class SubImageIndexEditor : ImageIndexEditor
    {
        public Image? GetImagePublic(ITypeDescriptorContext context, int index, string? key, bool useIntIndex)
            => GetImage(context, index, key, useIntIndex);
    }

    private class RelatedImageListComponent
    {
        public RelatedImageListComponent(ImageList imageList) => ImageList = imageList;

        public ImageList ImageList { get; set; }

        [RelatedImageList("ImageList")]
        public int ImageIndex { get; set; }

        [RelatedImageList("ImageList")]
        public string? ImageKey { get; set; }
    }

    private class NestedImageListHolder
    {
        public NestedImageListHolder(ImageList imageList) => ImageList = imageList;

        public ImageList ImageList { get; set; }
    }

    private class NestedRelatedImageListComponent
    {
        public NestedRelatedImageListComponent(ImageList imageList) => Holder = new NestedImageListHolder(imageList);

        public NestedImageListHolder Holder { get; set; }

        [RelatedImageList("Holder.ImageList")]
        public int ImageIndex { get; set; }
    }

    private class NullRelatedImageListComponent
    {
        [RelatedImageList(null)]
        public string Value { get; set; } = string.Empty;
    }

    private class NonImageListPathComponent
    {
        [RelatedImageList("Value")]
        public string Value { get; set; } = string.Empty;
    }

    private class ComponentWithImageListProperty
    {
        public ComponentWithImageListProperty(ImageList imageList) => ImageList = imageList;

        // Not annotated with [RelatedImageList] so GetImageListProperty returns null
        // and the GetImage fallback path that walks public properties is exercised.
        public ImageList ImageList { get; }

        public string Value { get; set; } = string.Empty;
    }

    // A simple component that has a public property with no [RelatedImageList] attribute.
    // Used to exercise the early-return path in GetImageListProperty when the property
    // descriptor is not associated with an image list.
    private class NonAttributedPropertyComponent
    {
        public string Value { get; set; } = string.Empty;
    }
}
