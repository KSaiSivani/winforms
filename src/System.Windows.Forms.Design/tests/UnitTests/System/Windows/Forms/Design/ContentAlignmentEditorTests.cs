// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Reflection;
using System.Windows.Forms.TestUtilities;
using Moq;
using Windows.Win32.UI.Accessibility;

namespace System.Windows.Forms.Design.Tests;

public class ContentAlignmentEditorTests
{
    private const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    private static Type GetContentUIType()
    {
        return typeof(ContentAlignmentEditor).GetNestedType("ContentUI", NonPublicInstance);
    }

    private static Type GetSelectionPanelBaseType()
    {
        return typeof(SelectionPanelBase);
    }

    private static object CreateContentUI()
    {
        Type type = GetContentUIType();
        return Activator.CreateInstance(type);
    }

    /// <summary>
    ///  Sets up the checked button independently of the alignment setter for getter tests.
    /// </summary>
    private static void SetCheckedControlDirect(object contentUI, RadioButton button)
    {
        FieldInfo field = GetSelectionPanelBaseType()
            .GetField("_checkedControl", NonPublicInstance);
        field.SetValue(contentUI, button);
    }

    private static Control CastToControl(object instance) => (Control)instance;

    private static Control GetRadioButton(object contentUI, string fieldName)
    {
        return (Control)contentUI.GetType()
            .GetField(fieldName, NonPublicInstance)
            .GetValue(contentUI);
    }

    [Fact]
    public void ContentAlignmentEditor_Ctor_Default()
    {
        ContentAlignmentEditor editor = new();
        Assert.False(editor.IsDropDownResizable);
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void ContentAlignmentEditor_GetEditStyle_Invoke_ReturnsDropDown(ITypeDescriptorContext context)
    {
        ContentAlignmentEditor editor = new();
        Assert.Equal(UITypeEditorEditStyle.DropDown, editor.GetEditStyle(context));
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void ContentAlignmentEditor_GetPaintValueSupported_Invoke_ReturnsFalse(ITypeDescriptorContext context)
    {
        ContentAlignmentEditor editor = new();
        Assert.False(editor.GetPaintValueSupported(context));
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetEditValueInvalidProviderTestData))]
    public void ContentAlignmentEditor_EditValue_InvalidProvider_ReturnsValue(IServiceProvider provider, object value)
    {
        ContentAlignmentEditor editor = new();
        Assert.Same(value, editor.EditValue(null, provider, value));
    }

    [Fact]
    public void ContentAlignmentEditor_EditValue_ValidProvider_NullValue_ReturnsValue()
    {
        ContentAlignmentEditor editor = new();
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(mockEditorService.Object)
            .Verifiable();
        mockEditorService
            .Setup(e => e.DropDownControl(It.IsAny<Control>()))
            .Verifiable();

        Assert.Null(editor.EditValue(null, mockServiceProvider.Object, null));
        mockServiceProvider.Verify(p => p.GetService(typeof(IWindowsFormsEditorService)), Times.Once());
        mockEditorService.Verify(e => e.DropDownControl(It.IsAny<Control>()), Times.Once());
    }

    [Theory]
    [InlineData(ContentAlignment.TopLeft)]
    [InlineData(ContentAlignment.TopCenter)]
    [InlineData(ContentAlignment.TopRight)]
    [InlineData(ContentAlignment.MiddleLeft)]
    [InlineData(ContentAlignment.MiddleCenter)]
    [InlineData(ContentAlignment.MiddleRight)]
    [InlineData(ContentAlignment.BottomLeft)]
    [InlineData(ContentAlignment.BottomCenter)]
    [InlineData(ContentAlignment.BottomRight)]
    public void ContentAlignmentEditor_EditValue_ValidProvider_ReturnsValue(ContentAlignment value)
    {
        ContentAlignmentEditor editor = new();
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
    }

