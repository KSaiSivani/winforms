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

public class DockEditorTests
{
    private const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

    private static Type GetDockUIType()
    {
        return typeof(DockEditor).GetNestedType("DockUI", NonPublicInstance);
    }

    private static Type GetContainerPlaceholderType()
    {
        return GetDockUIType()
            .GetNestedType("ContainerPlaceholder", NonPublicInstance);
    }

    private static object CreateDockUI()
    {
        return Activator.CreateInstance(GetDockUIType());
    }

    private static Control CastToControl(object instance) => (Control)instance;

    private static Control GetField(object dockUI, string fieldName)
    {
        return (Control)dockUI.GetType()
            .GetField(fieldName, NonPublicInstance)
            .GetValue(dockUI);
    }

    /// <summary>
    ///  Sets up the checked button independently of the dock-style setter for getter tests.
    /// </summary>
    private static void SetCheckedControlDirect(object dockUI, RadioButton button)
    {
        FieldInfo field = typeof(SelectionPanelBase)
            .GetField("_checkedControl", NonPublicInstance);
        field.SetValue(dockUI, button);
    }

    [Fact]
    public void DockEditor_Ctor_Default()
    {
        DockEditor editor = new();
        Assert.False(editor.IsDropDownResizable);
    }

    public static IEnumerable<object[]> EditValue_TestData()
    {
        yield return new object[] { null };
        yield return new object[] { "value" };
        yield return new object[] { DockStyle.Top };
        yield return new object[] { new() };
    }

