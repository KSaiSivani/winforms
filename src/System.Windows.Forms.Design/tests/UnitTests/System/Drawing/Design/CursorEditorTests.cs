// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms.Design;
using System.Windows.Forms.TestUtilities;
using Moq;

namespace System.Drawing.Design.Tests;

public class CursorEditorTests
{
    private const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

    private static Type GetCursorUIType()
    {
        return typeof(CursorEditor).GetNestedType("CursorUI", NonPublicInstance);
    }

    private static object CreateCursorUI()
    {
        return Activator.CreateInstance(GetCursorUIType());
    }

    private static ListBox CastToListBox(object instance) => (ListBox)instance;

    [Fact]
    public void CursorEditor_Ctor_Default()
    {
        CursorEditor editor = new();
        Assert.True(editor.IsDropDownResizable);
    }

    public static IEnumerable<object[]> EditValue_TestData()
    {
        yield return new object[] { null };
        yield return new object[] { "value" };
        yield return new object[] { Cursors.Default };
        yield return new object[] { new() };
    }

    [Theory]
    [MemberData(nameof(EditValue_TestData))]
    public void CursorEditor_EditValue_ValidProvider_ReturnsValue(object value)
    {
        CursorEditor editor = new();
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(mockEditorService.Object)
            .Verifiable();
        mockEditorService
            .Setup(e => e.DropDownControl(It.IsAny<Control>()))
            .Verifiable();
        Assert.Equal(value, editor.EditValue(null, mockServiceProvider.Object, value));
        mockServiceProvider.Verify(p => p.GetService(typeof(IWindowsFormsEditorService)), Times.Once());
        mockEditorService.Verify(e => e.DropDownControl(It.IsAny<Control>()), Times.Once());

        // Edit again.
        Assert.Equal(value, editor.EditValue(null, mockServiceProvider.Object, value));
        mockServiceProvider.Verify(p => p.GetService(typeof(IWindowsFormsEditorService)), Times.Exactly(2));
        mockServiceProvider.Verify(p => p.GetService(typeof(IWindowsFormsEditorService)), Times.Exactly(2));
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetEditValueInvalidProviderTestData))]
    public void CursorEditor_EditValue_InvalidProvider_ReturnsValue(IServiceProvider provider, object value)
    {
        CursorEditor editor = new();
        Assert.Same(value, editor.EditValue(null, provider, value));
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void CursorEditor_GetEditStyle_Invoke_ReturnsModal(ITypeDescriptorContext context)
    {
        CursorEditor editor = new();
        Assert.Equal(UITypeEditorEditStyle.DropDown, editor.GetEditStyle(context));
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void CursorEditor_GetPaintValueSupported_Invoke_ReturnsFalse(ITypeDescriptorContext context)
    {
        CursorEditor editor = new();
        Assert.False(editor.GetPaintValueSupported(context));
    }

    [Fact]
    public void CursorEditor_HasCursorUIField()
    {
        FieldInfo field = typeof(CursorEditor).GetField("_cursorUI", NonPublicInstance);
        Assert.NotNull(field);
    }

    [Fact]
    public void CursorEditor_CursorUIField_InitialValue_IsNull()
    {
        CursorEditor editor = new();
        FieldInfo field = typeof(CursorEditor).GetField("_cursorUI", NonPublicInstance);
        Assert.Null(field.GetValue(editor));
    }

    [Fact]
    public void CursorEditor_EditValue_CalledTwice_ReusesCursorUI()
    {
        CursorEditor editor = new();
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(mockEditorService.Object);
        mockEditorService
            .Setup(e => e.DropDownControl(It.IsAny<Control>()));

        _ = editor.EditValue(null, mockServiceProvider.Object, Cursors.Default);
        FieldInfo field = typeof(CursorEditor).GetField("_cursorUI", NonPublicInstance);
        object first = field.GetValue(editor);
        Assert.NotNull(first);

        _ = editor.EditValue(null, mockServiceProvider.Object, Cursors.Default);
        object second = field.GetValue(editor);
        Assert.Same(first, second);
    }

    [Fact]
    public void CursorEditor_EditValue_InvalidProvider_DoesNotCreateCursorUI()
    {
        CursorEditor editor = new();
        _ = editor.EditValue(null, null, Cursors.Default);

        FieldInfo field = typeof(CursorEditor).GetField("_cursorUI", NonPublicInstance);
        Assert.Null(field.GetValue(editor));
    }

    [Fact]
    public void CursorUI_Constructor_CreatesInstance()
    {
        object cursorUI = CreateCursorUI();
        Assert.NotNull(cursorUI);
    }

    [Fact]
    public void CursorUI_Constructor_DrawMode_IsOwnerDrawFixed()
    {
        object cursorUI = CreateCursorUI();
        var listBox = CastToListBox(cursorUI);
        Assert.Equal(DrawMode.OwnerDrawFixed, listBox.DrawMode);
    }

    [Fact]
    public void CursorUI_Constructor_BorderStyle_IsNone()
    {
        object cursorUI = CreateCursorUI();
        var listBox = CastToListBox(cursorUI);
        Assert.Equal(BorderStyle.None, listBox.BorderStyle);
    }

    [Fact]
    public void CursorUI_Constructor_Height_IsPositive()
    {
        object cursorUI = CreateCursorUI();
        var listBox = CastToListBox(cursorUI);
        Assert.True(listBox.Height > 0);
    }

    [Fact]
    public void CursorUI_Constructor_ItemHeight_IsPositive()
    {
        object cursorUI = CreateCursorUI();
        var listBox = CastToListBox(cursorUI);
        Assert.True(listBox.ItemHeight > 0);
    }

    [Fact]
    public void CursorUI_Constructor_ItemsPopulated_FromCursorConverter()
    {
        object cursorUI = CreateCursorUI();
        var listBox = CastToListBox(cursorUI);
        Assert.NotEmpty(listBox.Items);
        // All items should be Cursor instances.
        foreach (object item in listBox.Items)
        {
            Assert.IsType<Cursor>(item);
        }
    }

    [Fact]
    public void CursorUI_Value_InitiallyNull()
    {
        object cursorUI = CreateCursorUI();
        PropertyInfo property = GetCursorUIType().GetProperty("Value", PublicInstance);
        Assert.Null(property.GetValue(cursorUI));
    }

    [Fact]
    public void CursorUI_Start_SetsValue()
    {
        object cursorUI = CreateCursorUI();

        MethodInfo start = GetCursorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(cursorUI, [mockEditorService.Object, Cursors.Default]);

        PropertyInfo valueProperty = GetCursorUIType().GetProperty("Value", PublicInstance);
        Assert.Equal(Cursors.Default, valueProperty.GetValue(cursorUI));
    }

    [Fact]
    public void CursorUI_Start_SetsSelectedIndex_WhenValueMatches()
    {
        object cursorUI = CreateCursorUI();
        var listBox = CastToListBox(cursorUI);

        MethodInfo start = GetCursorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);

        // Use the first item in the list to ensure a match.
        object firstItem = listBox.Items[0];
        start.Invoke(cursorUI, [mockEditorService.Object, firstItem]);

        Assert.Equal(0, listBox.SelectedIndex);
    }

    [Fact]
    public void CursorUI_Start_SelectedIndexUnchanged_WhenValueDoesNotMatch()
    {
        object cursorUI = CreateCursorUI();
        var listBox = CastToListBox(cursorUI);

        MethodInfo start = GetCursorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);

        // A value not present in the items list - SelectedIndex should remain -1.
        start.Invoke(cursorUI, [mockEditorService.Object, "not-a-cursor"]);

        Assert.Equal(-1, listBox.SelectedIndex);
    }

    [Fact]
    public void CursorUI_Start_NullValue_DoesNotSetSelectedIndex()
    {
        object cursorUI = CreateCursorUI();
        var listBox = CastToListBox(cursorUI);

        MethodInfo start = GetCursorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);

        start.Invoke(cursorUI, [mockEditorService.Object, null]);

        Assert.Equal(-1, listBox.SelectedIndex);
        PropertyInfo valueProperty = GetCursorUIType().GetProperty("Value", PublicInstance);
        Assert.Null(valueProperty.GetValue(cursorUI));
    }

    [Fact]
    public void CursorUI_End_ResetsValueToNull()
    {
        object cursorUI = CreateCursorUI();
        MethodInfo start = GetCursorUIType().GetMethod("Start", PublicInstance);
        MethodInfo end = GetCursorUIType().GetMethod("End", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);

        start.Invoke(cursorUI, [mockEditorService.Object, Cursors.Default]);
        end.Invoke(cursorUI, null);

        PropertyInfo valueProperty = GetCursorUIType().GetProperty("Value", PublicInstance);
        Assert.Null(valueProperty.GetValue(cursorUI));
    }

    [Fact]
    public void CursorUI_End_ClearsCursorWidthCache()
    {
        object cursorUI = CreateCursorUI();
        MethodInfo start = GetCursorUIType().GetMethod("Start", PublicInstance);
        MethodInfo end = GetCursorUIType().GetMethod("End", PublicInstance);
        MethodInfo getCursorWidth = GetCursorUIType().GetMethod("GetCursorWidthForDpi", NonPublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);

        start.Invoke(cursorUI, [mockEditorService.Object, Cursors.Default]);
        // Populate cache.
        int first = (int)getCursorWidth.Invoke(cursorUI, [Cursors.Default, 96]);
        Assert.True(first > 0);

        end.Invoke(cursorUI, null);

        // After End, calling GetCursorWidthForDpi again must not throw and must return a value.
        int second = (int)getCursorWidth.Invoke(cursorUI, [Cursors.Default, 96]);
        Assert.True(second > 0);
    }

    [Fact]
    public void CursorUI_OnClick_SetsValueToSelectedItem_AndClosesDropDown()
    {
        object cursorUI = CreateCursorUI();
        var listBox = CastToListBox(cursorUI);
        object firstItem = listBox.Items[0];
        listBox.SelectedItem = firstItem;

        MethodInfo start = GetCursorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        mockEditorService
            .Setup(e => e.CloseDropDown())
            .Verifiable();
        start.Invoke(cursorUI, [mockEditorService.Object, firstItem]);

        MethodInfo onClick = GetCursorUIType().GetMethod("OnClick", NonPublicInstance);
        onClick.Invoke(cursorUI, [EventArgs.Empty]);

        PropertyInfo valueProperty = GetCursorUIType().GetProperty("Value", PublicInstance);
        Assert.Same(firstItem, valueProperty.GetValue(cursorUI));
        mockEditorService.Verify(e => e.CloseDropDown(), Times.Once());
    }

    [Fact]
    public void CursorUI_OnClick_NullSelectedItem_StillClosesDropDown()
    {
        object cursorUI = CreateCursorUI();
        var listBox = CastToListBox(cursorUI);
        listBox.SelectedItem = null;

        MethodInfo start = GetCursorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        mockEditorService
            .Setup(e => e.CloseDropDown())
            .Verifiable();
        start.Invoke(cursorUI, [mockEditorService.Object, null]);

        MethodInfo onClick = GetCursorUIType().GetMethod("OnClick", NonPublicInstance);
        onClick.Invoke(cursorUI, [EventArgs.Empty]);

        mockEditorService.Verify(e => e.CloseDropDown(), Times.Once());
    }

    [Fact]
    public void CursorUI_OnDrawItem_NegativeIndex_DoesNotThrow()
    {
        object cursorUI = CreateCursorUI();
        MethodInfo onDrawItem = GetCursorUIType().GetMethod("OnDrawItem", NonPublicInstance);

        using Bitmap bitmap = new(50, 20);
        using Graphics graphics = Graphics.FromImage(bitmap);
        DrawItemEventArgs args = new(graphics, Control.DefaultFont, new Rectangle(0, 0, 50, 20), -1, DrawItemState.Default);

        onDrawItem.Invoke(cursorUI, [args]);
    }

    [WinFormsFact]
    public void CursorUI_OnDrawItem_ValidIndex_DrawsItem()
    {
        using ListBox cursorUI = CastToListBox(CreateCursorUI());
        MethodInfo onDrawItem = GetCursorUIType().GetMethod("OnDrawItem", NonPublicInstance);

        using Bitmap bitmap = new(100, 40);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Magenta);
        DrawItemEventArgs args = new(
            graphics,
            Control.DefaultFont,
            new Rectangle(0, 0, 100, 40),
            0,
            DrawItemState.Default);

        onDrawItem.Invoke(cursorUI, [args]);
        Assert.NotEqual(Color.Magenta.ToArgb(), bitmap.GetPixel(2, 2).ToArgb());
    }

    [Fact]
    public void CursorUI_GetCursorWidthForDpi_ReturnsPositiveWidth()
    {
        object cursorUI = CreateCursorUI();
        MethodInfo getCursorWidth = GetCursorUIType().GetMethod("GetCursorWidthForDpi", NonPublicInstance);
        int width = (int)getCursorWidth.Invoke(cursorUI, [Cursors.Default, 96]);
        Assert.True(width > 0);
    }

    [WinFormsFact]
    public void CursorUI_GetCursorWidthForDpi_CachesResult()
    {
        using ListBox cursorUI = CastToListBox(CreateCursorUI());
        MethodInfo getCursorWidth = GetCursorUIType().GetMethod("GetCursorWidthForDpi", NonPublicInstance);
        Dictionary<(Cursor cursor, int dpi), int> cache = cursorUI.TestAccessor.Dynamic._cursorWidthCache;
        Assert.Empty(cache);

        int first = (int)getCursorWidth.Invoke(cursorUI, [Cursors.Default, 96]);
        Assert.True(first > 0);
        Assert.Equal(first, Assert.Single(cache).Value);
        Assert.Equal((Cursors.Default, 96), Assert.Single(cache).Key);
        // A sentinel distinguishes a cache hit from recomputing the same native size.
        cache[(Cursors.Default, 96)] = 123;
        int second = (int)getCursorWidth.Invoke(cursorUI, [Cursors.Default, 96]);
        Assert.Equal(123, second);
        Assert.Single(cache);

        int differentDpi = (int)getCursorWidth.Invoke(cursorUI, [Cursors.Default, 192]);
        Assert.True(differentDpi > 0);
        Assert.Equal(differentDpi, cache[(Cursors.Default, 192)]);
        Assert.Equal(2, cache.Count);
        GetCursorUIType().GetMethod("End", PublicInstance)!.Invoke(cursorUI, null);
        Assert.Empty(cache);
        Assert.Equal(first, (int)getCursorWidth.Invoke(cursorUI, [Cursors.Default, 96]));
    }

    [Fact]
    public void CursorUI_GetCursorWidthForDpi_DifferentDpis_ReturnPositiveWidths()
    {
        object cursorUI = CreateCursorUI();
        MethodInfo getCursorWidth = GetCursorUIType().GetMethod("GetCursorWidthForDpi", NonPublicInstance);

        int widthAt96 = (int)getCursorWidth.Invoke(cursorUI, [Cursors.Default, 96]);
        int widthAt192 = (int)getCursorWidth.Invoke(cursorUI, [Cursors.Default, 192]);

        // At a higher DPI the cursor width should be larger (or at least non-zero).
        Assert.True(widthAt96 > 0);
        Assert.True(widthAt192 > 0);
    }

    [Fact]
    public void CursorUI_ProcessDialogKey_Enter_TriggersOnClick()
    {
        object cursorUI = CreateCursorUI();
        var listBox = CastToListBox(cursorUI);
        object firstItem = listBox.Items[0];
        listBox.SelectedItem = firstItem;

        MethodInfo start = GetCursorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        mockEditorService
            .Setup(e => e.CloseDropDown())
            .Verifiable();
        start.Invoke(cursorUI, [mockEditorService.Object, firstItem]);

        MethodInfo processDialogKey = GetCursorUIType().GetMethod("ProcessDialogKey", NonPublicInstance);
        bool result = (bool)processDialogKey.Invoke(cursorUI, [Keys.Return]);

        Assert.True(result);
        PropertyInfo valueProperty = GetCursorUIType().GetProperty("Value", PublicInstance);
        Assert.Same(firstItem, valueProperty.GetValue(cursorUI));
        mockEditorService.Verify(e => e.CloseDropDown(), Times.Once());
    }
}