    [Fact]
    public void ContentAlignmentEditor_EditValue_ValidProvider_NonContentAlignmentValue_ReturnsValue()
    {
        ContentAlignmentEditor editor = new();
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(mockEditorService.Object);
        mockEditorService
            .Setup(e => e.DropDownControl(It.IsAny<Control>()));

        object value = "not-a-content-alignment";
        Assert.Same(value, editor.EditValue(null, mockServiceProvider.Object, value));
    }

    [Fact]
    public void ContentAlignmentEditor_EditValue_ValidProvider_CalledTwice_ReusesContentUI()
    {
        ContentAlignmentEditor editor = new();
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(mockEditorService.Object);
        mockEditorService
            .Setup(e => e.DropDownControl(It.IsAny<Control>()));

        _ = editor.EditValue(null, mockServiceProvider.Object, ContentAlignment.TopLeft);
        FieldInfo contentUIField = typeof(ContentAlignmentEditor).GetField("_contentUI", NonPublicInstance);
        object firstContentUI = contentUIField.GetValue(editor);
        Assert.NotNull(firstContentUI);

        _ = editor.EditValue(null, mockServiceProvider.Object, ContentAlignment.TopLeft);
        object secondContentUI = contentUIField.GetValue(editor);
        Assert.Same(firstContentUI, secondContentUI);
    }

    [Fact]
    public void ContentAlignmentEditor_HasContentUIField()
    {
        FieldInfo field = typeof(ContentAlignmentEditor).GetField("_contentUI", NonPublicInstance);
        Assert.NotNull(field);
    }

    [Fact]
    public void ContentAlignmentEditor_ContentUIField_InitialValue_IsNull()
    {
        ContentAlignmentEditor editor = new();
        FieldInfo field = typeof(ContentAlignmentEditor).GetField("_contentUI", NonPublicInstance);
        object value = field.GetValue(editor);
        Assert.Null(value);
    }

    [Fact]
    public void ContentAlignmentEditor_ContentUIField_AfterInvalidProviderEditValue_RemainsNull()
    {
        ContentAlignmentEditor editor = new();
        _ = editor.EditValue(null, null, ContentAlignment.TopLeft);

        FieldInfo field = typeof(ContentAlignmentEditor).GetField("_contentUI", NonPublicInstance);
        object value = field.GetValue(editor);
        Assert.Null(value);
    }

    [Theory]
    [InlineData("_topLeft")]
    [InlineData("_topCenter")]
    [InlineData("_topRight")]
    [InlineData("_middleLeft")]
    [InlineData("_middleCenter")]
    [InlineData("_middleRight")]
    [InlineData("_bottomLeft")]
    [InlineData("_bottomCenter")]
    [InlineData("_bottomRight")]
    public void ContentAlignmentEditor_ContentUI_AccessibilityRole_IsRadioButton(string fieldName)
    {
        object contentUI = CreateContentUI();
        var item = GetRadioButton(contentUI, fieldName);

        var actual = (UIA_CONTROLTYPE_ID)(int)item.AccessibilityObject.TestAccessor.Dynamic
            .GetPropertyValue(UIA_PROPERTY_ID.UIA_ControlTypePropertyId);

        Assert.Equal(UIA_CONTROLTYPE_ID.UIA_RadioButtonControlTypeId, actual);
    }

    [Theory]
    [InlineData("_topLeft")]
    [InlineData("_topCenter")]
    [InlineData("_topRight")]
    [InlineData("_middleLeft")]
    [InlineData("_middleCenter")]
    [InlineData("_middleRight")]
    [InlineData("_bottomLeft")]
    [InlineData("_bottomCenter")]
    [InlineData("_bottomRight")]
    public void ContentAlignmentEditor_ContentUI_Appearance_IsButton(string fieldName)
    {
        object contentUI = CreateContentUI();
        var item = (RadioButton)GetRadioButton(contentUI, fieldName);
        Assert.Equal(Appearance.Button, item.Appearance);
    }