    [Theory]
    [MemberData(nameof(EditValue_TestData))]
    public void DockEditor_EditValue_ValidProvider_ReturnsValue(object value)
    {
        DockEditor editor = new();
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
    public void DockEditor_EditValue_InvalidProvider_ReturnsValue(IServiceProvider provider, object value)
    {
        DockEditor editor = new();
        Assert.Same(value, editor.EditValue(null, provider, value));
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void DockEditor_GetEditStyle_Invoke_ReturnsModal(ITypeDescriptorContext context)
    {
        DockEditor editor = new();
        Assert.Equal(UITypeEditorEditStyle.DropDown, editor.GetEditStyle(context));
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void DockEditor_GetPaintValueSupported_Invoke_ReturnsFalse(ITypeDescriptorContext context)
    {
        DockEditor editor = new();
        Assert.False(editor.GetPaintValueSupported(context));
    }

    [Fact]
    public void DockEditor_HasDockUIField()
    {
        FieldInfo field = typeof(DockEditor).GetField("_dockUI", NonPublicInstance);
        Assert.NotNull(field);
    }

    [Fact]
    public void DockEditor_DockUIField_InitialValue_IsNull()
    {
        DockEditor editor = new();
        FieldInfo field = typeof(DockEditor).GetField("_dockUI", NonPublicInstance);
        Assert.Null(field.GetValue(editor));
    }

    [Fact]
    public void DockEditor_EditValue_CalledTwice_ReusesDockUI()
    {
        DockEditor editor = new();
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(mockEditorService.Object);
        mockEditorService
            .Setup(e => e.DropDownControl(It.IsAny<Control>()));

        _ = editor.EditValue(null, mockServiceProvider.Object, DockStyle.Top);
        FieldInfo field = typeof(DockEditor).GetField("_dockUI", NonPublicInstance);
        object first = field.GetValue(editor);
        Assert.NotNull(first);

        _ = editor.EditValue(null, mockServiceProvider.Object, DockStyle.Top);
        object second = field.GetValue(editor);
        Assert.Same(first, second);
    }

    [Fact]
    public void DockEditor_EditValue_InvalidProvider_DoesNotCreateDockUI()
    {
        DockEditor editor = new();
        _ = editor.EditValue(null, null, DockStyle.Top);

        FieldInfo field = typeof(DockEditor).GetField("_dockUI", NonPublicInstance);
        Assert.Null(field.GetValue(editor));
    }

    [Fact]
    public void DockUI_Constructor_CreatesInstance()
    {
        object dockUI = CreateDockUI();
        Assert.NotNull(dockUI);
    }

    [Fact]
    public void DockUI_Constructor_Size_IsNonZero()
    {
        object dockUI = CreateDockUI();
        var control = CastToControl(dockUI);
        Assert.True(control.Width > 0);
        Assert.True(control.Height > 0);
    }

    [Fact]
    public void DockUI_Constructor_BackColor_IsControl()
    {
        object dockUI = CreateDockUI();
        var control = CastToControl(dockUI);
        Assert.Equal(SystemColors.Control, control.BackColor);
    }

    [Fact]
    public void DockUI_Constructor_ForeColor_IsControlText()
    {
        object dockUI = CreateDockUI();
        var control = CastToControl(dockUI);
        Assert.Equal(SystemColors.ControlText, control.ForeColor);
    }

    [Fact]
    public void DockUI_Constructor_AccessibleName_IsSet()
    {
        object dockUI = CreateDockUI();
        var control = CastToControl(dockUI);
        Assert.False(string.IsNullOrEmpty(control.AccessibleName));
        Assert.Equal("Dock Picker", control.AccessibleName);
    }

    [Fact]
    public void DockUI_Constructor_HasContainer()
    {
        object dockUI = CreateDockUI();
        var control = CastToControl(dockUI);
        Assert.True(control.Controls.Count >= 1);
    }

    [Fact]
    public void DockUI_Container_Anchor_IsTopAndLeft()
    {
        // The container is declared with Anchor = Top | Left | Bottom | Right in the
        // field initializer, but the framework rewrites the anchor to Top | Left when
        // Dock = Fill is applied to a detached control. We assert the resulting value
        // to verify the framework is wiring it up correctly and not corrupting it.
        object dockUI = CreateDockUI();
        var container = GetField(dockUI, "_container");
        Assert.Equal(AnchorStyles.Top | AnchorStyles.Left, container.Anchor);
    }

    [Fact]
    public void DockUI_Container_Dock_IsFill()
    {
        object dockUI = CreateDockUI();
        var container = GetField(dockUI, "_container");
        Assert.Equal(DockStyle.Fill, container.Dock);
    }

    [Fact]
    public void DockUI_Container_BackColor_IsControl()
    {
        object dockUI = CreateDockUI();
        var container = GetField(dockUI, "_container");
        Assert.Equal(SystemColors.Control, container.BackColor);
    }

    [Fact]
    public void DockUI_Container_HasControls()
    {
        object dockUI = CreateDockUI();
        var container = GetField(dockUI, "_container");
        Assert.True(container.Controls.Count >= 6);
    }

    [Theory]
    [InlineData("_none", DockStyle.Bottom, "None", 0)]
    [InlineData("_right", DockStyle.Right, " ", 4)]
    [InlineData("_left", DockStyle.Left, " ", 2)]
    [InlineData("_top", DockStyle.Top, " ", 1)]
    [InlineData("_bottom", DockStyle.Bottom, " ", 5)]
    [InlineData("_fill", DockStyle.Fill, " ", 3)]
    public void DockUI_RadioButton_Dock_IsExpected(string fieldName, DockStyle expectedDock, string expectedText, int expectedTabIndex)
    {
        object dockUI = CreateDockUI();
        var button = (RadioButton)GetField(dockUI, fieldName);
        Assert.Equal(expectedDock, button.Dock);
        Assert.Equal(expectedText, button.Text);
        Assert.Equal(expectedTabIndex, button.TabIndex);
    }

    [Theory]
    [InlineData("_none")]
    [InlineData("_right")]
    [InlineData("_left")]
    [InlineData("_top")]
    [InlineData("_bottom")]
    [InlineData("_fill")]
    public void DockUI_RadioButton_TabStop_IsTrue(string fieldName)
    {
        object dockUI = CreateDockUI();
        var button = (RadioButton)GetField(dockUI, fieldName);
        Assert.True(button.TabStop);
    }

    [Theory]
    [InlineData("_none")]
    [InlineData("_right")]
    [InlineData("_left")]
    [InlineData("_top")]
    [InlineData("_bottom")]
    [InlineData("_fill")]
    public void DockUI_RadioButton_Appearance_IsButton(string fieldName)
    {
        object dockUI = CreateDockUI();
        var button = (RadioButton)GetField(dockUI, fieldName);
        Assert.Equal(Appearance.Button, button.Appearance);
    }

    [Theory]
    [InlineData("_none")]
    [InlineData("_right")]
    [InlineData("_left")]
    [InlineData("_top")]
    [InlineData("_bottom")]
    [InlineData("_fill")]
    public void DockUI_RadioButton_AccessibleName_IsSet(string fieldName)
    {
        object dockUI = CreateDockUI();
        var button = (RadioButton)GetField(dockUI, fieldName);
        Assert.False(string.IsNullOrEmpty(button.AccessibleName));
    }

    [Fact]
    public void DockUI_None_AccessibleName_IsNone()
    {
        object dockUI = CreateDockUI();
        var none = (RadioButton)GetField(dockUI, "_none");
        Assert.Equal("None", none.AccessibleName);
    }

    [Fact]
    public void DockUI_Right_AccessibleName_IsRight()
    {
        object dockUI = CreateDockUI();
        var right = (RadioButton)GetField(dockUI, "_right");
        Assert.Equal("Right", right.AccessibleName);
    }

    [Fact]
    public void DockUI_Left_AccessibleName_IsLeft()
    {
        object dockUI = CreateDockUI();
        var left = (RadioButton)GetField(dockUI, "_left");
        Assert.Equal("Left", left.AccessibleName);
    }

    [Fact]
    public void DockUI_Top_AccessibleName_IsTop()
    {
        object dockUI = CreateDockUI();
        var top = (RadioButton)GetField(dockUI, "_top");
        Assert.Equal("Top", top.AccessibleName);
    }

    [Fact]
    public void DockUI_Bottom_AccessibleName_IsBottom()
    {
        object dockUI = CreateDockUI();
        var bottom = (RadioButton)GetField(dockUI, "_bottom");
        Assert.Equal("Bottom", bottom.AccessibleName);
    }

    [Fact]
    public void DockUI_Fill_AccessibleName_IsFill()
    {
        object dockUI = CreateDockUI();
        var fill = (RadioButton)GetField(dockUI, "_fill");
        Assert.Equal("Fill", fill.AccessibleName);
    }

    [Theory]
    [InlineData("_none")]
    [InlineData("_right")]
    [InlineData("_left")]
    [InlineData("_top")]
    [InlineData("_bottom")]
    [InlineData("_fill")]
    public void DockUI_RadioButton_ControlType_IsRadioButton(string fieldName)
    {
        object dockUI = CreateDockUI();
        var item = (RadioButton)GetField(dockUI, fieldName);

        var actual = (UIA_CONTROLTYPE_ID)(int)item.AccessibilityObject.TestAccessor.Dynamic
            .GetPropertyValue(UIA_PROPERTY_ID.UIA_ControlTypePropertyId);

        Assert.Equal(UIA_CONTROLTYPE_ID.UIA_RadioButtonControlTypeId, actual);
    }

    [Fact]
    public void DockUI_SelectionOptions_ReturnsContainerControls()
    {
        object dockUI = CreateDockUI();
        PropertyInfo property = GetDockUIType().GetProperty("SelectionOptions", NonPublicInstance);
        var options = (Control.ControlCollection)property.GetValue(dockUI);
        var container = GetField(dockUI, "_container");
        Assert.Same(container.Controls, options);
    }

    [Fact]
    public void DockUI_Value_Initially_Null()
    {
        object dockUI = CreateDockUI();
        PropertyInfo property = GetDockUIType().GetProperty("Value", PublicInstance);
        Assert.Null(property.GetValue(dockUI));
    }

    [Theory]
    [InlineData(DockStyle.None)]
    [InlineData(DockStyle.Top)]
    [InlineData(DockStyle.Bottom)]
    [InlineData(DockStyle.Left)]
    [InlineData(DockStyle.Right)]
    [InlineData(DockStyle.Fill)]
    public void DockUI_Start_DockStyle_SetsValueToExpected(DockStyle value)
    {
        object dockUI = CreateDockUI();
        MethodInfo start = GetDockUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(dockUI, [mockEditorService.Object, value]);

        PropertyInfo valueProperty = GetDockUIType().GetProperty("Value", PublicInstance);
        Assert.Equal(value, valueProperty.GetValue(dockUI));
    }

    [Theory]
    [InlineData(DockStyle.None, "_none")]
    [InlineData(DockStyle.Top, "_top")]
    [InlineData(DockStyle.Bottom, "_bottom")]
    [InlineData(DockStyle.Left, "_left")]
    [InlineData(DockStyle.Right, "_right")]
    [InlineData(DockStyle.Fill, "_fill")]
    public void DockUI_Start_DockStyle_SetsExpectedCheckedControl(DockStyle value, string expectedField)
    {
        object dockUI = CreateDockUI();
        MethodInfo start = GetDockUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(dockUI, [mockEditorService.Object, value]);

        PropertyInfo checkedControlProperty = typeof(SelectionPanelBase)
            .GetProperty("CheckedControl", NonPublicInstance);
        var checkedControl = (RadioButton)checkedControlProperty.GetValue(dockUI);
        Assert.Equal(expectedField, checkedControl.Name);
    }

    [Fact]
    public void DockUI_Start_NullValue_SetsValueToNull()
    {
        object dockUI = CreateDockUI();
        MethodInfo start = GetDockUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(dockUI, [mockEditorService.Object, null]);

        PropertyInfo valueProperty = GetDockUIType().GetProperty("Value", PublicInstance);
        Assert.Null(valueProperty.GetValue(dockUI));
    }

    [Fact]
    public void DockUI_Start_NonDockStyleValue_SetsValueAndDoesNotThrow()
    {
        object dockUI = CreateDockUI();
        MethodInfo start = GetDockUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        object nonDock = "not-dock";
        start.Invoke(dockUI, [mockEditorService.Object, nonDock]);

        PropertyInfo valueProperty = GetDockUIType().GetProperty("Value", PublicInstance);
        Assert.Equal(nonDock, valueProperty.GetValue(dockUI));
    }

    [Fact]
    public void DockUI_End_SetsValueToNull()
    {
        object dockUI = CreateDockUI();
        MethodInfo start = GetDockUIType().GetMethod("Start", PublicInstance);
        MethodInfo end = GetDockUIType().GetMethod("End", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);

        start.Invoke(dockUI, [mockEditorService.Object, DockStyle.Top]);
        end.Invoke(dockUI, null);

        PropertyInfo valueProperty = GetDockUIType().GetProperty("Value", PublicInstance);
        Assert.Null(valueProperty.GetValue(dockUI));
    }

    [Fact]
    public void DockUI_OnGotFocus_DoesNotThrow()
    {
        object dockUI = CreateDockUI();
        // _checkedControl is null initially; OnGotFocus would NRE on Focus().
        // Set it via the public DockStyle setter path that uses Start.
        // The Start path internally calls Focus() but it does not throw for
        // detached radio buttons (Focus returns false silently). We then
        // exercise OnGotFocus with a real checked control in place.
        MethodInfo start = GetDockUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(dockUI, [mockEditorService.Object, DockStyle.Top]);

        MethodInfo onGotFocus = typeof(SelectionPanelBase)
            .GetMethod("OnGotFocus", NonPublicInstance);
        onGotFocus.Invoke(dockUI, [EventArgs.Empty]);
    }

    [Fact]
    public void DockUI_InitializeComponent_SetsSize()
    {
        object dockUI = CreateDockUI();
        var control = CastToControl(dockUI);
        Assert.True(control.Width > 0);
        Assert.True(control.Height > 0);
    }

    [Fact]
    public void DockUI_InitializeComponent_LaysOutContainer()
    {
        object dockUI = CreateDockUI();
        var container = GetField(dockUI, "_container");
        Assert.True(container.Width > 0);
        Assert.True(container.Height > 0);
    }

    [Fact]
    public void DockUI_InitializeComponent_ButtonsHaveNonZeroSize()
    {
        object dockUI = CreateDockUI();
        var top = (RadioButton)GetField(dockUI, "_top");
        var left = (RadioButton)GetField(dockUI, "_left");
        var right = (RadioButton)GetField(dockUI, "_right");
        var bottom = (RadioButton)GetField(dockUI, "_bottom");
        var fill = (RadioButton)GetField(dockUI, "_fill");
        var none = (RadioButton)GetField(dockUI, "_none");

        Assert.True(top.Width > 0);
        Assert.True(left.Width > 0);
        Assert.True(right.Width > 0);
        Assert.True(bottom.Width > 0);
        Assert.True(fill.Width > 0);
        Assert.True(none.Width > 0);
        Assert.True(none.Height > 0);
    }

    [Theory]
    [InlineData(DockStyle.None, "_none")]
    [InlineData(DockStyle.Top, "_top")]
    [InlineData(DockStyle.Bottom, "_bottom")]
    [InlineData(DockStyle.Left, "_left")]
    [InlineData(DockStyle.Right, "_right")]
    [InlineData(DockStyle.Fill, "_fill")]
    public void DockUI_DockStyle_Getter_ReturnsExpected(DockStyle value, string expectedField)
    {
        ArgumentNullException.ThrowIfNull(expectedField);
        object dockUI = CreateDockUI();
        SetCheckedControlDirect(dockUI, (RadioButton)GetField(dockUI, expectedField));

        PropertyInfo dockStyleProperty = GetDockUIType().GetProperty("DockStyle", NonPublicInstance);
        DockStyle actual = (DockStyle)dockStyleProperty.GetValue(dockUI);
        Assert.Equal(value, actual);
    }

    [WinFormsTheory]
    [InlineData(DockStyle.None, "_none")]
    [InlineData(DockStyle.Top, "_top")]
    [InlineData(DockStyle.Bottom, "_bottom")]
    [InlineData(DockStyle.Left, "_left")]
    [InlineData(DockStyle.Right, "_right")]
    [InlineData(DockStyle.Fill, "_fill")]
    public void DockUI_DockStyle_Setter_SetsExpectedControl(DockStyle value, string expectedField)
    {
        using Control dockUI = CastToControl(CreateDockUI());
        using Form host = new();
        host.Controls.Add(dockUI);
        var panel = (SelectionPanelBase)dockUI;
        Mock<IWindowsFormsEditorService> service = new(MockBehavior.Strict);
        panel.Start(service.Object, DockStyle.None);
        host.Show();

        GetDockUIType().GetProperty("DockStyle", NonPublicInstance).SetValue(dockUI, value);

        var expected = (RadioButton)GetField(dockUI, expectedField);
        Assert.True(expected.Focused);
        Assert.Same(expected, typeof(SelectionPanelBase).GetProperty("CheckedControl", NonPublicInstance).GetValue(dockUI));
        service.Verify(s => s.CloseDropDown(), Times.Never());
    }

    [WinFormsTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void DockEditor_EditValue_SelectionCommitsDifferentValue(bool useEnter)
    {
        DockEditor editor = new();
        using Form host = new();
        Mock<IWindowsFormsEditorService> service = new(MockBehavior.Strict);
        Mock<IServiceProvider> provider = new(MockBehavior.Strict);
        provider.Setup(p => p.GetService(typeof(IWindowsFormsEditorService))).Returns(service.Object);
        service.Setup(s => s.CloseDropDown());
        service.Setup(s => s.DropDownControl(It.IsAny<Control>())).Callback<Control>(control =>
        {
            host.Controls.Add(control);
            host.Show();
            var target = (RadioButton)GetField(control, "_fill");
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

        Assert.Equal(DockStyle.Fill, editor.EditValue(null, provider.Object, DockStyle.Top));
        service.Verify(s => s.CloseDropDown(), Times.Once());
    }

    [Theory]
    [InlineData("_top", "_fill")]
    [InlineData("_fill", "_bottom")]
    [InlineData("_bottom", "_none")]
    [InlineData("_left", "_bottom")]
    [InlineData("_right", "_bottom")]
    public void DockUI_ProcessDownKey_MovesDown(string from, string expected)
    {
        AssertNavigation("ProcessDownKey", from, expected);
    }

    [Theory]
    [InlineData("_none", "_none")]
    public void DockUI_ProcessDownKey_OnLast_NoChange(string from, string expected)
    {
        AssertNavigation("ProcessDownKey", from, expected);
    }

    [Theory]
    [InlineData("_top", "_top")]
    [InlineData("_left", "_top")]
    [InlineData("_right", "_top")]
    public void DockUI_ProcessUpKey_FromTop_NoChange(string from, string expected)
    {
        AssertNavigation("ProcessUpKey", from, expected);
    }

    [Theory]
    [InlineData("_bottom", "_fill")]
    [InlineData("_fill", "_top")]
    [InlineData("_none", "_bottom")]
    public void DockUI_ProcessUpKey_MovesUp(string from, string expected)
    {
        AssertNavigation("ProcessUpKey", from, expected);
    }

    [Theory]
    [InlineData("_left", "_fill")]
    [InlineData("_fill", "_right")]
    public void DockUI_ProcessRightKey_MovesRight(string from, string expected)
    {
        AssertNavigation("ProcessRightKey", from, expected);
    }

    [Theory]
    [InlineData("_right", "_fill")]
    [InlineData("_fill", "_left")]
    public void DockUI_ProcessLeftKey_MovesLeft(string from, string expected)
    {
        AssertNavigation("ProcessLeftKey", from, expected);
    }

    [Theory]
    [InlineData("_right", "_right")]
    public void DockUI_ProcessRightKey_OnRight_NoChange(string from, string expected)
    {
        AssertNavigation("ProcessRightKey", from, expected);
    }

    [Theory]
    [InlineData("_left", "_left")]
    public void DockUI_ProcessLeftKey_OnLeft_NoChange(string from, string expected)
    {
        AssertNavigation("ProcessLeftKey", from, expected);
    }

    [WinFormsTheory]
    [InlineData(DockStyle.Top, Keys.Tab, "_left")]
    [InlineData(DockStyle.None, Keys.Tab, "_top")]
    [InlineData(DockStyle.None, Keys.Tab | Keys.Shift, "_bottom")]
    [InlineData(DockStyle.Top, Keys.Tab | Keys.Shift, "_none")]
    public void DockUI_ProcessDialogKey_Tab_MovesFocus(DockStyle initial, Keys keyData, string expectedField)
    {
        using Control dockUI = CastToControl(CreateDockUI());
        using Form host = new();
        host.Controls.Add(dockUI);
        Mock<IWindowsFormsEditorService> service = new(MockBehavior.Strict);
        ((SelectionPanelBase)dockUI).Start(service.Object, initial);
        host.Show();
        var source = (RadioButton)typeof(SelectionPanelBase).GetProperty("CheckedControl", NonPublicInstance).GetValue(dockUI);
        Assert.True(source.Focus());
        Assert.True(source.Focused);

        Assert.True((bool)dockUI.TestAccessor.Dynamic.ProcessDialogKey(keyData));

        Assert.True(GetField(dockUI, expectedField).Focused);
        Assert.False(source.Focused);
        service.Verify(s => s.CloseDropDown(), Times.Never());
    }

    [WinFormsTheory]
    [InlineData(DockStyle.Top)]
    [InlineData(DockStyle.Bottom)]
    [InlineData(DockStyle.Left)]
    [InlineData(DockStyle.Right)]
    [InlineData(DockStyle.Fill)]
    [InlineData(DockStyle.None)]
    public void DockUI_UpdateValue_SetsValueToDockStyle(DockStyle value)
    {
        using Control dockUI = CastToControl(CreateDockUI());
        using Form host = new();
        host.Controls.Add(dockUI);
        Mock<IWindowsFormsEditorService> service = new(MockBehavior.Strict);
        ((SelectionPanelBase)dockUI).Start(service.Object, value == DockStyle.None ? DockStyle.Top : DockStyle.None);
        host.Show();
        GetDockUIType().GetProperty("DockStyle", NonPublicInstance).SetValue(dockUI, value);

        MethodInfo updateValue = GetDockUIType().GetMethod("UpdateValue", NonPublicInstance);
        updateValue.Invoke(dockUI, null);

        PropertyInfo valueProperty = GetDockUIType().GetProperty("Value", PublicInstance);
        Assert.Equal(value, valueProperty.GetValue(dockUI));
    }

    [Theory]
    [InlineData(DockStyle.Top)]
    [InlineData(DockStyle.Bottom)]
    [InlineData(DockStyle.Left)]
    [InlineData(DockStyle.Right)]
    [InlineData(DockStyle.Fill)]
    [InlineData(DockStyle.None)]
    public void DockUI_SetInitialCheckedControl_SetsExpectedControl(DockStyle value)
    {
        object dockUI = CreateDockUI();
        PropertyInfo valueProperty = GetDockUIType().GetProperty("Value", PublicInstance);
        valueProperty.SetValue(dockUI, value);

        MethodInfo setInitial = GetDockUIType().GetMethod("SetInitialCheckedControl", NonPublicInstance);
        setInitial.Invoke(dockUI, null);

        PropertyInfo checkedControlProperty = typeof(SelectionPanelBase)
            .GetProperty("CheckedControl", NonPublicInstance);
        var checkedControl = (RadioButton)checkedControlProperty.GetValue(dockUI);
        Assert.NotNull(checkedControl);
    }

    [Fact]
    public void DockUI_SetInitialCheckedControl_NonDockStyleValue_DefaultsToNone()
    {
        object dockUI = CreateDockUI();
        PropertyInfo valueProperty = GetDockUIType().GetProperty("Value", PublicInstance);
        valueProperty.SetValue(dockUI, "not-a-dock-style");

        MethodInfo setInitial = GetDockUIType().GetMethod("SetInitialCheckedControl", NonPublicInstance);
        setInitial.Invoke(dockUI, null);

        PropertyInfo checkedControlProperty = typeof(SelectionPanelBase)
            .GetProperty("CheckedControl", NonPublicInstance);
        var checkedControl = (RadioButton)checkedControlProperty.GetValue(dockUI);
        Assert.Equal("_none", checkedControl.Name);
    }

    [Fact]
    public void DockUI_ContainerPlaceholder_Constructor_SetsBackColor()
    {
        Type type = GetContainerPlaceholderType();
        var placeholder = (Control)Activator.CreateInstance(type);
        Assert.Equal(SystemColors.Control, placeholder.BackColor);
    }

    [Fact]
    public void DockUI_ContainerPlaceholder_Constructor_TabStopIsFalse()
    {
        Type type = GetContainerPlaceholderType();
        var placeholder = (Control)Activator.CreateInstance(type);
        Assert.False(placeholder.TabStop);
    }

    private static void AssertNavigation(string methodName, string fromField, string expectedField)
    {
        // The navigation methods (ProcessUpKey / ProcessDownKey / ProcessLeftKey /
        // ProcessRightKey) do NOT call the CheckedControl setter or Focus; they
        // simply return a reference to one of the existing radio buttons. A
        // detached DockUI is sufficient.
        object dockUI = CreateDockUI();
        RadioButton fromButton = (RadioButton)GetField(dockUI, fromField);

        MethodInfo method = GetDockUIType()
            .GetMethod(methodName, NonPublicInstance);

        RadioButton result = (RadioButton)method.Invoke(dockUI, [fromButton]);
        Assert.Equal(expectedField, result.Name);
    }
}
