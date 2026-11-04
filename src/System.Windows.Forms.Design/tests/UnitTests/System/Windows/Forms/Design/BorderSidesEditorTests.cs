// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Reflection;
using System.Windows.Forms.TestUtilities;
using Moq;

namespace System.Windows.Forms.Design.Tests;

public class BorderSidesEditorTests
{
    private const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

    private static Type GetBorderSidesEditorUIType()
    {
        return typeof(BorderSidesEditor).GetNestedType("BorderSidesEditorUI", NonPublicInstance);
    }

    private static object CreateBorderSidesEditorUI()
    {
        return Activator.CreateInstance(GetBorderSidesEditorUIType());
    }

    private static Control CastToControl(object instance) => (Control)instance;

    private static object GetValue(object borderSidesEditorUI)
        => GetBorderSidesEditorUIType().GetProperty("Value", PublicInstance).GetValue(borderSidesEditorUI);

    private static void Start(object borderSidesEditorUI, IWindowsFormsEditorService service, ToolStripStatusLabelBorderSides value)
        => GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance).Invoke(borderSidesEditorUI, [service, value]);

    private static void End(object borderSidesEditorUI)
        => GetBorderSidesEditorUIType().GetMethod("End", PublicInstance).Invoke(borderSidesEditorUI, null);

    private static CheckBox GetCheckBox(object borderSidesEditorUI, string fieldName)
    {
        return (CheckBox)borderSidesEditorUI.GetType()
            .GetField(fieldName, NonPublicInstance)
            .GetValue(borderSidesEditorUI);
    }

    private static TableLayoutPanel GetTableLayoutPanel(object borderSidesEditorUI)
    {
        return (TableLayoutPanel)borderSidesEditorUI.GetType()
            .GetField("_tableLayoutPanel", NonPublicInstance)
            .GetValue(borderSidesEditorUI);
    }

    private static Label GetSplitterLabel(object borderSidesEditorUI)
    {
        return (Label)borderSidesEditorUI.GetType()
            .GetField("_splitterLabel", NonPublicInstance)
            .GetValue(borderSidesEditorUI);
    }

    [Fact]
    public void BorderSidesEditor_Ctor_Default()
    {
        BorderSidesEditor editor = new();
        Assert.False(editor.IsDropDownResizable);
    }

    [Fact]
    public void BorderSidesEditor_HasBorderSidesEditorUIField()
    {
        FieldInfo field = typeof(BorderSidesEditor).GetField("_borderSidesEditorUI", NonPublicInstance);
        Assert.NotNull(field);
    }

    [Fact]
    public void BorderSidesEditor_BorderSidesEditorUIField_InitialValue_IsNull()
    {
        BorderSidesEditor editor = new();
        FieldInfo field = typeof(BorderSidesEditor).GetField("_borderSidesEditorUI", NonPublicInstance);
        Assert.Null(field.GetValue(editor));
    }

    public static IEnumerable<object[]> EditValue_TestData()
    {
        yield return new object[] { null };
        yield return new object[] { "value" };
        yield return new object[] { ToolStripStatusLabelBorderSides.None };
        yield return new object[] { ToolStripStatusLabelBorderSides.Top };
        yield return new object[] { ToolStripStatusLabelBorderSides.Bottom };
        yield return new object[] { ToolStripStatusLabelBorderSides.Left };
        yield return new object[] { ToolStripStatusLabelBorderSides.Right };
        yield return new object[] { ToolStripStatusLabelBorderSides.All };
        yield return new object[] { new() };
    }

    [Theory]
    [MemberData(nameof(EditValue_TestData))]
    public void BorderSidesEditor_EditValue_ValidProvider_ReturnsValue(object value)
    {
        BorderSidesEditor editor = new();
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

    [Fact]
    public void BorderSidesEditor_EditValue_CalledTwice_ReusesBorderSidesEditorUI()
    {
        BorderSidesEditor editor = new();
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(mockEditorService.Object);
        mockEditorService
            .Setup(e => e.DropDownControl(It.IsAny<Control>()));

        _ = editor.EditValue(null, mockServiceProvider.Object, ToolStripStatusLabelBorderSides.Top);
        FieldInfo field = typeof(BorderSidesEditor).GetField("_borderSidesEditorUI", NonPublicInstance);
        object first = field.GetValue(editor);
        Assert.NotNull(first);

        _ = editor.EditValue(null, mockServiceProvider.Object, ToolStripStatusLabelBorderSides.Top);
        object second = field.GetValue(editor);
        Assert.Same(first, second);
    }

    [Fact]
    public void BorderSidesEditor_EditValue_InvalidProvider_DoesNotCreateBorderSidesEditorUI()
    {
        BorderSidesEditor editor = new();
        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns((object)null);

        _ = editor.EditValue(null, mockServiceProvider.Object, ToolStripStatusLabelBorderSides.Top);

        FieldInfo field = typeof(BorderSidesEditor).GetField("_borderSidesEditorUI", NonPublicInstance);
        Assert.Null(field.GetValue(editor));
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetEditValueInvalidProviderTestData))]
    public void BorderSidesEditor_EditValue_InvalidProvider_ReturnsValue(IServiceProvider provider, object value)
    {
        BorderSidesEditor editor = new();
        Assert.Same(value, editor.EditValue(null, provider, value));
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void BorderSidesEditor_GetEditStyle_Invoke_ReturnsModal(ITypeDescriptorContext context)
    {
        BorderSidesEditor editor = new();
        Assert.Equal(UITypeEditorEditStyle.DropDown, editor.GetEditStyle(context));
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void BorderSidesEditor_GetPaintValueSupported_Invoke_ReturnsFalse(ITypeDescriptorContext context)
    {
        BorderSidesEditor editor = new();
        Assert.False(editor.GetPaintValueSupported(context));
    }

    [Fact]
    public void BorderSidesEditorUI_Constructor_CreatesInstance()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        Assert.NotNull(borderSidesEditorUI);
    }

    [Fact]
    public void BorderSidesEditorUI_Constructor_Size_IsNonZero()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        var control = CastToControl(borderSidesEditorUI);
        Assert.True(control.Width > 0);
        Assert.True(control.Height > 0);
    }

    [Fact]
    public void BorderSidesEditorUI_Constructor_HasTableLayoutPanel()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        var control = CastToControl(borderSidesEditorUI);
        Assert.Single(control.Controls);
    }

    [Fact]
    public void BorderSidesEditorUI_TableLayoutPanel_IsPresent()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        var tableLayoutPanel = GetTableLayoutPanel(borderSidesEditorUI);
        Assert.NotNull(tableLayoutPanel);
    }

    [Fact]
    public void BorderSidesEditorUI_TableLayoutPanel_BackColor_IsWindow()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        var tableLayoutPanel = GetTableLayoutPanel(borderSidesEditorUI);
        Assert.Equal(SystemColors.Window, tableLayoutPanel.BackColor);
    }

    [Fact]
    public void BorderSidesEditorUI_TableLayoutPanel_Margin_IsZero()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        var tableLayoutPanel = GetTableLayoutPanel(borderSidesEditorUI);
        Assert.Equal(Padding.Empty, tableLayoutPanel.Margin);
    }

    [Fact]
    public void BorderSidesEditorUI_TableLayoutPanel_HasAllControls()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        var tableLayoutPanel = GetTableLayoutPanel(borderSidesEditorUI);
        Assert.True(tableLayoutPanel.Controls.Count >= 7);
    }

    [Fact]
    public void BorderSidesEditorUI_NoneCheckBox_IsPresent()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        var noneCheckBox = GetCheckBox(borderSidesEditorUI, "_noneCheckBox");
        Assert.NotNull(noneCheckBox);
    }

    [Fact]
    public void BorderSidesEditorUI_AllCheckBox_IsPresent()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");
        Assert.NotNull(allCheckBox);
    }

    [Fact]
    public void BorderSidesEditorUI_TopCheckBox_IsPresent()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        var topCheckBox = GetCheckBox(borderSidesEditorUI, "_topCheckBox");
        Assert.NotNull(topCheckBox);
    }

    [Fact]
    public void BorderSidesEditorUI_BottomCheckBox_IsPresent()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        var bottomCheckBox = GetCheckBox(borderSidesEditorUI, "_bottomCheckBox");
        Assert.NotNull(bottomCheckBox);
    }

    [Fact]
    public void BorderSidesEditorUI_LeftCheckBox_IsPresent()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        var leftCheckBox = GetCheckBox(borderSidesEditorUI, "_leftCheckBox");
        Assert.NotNull(leftCheckBox);
    }

    [Fact]
    public void BorderSidesEditorUI_RightCheckBox_IsPresent()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        var rightCheckBox = GetCheckBox(borderSidesEditorUI, "_rightCheckBox");
        Assert.NotNull(rightCheckBox);
    }

    [Fact]
    public void BorderSidesEditorUI_SplitterLabel_IsPresent()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        var splitterLabel = GetSplitterLabel(borderSidesEditorUI);
        Assert.NotNull(splitterLabel);
    }

    [Fact]
    public void BorderSidesEditorUI_SplitterLabel_BackColor_IsControlDark()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        var splitterLabel = GetSplitterLabel(borderSidesEditorUI);
        Assert.Equal(SystemColors.ControlDark, splitterLabel.BackColor);
    }

    [Fact]
    public void BorderSidesEditorUI_Value_Initially_Null()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        PropertyInfo valueProperty = GetBorderSidesEditorUIType().GetProperty("Value", PublicInstance);
        Assert.Null(valueProperty.GetValue(borderSidesEditorUI));
    }

    [Fact]
    public void BorderSidesEditorUI_EditorService_Initially_Null()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        PropertyInfo editorServiceProperty = GetBorderSidesEditorUIType().GetProperty("EditorService", PublicInstance);
        Assert.Null(editorServiceProperty.GetValue(borderSidesEditorUI));
    }

    [Fact]
    public void BorderSidesEditorUI_End_SetsValueToNull()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        MethodInfo end = GetBorderSidesEditorUIType().GetMethod("End", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);

        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.Top]);
        end.Invoke(borderSidesEditorUI, null);

        PropertyInfo valueProperty = GetBorderSidesEditorUIType().GetProperty("Value", PublicInstance);
        Assert.Null(valueProperty.GetValue(borderSidesEditorUI));
    }

    [Fact]
    public void BorderSidesEditorUI_End_SetsEditorServiceToNull()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        MethodInfo end = GetBorderSidesEditorUIType().GetMethod("End", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);

        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.Top]);
        end.Invoke(borderSidesEditorUI, null);

        PropertyInfo editorServiceProperty = GetBorderSidesEditorUIType().GetProperty("EditorService", PublicInstance);
        Assert.Null(editorServiceProperty.GetValue(borderSidesEditorUI));
    }

    [Fact]
    public void BorderSidesEditorUI_End_ResetsUpdateCurrentValue()
    {
        using Control borderSidesEditorUI = CastToControl(CreateBorderSidesEditorUI());
        Mock<IWindowsFormsEditorService> service = new(MockBehavior.Strict);
        Start(borderSidesEditorUI, service.Object, ToolStripStatusLabelBorderSides.Top);
        GetCheckBox(borderSidesEditorUI, "_leftCheckBox").Checked = true;
        Assert.Equal(ToolStripStatusLabelBorderSides.Top | ToolStripStatusLabelBorderSides.Left,
            GetValue(borderSidesEditorUI));

        End(borderSidesEditorUI);
        GetCheckBox(borderSidesEditorUI, "_rightCheckBox").Checked = true;
        GetCheckBox(borderSidesEditorUI, "_topCheckBox").Checked = false;

        Assert.Null(GetValue(borderSidesEditorUI));
        Assert.True(GetCheckBox(borderSidesEditorUI, "_rightCheckBox").Checked);
    }

    [Theory]
    [InlineData(ToolStripStatusLabelBorderSides.None)]
    [InlineData(ToolStripStatusLabelBorderSides.Top)]
    [InlineData(ToolStripStatusLabelBorderSides.Bottom)]
    [InlineData(ToolStripStatusLabelBorderSides.Left)]
    [InlineData(ToolStripStatusLabelBorderSides.Right)]
    [InlineData(ToolStripStatusLabelBorderSides.All)]
    [InlineData(ToolStripStatusLabelBorderSides.Top | ToolStripStatusLabelBorderSides.Bottom)]
    [InlineData(ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Right)]
    [InlineData(ToolStripStatusLabelBorderSides.Top | ToolStripStatusLabelBorderSides.Left)]
    [InlineData(ToolStripStatusLabelBorderSides.Top | ToolStripStatusLabelBorderSides.Right)]
    [InlineData(ToolStripStatusLabelBorderSides.Bottom | ToolStripStatusLabelBorderSides.Left)]
    [InlineData(ToolStripStatusLabelBorderSides.Bottom | ToolStripStatusLabelBorderSides.Right)]
    public void BorderSidesEditorUI_Start_ToolStripStatusLabelBorderSides_SetsValueToExpected(ToolStripStatusLabelBorderSides value)
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, value]);

        PropertyInfo valueProperty = GetBorderSidesEditorUIType().GetProperty("Value", PublicInstance);
        Assert.Equal(value, valueProperty.GetValue(borderSidesEditorUI));
    }

    [Theory]
    [InlineData(ToolStripStatusLabelBorderSides.None)]
    [InlineData(ToolStripStatusLabelBorderSides.Top)]
    [InlineData(ToolStripStatusLabelBorderSides.Bottom)]
    [InlineData(ToolStripStatusLabelBorderSides.Left)]
    [InlineData(ToolStripStatusLabelBorderSides.Right)]
    [InlineData(ToolStripStatusLabelBorderSides.All)]
    public void BorderSidesEditorUI_Start_ToolStripStatusLabelBorderSides_SetsEditorService(ToolStripStatusLabelBorderSides value)
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, value]);

        PropertyInfo editorServiceProperty = GetBorderSidesEditorUIType().GetProperty("EditorService", PublicInstance);
        Assert.Same(mockEditorService.Object, editorServiceProperty.GetValue(borderSidesEditorUI));
    }

    [Fact]
    public void BorderSidesEditorUI_Start_NullValue_SetsValueToNull()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, null]);

        PropertyInfo valueProperty = GetBorderSidesEditorUIType().GetProperty("Value", PublicInstance);
        Assert.Null(valueProperty.GetValue(borderSidesEditorUI));
    }

    [Fact]
    public void BorderSidesEditorUI_Start_NonEnumValue_SetsValueAndDoesNotThrow()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        object nonEnum = "not-sides";
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, nonEnum]);

        PropertyInfo valueProperty = GetBorderSidesEditorUIType().GetProperty("Value", PublicInstance);
        Assert.Equal(nonEnum, valueProperty.GetValue(borderSidesEditorUI));
    }

    [Fact]
    public void BorderSidesEditorUI_Start_NullValue_DoesNotInvokeUpdateCurrentValue()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, null]);

        // Without _updateCurrentValue being set, toggling checkboxes should not
        // cause UpdateCurrentValue to overwrite Value.
        var noneCheckBox = GetCheckBox(borderSidesEditorUI, "_noneCheckBox");
        noneCheckBox.Checked = true;

        PropertyInfo valueProperty = GetBorderSidesEditorUIType().GetProperty("Value", PublicInstance);
        Assert.Null(valueProperty.GetValue(borderSidesEditorUI));
    }

    [Fact]
    public void BorderSidesEditorUI_Start_NonEnumValue_DoesNotInvokeUpdateCurrentValue()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, "not-sides"]);

        // Without _updateCurrentValue being set, toggling checkboxes should not
        // cause UpdateCurrentValue to overwrite Value.
        var noneCheckBox = GetCheckBox(borderSidesEditorUI, "_noneCheckBox");
        noneCheckBox.Checked = true;

        PropertyInfo valueProperty = GetBorderSidesEditorUIType().GetProperty("Value", PublicInstance);
        Assert.Equal("not-sides", valueProperty.GetValue(borderSidesEditorUI));
    }

    [Fact]
    public void BorderSidesEditorUI_OnGotFocus_DoesNotThrow()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo onGotFocus = GetBorderSidesEditorUIType()
            .GetMethod("OnGotFocus", NonPublicInstance);
        onGotFocus.Invoke(borderSidesEditorUI, [EventArgs.Empty]);
    }

    [Fact]
    public void BorderSidesEditorUI_NoneCheckBox_Checked_UnchecksAllSides()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.All]);

        var noneCheckBox = GetCheckBox(borderSidesEditorUI, "_noneCheckBox");
        noneCheckBox.Checked = true;

        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");
        var topCheckBox = GetCheckBox(borderSidesEditorUI, "_topCheckBox");
        var bottomCheckBox = GetCheckBox(borderSidesEditorUI, "_bottomCheckBox");
        var leftCheckBox = GetCheckBox(borderSidesEditorUI, "_leftCheckBox");
        var rightCheckBox = GetCheckBox(borderSidesEditorUI, "_rightCheckBox");

        Assert.True(noneCheckBox.Checked);
        Assert.False(allCheckBox.Checked);
        Assert.False(topCheckBox.Checked);
        Assert.False(bottomCheckBox.Checked);
        Assert.False(leftCheckBox.Checked);
        Assert.False(rightCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_NoneCheckBox_Unchecked_DoesNotChangeOthers()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.None]);

        var noneCheckBox = GetCheckBox(borderSidesEditorUI, "_noneCheckBox");
        var topCheckBox = GetCheckBox(borderSidesEditorUI, "_topCheckBox");

        // After Start(None), noneCheckBox is checked and all sides are unchecked.
        // Unchecking noneCheckBox when all sides are already unchecked takes the no-op branch.
        bool topBefore = topCheckBox.Checked;
        noneCheckBox.Checked = false;

        Assert.Equal(topBefore, topCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_AllCheckBox_Checked_ChecksAllSides()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.None]);

        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");
        allCheckBox.Checked = true;

        var noneCheckBox = GetCheckBox(borderSidesEditorUI, "_noneCheckBox");
        var topCheckBox = GetCheckBox(borderSidesEditorUI, "_topCheckBox");
        var bottomCheckBox = GetCheckBox(borderSidesEditorUI, "_bottomCheckBox");
        var leftCheckBox = GetCheckBox(borderSidesEditorUI, "_leftCheckBox");
        var rightCheckBox = GetCheckBox(borderSidesEditorUI, "_rightCheckBox");

        Assert.True(allCheckBox.Checked);
        Assert.False(noneCheckBox.Checked);
        Assert.True(topCheckBox.Checked);
        Assert.True(bottomCheckBox.Checked);
        Assert.True(leftCheckBox.Checked);
        Assert.True(rightCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_AllCheckBox_Unchecked_DoesNotChangeSides()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.None]);

        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");
        var topCheckBox = GetCheckBox(borderSidesEditorUI, "_topCheckBox");

        // After Start(None), allCheckBox is unchecked. Unchecking it (no-op branch)
        // must not affect the side checkboxes.
        bool topBefore = topCheckBox.Checked;
        allCheckBox.Checked = false;

        Assert.Equal(topBefore, topCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_TopCheckBox_Checked_UnchecksNone()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.None]);

        var topCheckBox = GetCheckBox(borderSidesEditorUI, "_topCheckBox");
        topCheckBox.Checked = true;

        var noneCheckBox = GetCheckBox(borderSidesEditorUI, "_noneCheckBox");
        Assert.False(noneCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_TopCheckBox_Unchecked_UnchecksAll()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.All]);

        var topCheckBox = GetCheckBox(borderSidesEditorUI, "_topCheckBox");
        topCheckBox.Checked = false;

        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");
        Assert.False(allCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_BottomCheckBox_Checked_UnchecksNone()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.None]);

        var bottomCheckBox = GetCheckBox(borderSidesEditorUI, "_bottomCheckBox");
        bottomCheckBox.Checked = true;

        var noneCheckBox = GetCheckBox(borderSidesEditorUI, "_noneCheckBox");
        Assert.False(noneCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_BottomCheckBox_Unchecked_UnchecksAll()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.All]);

        var bottomCheckBox = GetCheckBox(borderSidesEditorUI, "_bottomCheckBox");
        bottomCheckBox.Checked = false;

        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");
        Assert.False(allCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_LeftCheckBox_Checked_UnchecksNone()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.None]);

        var leftCheckBox = GetCheckBox(borderSidesEditorUI, "_leftCheckBox");
        leftCheckBox.Checked = true;

        var noneCheckBox = GetCheckBox(borderSidesEditorUI, "_noneCheckBox");
        Assert.False(noneCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_LeftCheckBox_Unchecked_UnchecksAll()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.All]);

        var leftCheckBox = GetCheckBox(borderSidesEditorUI, "_leftCheckBox");
        leftCheckBox.Checked = false;

        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");
        Assert.False(allCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_RightCheckBox_Checked_UnchecksNone()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.None]);

        var rightCheckBox = GetCheckBox(borderSidesEditorUI, "_rightCheckBox");
        rightCheckBox.Checked = true;

        var noneCheckBox = GetCheckBox(borderSidesEditorUI, "_noneCheckBox");
        Assert.False(noneCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_RightCheckBox_Unchecked_UnchecksAll()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.All]);

        var rightCheckBox = GetCheckBox(borderSidesEditorUI, "_rightCheckBox");
        rightCheckBox.Checked = false;

        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");
        Assert.False(allCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_TopCheckBox_Unchecked_NoAllChecked_DoesNothing()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.None]);

        var topCheckBox = GetCheckBox(borderSidesEditorUI, "_topCheckBox");
        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");

        // First, uncheck the top without all being checked
        topCheckBox.Checked = false;

        Assert.False(allCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_BottomCheckBox_Unchecked_NoAllChecked_DoesNothing()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.None]);

        var bottomCheckBox = GetCheckBox(borderSidesEditorUI, "_bottomCheckBox");
        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");

        bottomCheckBox.Checked = false;

        Assert.False(allCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_LeftCheckBox_Unchecked_NoAllChecked_DoesNothing()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.None]);

        var leftCheckBox = GetCheckBox(borderSidesEditorUI, "_leftCheckBox");
        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");

        leftCheckBox.Checked = false;

        Assert.False(allCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_RightCheckBox_Unchecked_NoAllChecked_DoesNothing()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.None]);

        var rightCheckBox = GetCheckBox(borderSidesEditorUI, "_rightCheckBox");
        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");

        rightCheckBox.Checked = false;

        Assert.False(allCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_UpdateCurrentValue_NotStarted_DoesNotChangeValue()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo updateCurrentValue = GetBorderSidesEditorUIType()
            .GetMethod("UpdateCurrentValue", NonPublicInstance);

        updateCurrentValue.Invoke(borderSidesEditorUI, null);

        PropertyInfo valueProperty = GetBorderSidesEditorUIType().GetProperty("Value", PublicInstance);
        Assert.Null(valueProperty.GetValue(borderSidesEditorUI));
    }

    [Fact]
    public void BorderSidesEditorUI_UpdateCurrentValue_AllChecked_SetsValueToAll()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.None]);

        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");
        allCheckBox.Checked = true;

        PropertyInfo valueProperty = GetBorderSidesEditorUIType().GetProperty("Value", PublicInstance);
        Assert.Equal(ToolStripStatusLabelBorderSides.All, valueProperty.GetValue(borderSidesEditorUI));
    }

    [Fact]
    public void BorderSidesEditorUI_Start_All_SetsAllCheckboxes()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.All]);

        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");
        var topCheckBox = GetCheckBox(borderSidesEditorUI, "_topCheckBox");
        var bottomCheckBox = GetCheckBox(borderSidesEditorUI, "_bottomCheckBox");
        var leftCheckBox = GetCheckBox(borderSidesEditorUI, "_leftCheckBox");
        var rightCheckBox = GetCheckBox(borderSidesEditorUI, "_rightCheckBox");

        Assert.True(allCheckBox.Checked);
        Assert.True(topCheckBox.Checked);
        Assert.True(bottomCheckBox.Checked);
        Assert.True(leftCheckBox.Checked);
        Assert.True(rightCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_Start_None_SetsOnlyNoneCheckBox()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.None]);

        var noneCheckBox = GetCheckBox(borderSidesEditorUI, "_noneCheckBox");
        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");
        var topCheckBox = GetCheckBox(borderSidesEditorUI, "_topCheckBox");
        var bottomCheckBox = GetCheckBox(borderSidesEditorUI, "_bottomCheckBox");
        var leftCheckBox = GetCheckBox(borderSidesEditorUI, "_leftCheckBox");
        var rightCheckBox = GetCheckBox(borderSidesEditorUI, "_rightCheckBox");

        Assert.True(noneCheckBox.Checked);
        Assert.False(allCheckBox.Checked);
        Assert.False(topCheckBox.Checked);
        Assert.False(bottomCheckBox.Checked);
        Assert.False(leftCheckBox.Checked);
        Assert.False(rightCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_Start_Top_SetsOnlyTop()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.Top]);

        var noneCheckBox = GetCheckBox(borderSidesEditorUI, "_noneCheckBox");
        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");
        var topCheckBox = GetCheckBox(borderSidesEditorUI, "_topCheckBox");
        var bottomCheckBox = GetCheckBox(borderSidesEditorUI, "_bottomCheckBox");
        var leftCheckBox = GetCheckBox(borderSidesEditorUI, "_leftCheckBox");
        var rightCheckBox = GetCheckBox(borderSidesEditorUI, "_rightCheckBox");

        Assert.False(noneCheckBox.Checked);
        Assert.True(topCheckBox.Checked);
        Assert.False(bottomCheckBox.Checked);
        Assert.False(leftCheckBox.Checked);
        Assert.False(rightCheckBox.Checked);
        Assert.False(allCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_Start_Bottom_SetsOnlyBottom()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.Bottom]);

        var bottomCheckBox = GetCheckBox(borderSidesEditorUI, "_bottomCheckBox");
        var topCheckBox = GetCheckBox(borderSidesEditorUI, "_topCheckBox");
        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");
        var noneCheckBox = GetCheckBox(borderSidesEditorUI, "_noneCheckBox");

        Assert.True(bottomCheckBox.Checked);
        Assert.False(topCheckBox.Checked);
        Assert.False(allCheckBox.Checked);
        Assert.False(noneCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_Start_Left_SetsOnlyLeft()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.Left]);

        var leftCheckBox = GetCheckBox(borderSidesEditorUI, "_leftCheckBox");
        var topCheckBox = GetCheckBox(borderSidesEditorUI, "_topCheckBox");
        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");
        var noneCheckBox = GetCheckBox(borderSidesEditorUI, "_noneCheckBox");

        Assert.True(leftCheckBox.Checked);
        Assert.False(topCheckBox.Checked);
        Assert.False(allCheckBox.Checked);
        Assert.False(noneCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_Start_Right_SetsOnlyRight()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.Right]);

        var rightCheckBox = GetCheckBox(borderSidesEditorUI, "_rightCheckBox");
        var topCheckBox = GetCheckBox(borderSidesEditorUI, "_topCheckBox");
        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");
        var noneCheckBox = GetCheckBox(borderSidesEditorUI, "_noneCheckBox");

        Assert.True(rightCheckBox.Checked);
        Assert.False(topCheckBox.Checked);
        Assert.False(allCheckBox.Checked);
        Assert.False(noneCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_Start_TopBottomLeftRight_DoesNotCascadeToAll()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object,
            ToolStripStatusLabelBorderSides.Top | ToolStripStatusLabelBorderSides.Bottom |
            ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Right]);

        var topCheckBox = GetCheckBox(borderSidesEditorUI, "_topCheckBox");
        var bottomCheckBox = GetCheckBox(borderSidesEditorUI, "_bottomCheckBox");
        var leftCheckBox = GetCheckBox(borderSidesEditorUI, "_leftCheckBox");
        var rightCheckBox = GetCheckBox(borderSidesEditorUI, "_rightCheckBox");

        Assert.True(topCheckBox.Checked);
        Assert.True(bottomCheckBox.Checked);
        Assert.True(leftCheckBox.Checked);
        Assert.True(rightCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_UpdateCurrentValue_NoneCascade_ChecksNoneCheckBox()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.Top]);

        // Uncheck top - the value cascades to None, which should re-check _noneCheckBox
        var topCheckBox = GetCheckBox(borderSidesEditorUI, "_topCheckBox");
        topCheckBox.Checked = false;

        var noneCheckBox = GetCheckBox(borderSidesEditorUI, "_noneCheckBox");
        Assert.True(noneCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_UpdateCurrentValue_AllSidesCascade_ChecksAllCheckBox()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.None]);

        var topCheckBox = GetCheckBox(borderSidesEditorUI, "_topCheckBox");
        var bottomCheckBox = GetCheckBox(borderSidesEditorUI, "_bottomCheckBox");
        var leftCheckBox = GetCheckBox(borderSidesEditorUI, "_leftCheckBox");
        var rightCheckBox = GetCheckBox(borderSidesEditorUI, "_rightCheckBox");

        topCheckBox.Checked = true;
        bottomCheckBox.Checked = true;
        leftCheckBox.Checked = true;
        // After all four are checked, the cascade should re-check _allCheckBox.
        rightCheckBox.Checked = true;

        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");
        Assert.True(allCheckBox.Checked);
    }

    [Fact]
    public void BorderSidesEditorUI_UpdateCurrentValue_AllChecked_SetsValueToAllSides()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.None]);

        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");
        allCheckBox.Checked = true;

        PropertyInfo valueProperty = GetBorderSidesEditorUIType().GetProperty("Value", PublicInstance);
        Assert.Equal(ToolStripStatusLabelBorderSides.All, valueProperty.GetValue(borderSidesEditorUI));
    }

    [Fact]
    public void BorderSidesEditorUI_UpdateCurrentValue_OnlyNoneChecked_SetsValueToNone()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.Top]);

        // Uncheck all sides so the value cascades to None
        var topCheckBox = GetCheckBox(borderSidesEditorUI, "_topCheckBox");
        topCheckBox.Checked = false;

        PropertyInfo valueProperty = GetBorderSidesEditorUIType().GetProperty("Value", PublicInstance);
        Assert.Equal(ToolStripStatusLabelBorderSides.None, valueProperty.GetValue(borderSidesEditorUI));
    }

    [Fact]
    public void BorderSidesEditorUI_UpdateCurrentValue_TopChecked_SetsValueToTop()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.None]);

        var topCheckBox = GetCheckBox(borderSidesEditorUI, "_topCheckBox");
        topCheckBox.Checked = true;

        PropertyInfo valueProperty = GetBorderSidesEditorUIType().GetProperty("Value", PublicInstance);
        Assert.Equal(ToolStripStatusLabelBorderSides.Top, valueProperty.GetValue(borderSidesEditorUI));
    }

    [Fact]
    public void BorderSidesEditorUI_UpdateCurrentValue_BottomChecked_SetsValueToBottom()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.None]);

        var bottomCheckBox = GetCheckBox(borderSidesEditorUI, "_bottomCheckBox");
        bottomCheckBox.Checked = true;

        PropertyInfo valueProperty = GetBorderSidesEditorUIType().GetProperty("Value", PublicInstance);
        Assert.Equal(ToolStripStatusLabelBorderSides.Bottom, valueProperty.GetValue(borderSidesEditorUI));
    }

    [Fact]
    public void BorderSidesEditorUI_UpdateCurrentValue_LeftChecked_SetsValueToLeft()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.None]);

        var leftCheckBox = GetCheckBox(borderSidesEditorUI, "_leftCheckBox");
        leftCheckBox.Checked = true;

        PropertyInfo valueProperty = GetBorderSidesEditorUIType().GetProperty("Value", PublicInstance);
        Assert.Equal(ToolStripStatusLabelBorderSides.Left, valueProperty.GetValue(borderSidesEditorUI));
    }

    [Fact]
    public void BorderSidesEditorUI_UpdateCurrentValue_RightChecked_SetsValueToRight()
    {
        object borderSidesEditorUI = CreateBorderSidesEditorUI();
        MethodInfo start = GetBorderSidesEditorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(borderSidesEditorUI, [mockEditorService.Object, ToolStripStatusLabelBorderSides.None]);

        var rightCheckBox = GetCheckBox(borderSidesEditorUI, "_rightCheckBox");
        rightCheckBox.Checked = true;

        PropertyInfo valueProperty = GetBorderSidesEditorUIType().GetProperty("Value", PublicInstance);
        Assert.Equal(ToolStripStatusLabelBorderSides.Right, valueProperty.GetValue(borderSidesEditorUI));
    }

    [WinFormsFact]
    public void BorderSidesEditorUI_NoneCheckBox_Click_ReChecksWhenNoneCheckedIsTrue()
    {
        using Control borderSidesEditorUI = CastToControl(CreateBorderSidesEditorUI());
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        Start(borderSidesEditorUI, mockEditorService.Object, ToolStripStatusLabelBorderSides.Top);

        var topCheckBox = GetCheckBox(borderSidesEditorUI, "_topCheckBox");
        topCheckBox.TestAccessor.Dynamic.OnClick(EventArgs.Empty);
        Assert.Equal(ToolStripStatusLabelBorderSides.None, GetValue(borderSidesEditorUI));

        // Freeze CheckedChanged updates so the Click handler must preserve None itself.
        End(borderSidesEditorUI);
        var noneCheckBox = GetCheckBox(borderSidesEditorUI, "_noneCheckBox");
        noneCheckBox.TestAccessor.Dynamic.OnClick(EventArgs.Empty);

        Assert.True(noneCheckBox.Checked);
        Assert.False(topCheckBox.Checked);
        Assert.False(GetCheckBox(borderSidesEditorUI, "_allCheckBox").Checked);
        Assert.Null(GetValue(borderSidesEditorUI));
    }

    [WinFormsFact]
    public void BorderSidesEditorUI_NoneCheckBox_Click_ClearsSelectedSides()
    {
        using Control borderSidesEditorUI = CastToControl(CreateBorderSidesEditorUI());
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        Start(borderSidesEditorUI, mockEditorService.Object, ToolStripStatusLabelBorderSides.All);

        var noneCheckBox = GetCheckBox(borderSidesEditorUI, "_noneCheckBox");
        noneCheckBox.TestAccessor.Dynamic.OnClick(EventArgs.Empty);

        Assert.True(noneCheckBox.Checked);
        Assert.Equal(ToolStripStatusLabelBorderSides.None, GetValue(borderSidesEditorUI));
        foreach (string field in new[] { "_allCheckBox", "_topCheckBox", "_bottomCheckBox", "_leftCheckBox", "_rightCheckBox" })
        {
            Assert.False(GetCheckBox(borderSidesEditorUI, field).Checked);
        }
    }

    [WinFormsFact]
    public void BorderSidesEditorUI_AllCheckBox_Click_ReChecksWhenAllCheckedIsTrue()
    {
        using Control borderSidesEditorUI = CastToControl(CreateBorderSidesEditorUI());
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        Start(borderSidesEditorUI, mockEditorService.Object, ToolStripStatusLabelBorderSides.None);

        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");
        allCheckBox.TestAccessor.Dynamic.OnClick(EventArgs.Empty);
        Assert.Equal(ToolStripStatusLabelBorderSides.All, GetValue(borderSidesEditorUI));

        // Freeze CheckedChanged updates so the Click handler must preserve All itself.
        End(borderSidesEditorUI);
        allCheckBox.TestAccessor.Dynamic.OnClick(EventArgs.Empty);

        Assert.True(allCheckBox.Checked);
        Assert.False(GetCheckBox(borderSidesEditorUI, "_noneCheckBox").Checked);
        foreach (string field in new[] { "_topCheckBox", "_bottomCheckBox", "_leftCheckBox", "_rightCheckBox" })
        {
            Assert.True(GetCheckBox(borderSidesEditorUI, field).Checked);
        }

        Assert.Null(GetValue(borderSidesEditorUI));
    }

    [WinFormsFact]
    public void BorderSidesEditorUI_AllCheckBox_Click_SelectsAllSides()
    {
        using Control borderSidesEditorUI = CastToControl(CreateBorderSidesEditorUI());
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        Start(borderSidesEditorUI, mockEditorService.Object, ToolStripStatusLabelBorderSides.None);

        var allCheckBox = GetCheckBox(borderSidesEditorUI, "_allCheckBox");
        allCheckBox.TestAccessor.Dynamic.OnClick(EventArgs.Empty);

        Assert.True(allCheckBox.Checked);
        Assert.False(GetCheckBox(borderSidesEditorUI, "_noneCheckBox").Checked);
        foreach (string field in new[] { "_topCheckBox", "_bottomCheckBox", "_leftCheckBox", "_rightCheckBox" })
        {
            Assert.True(GetCheckBox(borderSidesEditorUI, field).Checked);
        }

        Assert.Equal(ToolStripStatusLabelBorderSides.All, GetValue(borderSidesEditorUI));
    }

    [WinFormsFact]
    public void BorderSidesEditor_EditValue_ClickSelection_ReturnsDifferentValue()
    {
        BorderSidesEditor editor = new();
        using Form host = new();
        Mock<IWindowsFormsEditorService> service = new(MockBehavior.Strict);
        Mock<IServiceProvider> provider = new(MockBehavior.Strict);
        provider.Setup(p => p.GetService(typeof(IWindowsFormsEditorService))).Returns(service.Object);
        service.Setup(s => s.DropDownControl(It.IsAny<Control>())).Callback<Control>(control =>
        {
            host.Controls.Add(control);
            host.Show();
            GetCheckBox(control, "_leftCheckBox").TestAccessor.Dynamic.OnClick(EventArgs.Empty);
            Assert.True(GetCheckBox(control, "_leftCheckBox").Checked);
            Assert.True(GetCheckBox(control, "_topCheckBox").Checked);
        });

        Assert.Equal(ToolStripStatusLabelBorderSides.Top | ToolStripStatusLabelBorderSides.Left,
            editor.EditValue(null, provider.Object, ToolStripStatusLabelBorderSides.Top));
    }
}