    [Theory]
    [InlineData("_topLeft", 8)]
    [InlineData("_topCenter", 0)]
    [InlineData("_topRight", 1)]
    [InlineData("_middleLeft", 2)]
    [InlineData("_middleCenter", 3)]
    [InlineData("_middleRight", 4)]
    [InlineData("_bottomLeft", 5)]
    [InlineData("_bottomCenter", 6)]
    [InlineData("_bottomRight", 7)]
    public void ContentAlignmentEditor_ContentUI_TabIndex_IsExpected(string fieldName, int expectedTabIndex)
    {
        object contentUI = CreateContentUI();
        var item = (RadioButton)GetRadioButton(contentUI, fieldName);
        Assert.Equal(expectedTabIndex, item.TabIndex);
    }

    [Fact]
    public void ContentAlignmentEditor_ContentUI_AccessibleName_IsSet()
    {
        object contentUI = CreateContentUI();
        var control = CastToControl(contentUI);
        Assert.False(string.IsNullOrEmpty(control.AccessibleName));
    }

    [Theory]
    [InlineData("_topLeft")]
    [InlineData("_topCenter")]
    [InlineData("_topRight")]
    [InlineData("_middleLeft")]
    [InlineData("_middleCenter")]
    [InlineData("_middleRight")]
    [InlineData("_bottomLeft")]
    [InlineData("_bottomCenter")]
    [InlineData("_bottomRight")]
    public void ContentAlignmentEditor_ContentUI_RadioButtons_AccessibleName_IsSet(string fieldName)
    {
        object contentUI = CreateContentUI();
        var item = (RadioButton)GetRadioButton(contentUI, fieldName);
        Assert.False(string.IsNullOrEmpty(item.AccessibleName));
    }

    [Fact]
    public void ContentAlignmentEditor_ContentUI_BackColor_IsControl()
    {
        object contentUI = CreateContentUI();
        var control = CastToControl(contentUI);
        Assert.Equal(SystemColors.Control, control.BackColor);
    }

    [Fact]
    public void ContentAlignmentEditor_ContentUI_ForeColor_IsControlText()
    {
        object contentUI = CreateContentUI();
        var control = CastToControl(contentUI);
        Assert.Equal(SystemColors.ControlText, control.ForeColor);
    }

    [Fact]
    public void ContentAlignmentEditor_ContentUI_Size_IsNonZero()
    {
        object contentUI = CreateContentUI();
        var control = CastToControl(contentUI);
        Assert.True(control.Width > 0);
        Assert.True(control.Height > 0);
    }

    [Fact]
    public void ContentAlignmentEditor_ContentUI_AllRadioButtons_AddedToControls()
    {
        object contentUI = CreateContentUI();
        var control = CastToControl(contentUI);
        Assert.True(control.Controls.Count >= 9);
    }

    [Fact]
    public void ContentAlignmentEditor_ContentUI_SelectionOptions_ReturnsControls()
    {
        object contentUI = CreateContentUI();
        PropertyInfo property = GetContentUIType().GetProperty("SelectionOptions", NonPublicInstance);
        var options = (Control.ControlCollection)property.GetValue(contentUI);
        var control = CastToControl(contentUI);
        Assert.Same(control.Controls, options);
    }

    [Fact]
    public void ContentAlignmentEditor_ContentUI_RadioButtons_AutoCheckIsFalse()
    {
        object contentUI = CreateContentUI();
        var topLeft = (RadioButton)GetRadioButton(contentUI, "_topLeft");
        Assert.False(topLeft.AutoCheck);
    }

    [WinFormsTheory]
    [InlineData(192, 250, 178, 64, 128, 198, 118, 48, 50)]
    [InlineData(144, 188, 134, 48, 96, 148, 88, 36, 38)]
    public void ContentAlignmentEditor_ContentUI_RescaleConstantsForDpi_DifferentDpi_ResetsAnchorAndSizes(
        int dpi, int width, int height, int column, int bottom, int right, int centerWidth, int sideWidth, int buttonHeight)
    {
        using Control control = CastToControl(CreateContentUI());
        MethodInfo rescaleMethod = GetContentUIType().GetMethod("RescaleConstantsForDpi", NonPublicInstance);
        rescaleMethod.Invoke(control, [96, 96]);
        Rectangle[] originalBounds = control.Controls.Cast<Control>().Select(c => c.Bounds).ToArray();

        rescaleMethod.Invoke(control, [96, dpi]);

        Assert.Equal(new Size(width, height), control.Size);
        Assert.Equal(new Rectangle(column, bottom, centerWidth, buttonHeight), GetRadioButton(control, "_bottomCenter").Bounds);
        Assert.Equal(new Rectangle(right, 0, sideWidth, buttonHeight), GetRadioButton(control, "_topRight").Bounds);
        Assert.Equal(new Size(sideWidth, buttonHeight), GetRadioButton(control, "_topLeft").Size);
        foreach (string row in new[] { "_top", "_middle", "_bottom" })
        {
            Assert.Equal(AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, GetRadioButton(control, row + "Center").Anchor);
            Assert.Equal(AnchorStyles.Top | AnchorStyles.Right, GetRadioButton(control, row + "Right").Anchor);
        }

        rescaleMethod.Invoke(control, [dpi, 96]);

        Assert.Equal(new Size(125, 89), control.Size);
        Assert.Equal(originalBounds, control.Controls.Cast<Control>().Select(c => c.Bounds).ToArray());
        foreach (string row in new[] { "_top", "_middle", "_bottom" })
        {
            Assert.Equal(AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, GetRadioButton(control, row + "Center").Anchor);
            Assert.Equal(AnchorStyles.Top | AnchorStyles.Right, GetRadioButton(control, row + "Right").Anchor);
        }
    }

    [WinFormsFact]
    public void ContentAlignmentEditor_ContentUI_RescaleConstantsForDpi_SameDpi_ResetsAnchor()
    {
        using Control control = CastToControl(CreateContentUI());

        MethodInfo rescaleMethod = GetContentUIType()
            .GetMethod("RescaleConstantsForDpi", NonPublicInstance);

        rescaleMethod.Invoke(control, [96, 96]);

        Assert.Equal(new Size(125, 89), control.Size);
        Assert.Equal(new Rectangle(32, 64, 59, 25), GetRadioButton(control, "_bottomCenter").Bounds);
        Assert.Equal(AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, GetRadioButton(control, "_bottomCenter").Anchor);
        Assert.Equal(AnchorStyles.Top | AnchorStyles.Right, GetRadioButton(control, "_bottomRight").Anchor);
    }

    [Fact]
    public void ContentAlignmentEditor_ContentUI_ResetAnchorStyle_TrueResetsAnchors()
    {
        object contentUI = CreateContentUI();

        MethodInfo resetAnchorMethod = GetContentUIType()
            .GetMethod("ResetAnchorStyle", NonPublicInstance);

        resetAnchorMethod.Invoke(contentUI, [true]);

        var topLeft = (RadioButton)GetRadioButton(contentUI, "_topLeft");
        var topCenter = (RadioButton)GetRadioButton(contentUI, "_topCenter");
        var topRight = (RadioButton)GetRadioButton(contentUI, "_topRight");
        var middleLeft = (RadioButton)GetRadioButton(contentUI, "_middleLeft");
        var middleCenter = (RadioButton)GetRadioButton(contentUI, "_middleCenter");
        var middleRight = (RadioButton)GetRadioButton(contentUI, "_middleRight");
        var bottomLeft = (RadioButton)GetRadioButton(contentUI, "_bottomLeft");
        var bottomCenter = (RadioButton)GetRadioButton(contentUI, "_bottomCenter");
        var bottomRight = (RadioButton)GetRadioButton(contentUI, "_bottomRight");

        // TopLeft, MiddleLeft, BottomLeft are not changed.
        Assert.Equal(AnchorStyles.Top | AnchorStyles.Left, topLeft.Anchor);
        Assert.Equal(AnchorStyles.Top | AnchorStyles.Left, middleLeft.Anchor);
        Assert.Equal(AnchorStyles.Top | AnchorStyles.Left, bottomLeft.Anchor);

        // Center and Right columns are reset to None.
        Assert.Equal(AnchorStyles.None, topCenter.Anchor);
        Assert.Equal(AnchorStyles.None, topRight.Anchor);
        Assert.Equal(AnchorStyles.None, middleCenter.Anchor);
        Assert.Equal(AnchorStyles.None, middleRight.Anchor);
        Assert.Equal(AnchorStyles.None, bottomCenter.Anchor);
        Assert.Equal(AnchorStyles.None, bottomRight.Anchor);
    }

    [Fact]
    public void ContentAlignmentEditor_ContentUI_ResetAnchorStyle_FalseSetsDefaultAnchors()
    {
        object contentUI = CreateContentUI();

        MethodInfo resetAnchorMethod = GetContentUIType()
            .GetMethod("ResetAnchorStyle", NonPublicInstance);

        // First clear anchors
        resetAnchorMethod.Invoke(contentUI, [true]);
        // Then set them back
        resetAnchorMethod.Invoke(contentUI, [false]);

        var topCenter = (RadioButton)GetRadioButton(contentUI, "_topCenter");
        var topRight = (RadioButton)GetRadioButton(contentUI, "_topRight");
        var middleCenter = (RadioButton)GetRadioButton(contentUI, "_middleCenter");
        var middleRight = (RadioButton)GetRadioButton(contentUI, "_middleRight");
        var bottomCenter = (RadioButton)GetRadioButton(contentUI, "_bottomCenter");
        var bottomRight = (RadioButton)GetRadioButton(contentUI, "_bottomRight");

        Assert.Equal(AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, topCenter.Anchor);
        Assert.Equal(AnchorStyles.Top | AnchorStyles.Right, topRight.Anchor);
        Assert.Equal(AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, middleCenter.Anchor);
        Assert.Equal(AnchorStyles.Top | AnchorStyles.Right, middleRight.Anchor);
        Assert.Equal(AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, bottomCenter.Anchor);
        Assert.Equal(AnchorStyles.Top | AnchorStyles.Right, bottomRight.Anchor);
    }

    [Fact]
    public void ContentAlignmentEditor_ContentUI_ResetAnchorStyle_DefaultParamValue()
    {
        object contentUI = CreateContentUI();

        MethodInfo resetAnchorMethod = GetContentUIType()
            .GetMethod("ResetAnchorStyle", NonPublicInstance);

        // No parameter passed -> default value is false -> sets default anchors
        resetAnchorMethod.Invoke(contentUI, [false]);

        var topCenter = (RadioButton)GetRadioButton(contentUI, "_topCenter");
        Assert.Equal(AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, topCenter.Anchor);
    }

    [Fact]
    public void ContentAlignmentEditor_ContentUI_SetDimensions_DefaultDpi_SetsExpectedSize()
    {
        object contentUI = CreateContentUI();
        var control = CastToControl(contentUI);

        // Constructor calls SetDimensions(InitialSystemDpi), so size should be non-zero.
        Assert.True(control.Width > 0);
        Assert.True(control.Height > 0);
    }

    [Fact]
    public void ContentAlignmentEditor_ContentUI_SetDimensions_DifferentDpi_ResizesControls()
    {
        object contentUI = CreateContentUI();
        var control = CastToControl(contentUI);

        MethodInfo setDimensions = GetContentUIType()
            .GetMethod("SetDimensions", NonPublicInstance);

        setDimensions.Invoke(contentUI, [96]);
        Size sizeAt96 = control.Size;

        setDimensions.Invoke(contentUI, [192]);
        Size sizeAt192 = control.Size;

        Assert.True(sizeAt192.Width > sizeAt96.Width);
        Assert.True(sizeAt192.Height > sizeAt96.Height);
    }

    [Fact]
    public void ContentAlignmentEditor_ContentUI_SetDimensions_AddsRadioButtonsToControls()
    {
        object contentUI = CreateContentUI();
        var control = CastToControl(contentUI);

        MethodInfo setDimensions = GetContentUIType()
            .GetMethod("SetDimensions", NonPublicInstance);

        setDimensions.Invoke(contentUI, [96]);
        Assert.True(control.Controls.Count >= 9);
    }

    [Fact]
    public void ContentAlignmentEditor_ContentUI_SetDimensions_UpdatesRadioButtonSizes()
    {
        object contentUI = CreateContentUI();

        MethodInfo setDimensions = GetContentUIType()
            .GetMethod("SetDimensions", NonPublicInstance);

        setDimensions.Invoke(contentUI, [96]);
        var topLeftAt96 = (RadioButton)GetRadioButton(contentUI, "_topLeft");
        Size topLeftSizeAt96 = topLeftAt96.Size;

        setDimensions.Invoke(contentUI, [192]);
        var topLeftAt192 = (RadioButton)GetRadioButton(contentUI, "_topLeft");
        Size topLeftSizeAt192 = topLeftAt192.Size;

        Assert.True(topLeftSizeAt192.Width > topLeftSizeAt96.Width);
    }

    [Theory]
    [InlineData(ContentAlignment.TopLeft, "_topLeft")]
    [InlineData(ContentAlignment.TopCenter, "_topCenter")]
    [InlineData(ContentAlignment.TopRight, "_topRight")]
    [InlineData(ContentAlignment.MiddleLeft, "_middleLeft")]
    [InlineData(ContentAlignment.MiddleCenter, "_middleCenter")]
    [InlineData(ContentAlignment.MiddleRight, "_middleRight")]
    [InlineData(ContentAlignment.BottomLeft, "_bottomLeft")]
    [InlineData(ContentAlignment.BottomCenter, "_bottomCenter")]
    [InlineData(ContentAlignment.BottomRight, "_bottomRight")]
    public void ContentAlignmentEditor_ContentUI_Align_Getter_ReturnsExpected(ContentAlignment value, string expectedField)
    {
        ArgumentNullException.ThrowIfNull(expectedField);
        object contentUI = CreateContentUI();
        SetCheckedControlDirect(contentUI, (RadioButton)GetRadioButton(contentUI, expectedField));

        PropertyInfo alignProperty = GetContentUIType().GetProperty("Align", NonPublicInstance);
        ContentAlignment actual = (ContentAlignment)alignProperty.GetValue(contentUI);
        Assert.Equal(value, actual);
    }

    [WinFormsTheory]
    [InlineData(ContentAlignment.TopLeft, "_topLeft")]
    [InlineData(ContentAlignment.TopCenter, "_topCenter")]
    [InlineData(ContentAlignment.TopRight, "_topRight")]
    [InlineData(ContentAlignment.MiddleLeft, "_middleLeft")]
    [InlineData(ContentAlignment.MiddleCenter, "_middleCenter")]
    [InlineData(ContentAlignment.MiddleRight, "_middleRight")]
    [InlineData(ContentAlignment.BottomLeft, "_bottomLeft")]
    [InlineData(ContentAlignment.BottomCenter, "_bottomCenter")]
    [InlineData(ContentAlignment.BottomRight, "_bottomRight")]
    public void ContentAlignmentEditor_ContentUI_Align_Setter_SetsExpectedControl(ContentAlignment value, string expectedField)
    {
        using Control contentUI = CastToControl(CreateContentUI());
        using Form host = new();
        host.Controls.Add(contentUI);
        var panel = (SelectionPanelBase)contentUI;
        Mock<IWindowsFormsEditorService> service = new(MockBehavior.Strict);
        panel.Start(service.Object, ContentAlignment.MiddleCenter);
        host.Show();

        GetContentUIType().GetProperty("Align", NonPublicInstance).SetValue(contentUI, value);

        var expected = (RadioButton)GetRadioButton(contentUI, expectedField);
        Assert.True(expected.Focused);
        Assert.Same(expected, typeof(SelectionPanelBase).GetProperty("CheckedControl", NonPublicInstance).GetValue(contentUI));
        service.Verify(s => s.CloseDropDown(), Times.Never());
    }

    [Theory]
    [InlineData("_topRight", "_middleRight")]
    [InlineData("_middleRight", "_bottomRight")]
    [InlineData("_topCenter", "_middleCenter")]
    [InlineData("_middleCenter", "_bottomCenter")]
    [InlineData("_topLeft", "_middleLeft")]
    [InlineData("_middleLeft", "_bottomLeft")]
    public void ContentAlignmentEditor_ContentUI_ProcessDownKey_MovesToLowerRow(string from, string expected)
    {
        AssertNavigation("ProcessDownKey", from, expected);
    }

    [Theory]
    [InlineData("_bottomRight", "_middleRight")]
    [InlineData("_middleRight", "_topRight")]
    [InlineData("_bottomCenter", "_middleCenter")]
    [InlineData("_middleCenter", "_topCenter")]
    [InlineData("_bottomLeft", "_middleLeft")]
    [InlineData("_middleLeft", "_topLeft")]
    public void ContentAlignmentEditor_ContentUI_ProcessUpKey_MovesToHigherRow(string from, string expected)
    {
        AssertNavigation("ProcessUpKey", from, expected);
    }

    [Theory]
    [InlineData("_bottomLeft", "_bottomCenter")]
    [InlineData("_middleLeft", "_middleCenter")]
    [InlineData("_topLeft", "_topCenter")]
    [InlineData("_bottomCenter", "_bottomRight")]
    [InlineData("_middleCenter", "_middleRight")]
    [InlineData("_topCenter", "_topRight")]
    public void ContentAlignmentEditor_ContentUI_ProcessRightKey_MovesToRightColumn(string from, string expected)
    {
        AssertNavigation("ProcessRightKey", from, expected);
    }

    [Theory]
    [InlineData("_bottomRight", "_bottomCenter")]
    [InlineData("_middleRight", "_middleCenter")]
    [InlineData("_topRight", "_topCenter")]
    [InlineData("_bottomCenter", "_bottomLeft")]
    [InlineData("_middleCenter", "_middleLeft")]
    [InlineData("_topCenter", "_topLeft")]
    public void ContentAlignmentEditor_ContentUI_ProcessLeftKey_MovesToLeftColumn(string from, string expected)
    {
        AssertNavigation("ProcessLeftKey", from, expected);
    }

    [Theory]
    [InlineData("_bottomLeft", "_bottomLeft")]
    [InlineData("_bottomRight", "_bottomRight")]
    [InlineData("_bottomCenter", "_bottomCenter")]
    public void ContentAlignmentEditor_ContentUI_ProcessDownKey_OnBottomRow_NoChange(string from, string expected)
    {
        AssertNavigation("ProcessDownKey", from, expected);
    }

    [Theory]
    [InlineData("_topLeft", "_topLeft")]
    [InlineData("_topRight", "_topRight")]
    [InlineData("_topCenter", "_topCenter")]
    public void ContentAlignmentEditor_ContentUI_ProcessUpKey_OnTopRow_NoChange(string from, string expected)
    {
        AssertNavigation("ProcessUpKey", from, expected);
    }

    [Theory]
    [InlineData("_bottomRight", "_bottomRight")]
    [InlineData("_middleRight", "_middleRight")]
    [InlineData("_topRight", "_topRight")]
    public void ContentAlignmentEditor_ContentUI_ProcessRightKey_OnRightColumn_NoChange(string from, string expected)
    {
        AssertNavigation("ProcessRightKey", from, expected);
    }

    [Theory]
    [InlineData("_bottomLeft", "_bottomLeft")]
    [InlineData("_middleLeft", "_middleLeft")]
    [InlineData("_topLeft", "_topLeft")]
    public void ContentAlignmentEditor_ContentUI_ProcessLeftKey_OnLeftColumn_NoChange(string from, string expected)
    {
        AssertNavigation("ProcessLeftKey", from, expected);
    }

    [WinFormsTheory]
    [InlineData(ContentAlignment.TopLeft, Keys.Tab, "_topCenter")]
    [InlineData(ContentAlignment.BottomRight, Keys.Tab, "_topLeft")]
    [InlineData(ContentAlignment.BottomRight, Keys.Tab | Keys.Shift, "_bottomCenter")]
    [InlineData(ContentAlignment.TopCenter, Keys.Tab | Keys.Shift, "_topLeft")]
    public void ContentAlignmentEditor_ContentUI_ProcessDialogKey_Tab_MovesFocus(
        ContentAlignment initial, Keys keyData, string expectedField)
    {
        using Control contentUI = CastToControl(CreateContentUI());
        using Form host = new();
        host.Controls.Add(contentUI);
        Mock<IWindowsFormsEditorService> service = new(MockBehavior.Strict);
        ((SelectionPanelBase)contentUI).Start(service.Object, initial);
        host.Show();
        var source = (RadioButton)typeof(SelectionPanelBase).GetProperty("CheckedControl", NonPublicInstance).GetValue(contentUI);
        Assert.True(source.Focus());
        Assert.True(source.Focused);

        Assert.True((bool)contentUI.TestAccessor.Dynamic.ProcessDialogKey(keyData));

        Assert.True(GetRadioButton(contentUI, expectedField).Focused);
        Assert.False(source.Focused);
        service.Verify(s => s.CloseDropDown(), Times.Never());
    }

    [WinFormsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ContentAlignmentEditor_EditValue_SelectionCommitsDifferentValue(bool useEnter)
    {
        ContentAlignmentEditor editor = new();
        using Form host = new();
        Mock<IWindowsFormsEditorService> service = new(MockBehavior.Strict);
        Mock<IServiceProvider> provider = new(MockBehavior.Strict);
        provider.Setup(p => p.GetService(typeof(IWindowsFormsEditorService))).Returns(service.Object);
        service.Setup(s => s.CloseDropDown());
        service.Setup(s => s.DropDownControl(It.IsAny<Control>())).Callback<Control>(control =>
        {
            host.Controls.Add(control);
            host.Show();
            var target = (RadioButton)GetRadioButton(control, "_bottomRight");
            if (useEnter)
            {
                typeof(SelectionPanelBase).GetProperty("CheckedControl", NonPublicInstance).SetValue(control, target);
                Assert.True(target.Focused);
                typeof(Control).GetMethod("OnKeyDown", NonPublicInstance).Invoke(target, [new KeyEventArgs(Keys.Enter)]);
            }
            else
            {
                target.PerformClick();
            }
        });

        Assert.Equal(ContentAlignment.BottomRight, editor.EditValue(null, provider.Object, ContentAlignment.TopLeft));
        service.Verify(s => s.CloseDropDown(), Times.Once());
    }

    private static void AssertNavigation(string methodName, string fromField, string expectedField)
    {
        // The navigation methods (ProcessUpKey / ProcessDownKey / ProcessLeftKey /
        // ProcessRightKey) do NOT call the CheckedControl setter or Focus; they
        // simply return a reference to one of the existing radio buttons. A
        // detached ContentUI is sufficient.
        object contentUI = CreateContentUI();
        RadioButton fromButton = (RadioButton)GetRadioButton(contentUI, fromField);

        MethodInfo method = GetContentUIType()
            .GetMethod(methodName, NonPublicInstance);

        RadioButton result = (RadioButton)method.Invoke(contentUI, [fromButton]);
        Assert.Equal(expectedField, result.Name);
    }
}
