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

public class AnchorEditorTests
{
    private const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

    private static Type GetAnchorUIType()
    {
        return typeof(AnchorEditor).GetNestedType("AnchorUI", NonPublicInstance);
    }

    private static Type GetSpringControlType()
    {
        return typeof(AnchorEditor).GetNestedType("AnchorUI", NonPublicInstance)
            .GetNestedType("SpringControl", NonPublicInstance);
    }

    private static Type GetContainerPlaceholderType()
    {
        return typeof(AnchorEditor).GetNestedType("AnchorUI", NonPublicInstance)
            .GetNestedType("ContainerPlaceholder", NonPublicInstance);
    }

    private static Type GetControlPlaceholderType()
    {
        return typeof(AnchorEditor).GetNestedType("AnchorUI", NonPublicInstance)
            .GetNestedType("ControlPlaceholder", NonPublicInstance);
    }

    private static object CreateAnchorUI()
    {
        return Activator.CreateInstance(GetAnchorUIType());
    }

    private static Control CastToControl(object instance) => (Control)instance;

    private static Control GetField(object anchorUI, string fieldName)
    {
        return (Control)anchorUI.GetType()
            .GetField(fieldName, NonPublicInstance)
            .GetValue(anchorUI);
    }

    private static object GetSpringControl(object anchorUI, string fieldName)
    {
        return GetField(anchorUI, fieldName);
    }

    private static object CreateSpringControl(object anchorUI)
    {
        return Activator.CreateInstance(GetSpringControlType(), [anchorUI]);
    }

    [Fact]
    public void AnchorEditor_Ctor_Default()
    {
        AnchorEditor editor = new();
        Assert.False(editor.IsDropDownResizable);
    }

    public static IEnumerable<object[]> EditValue_TestData()
    {
        yield return new object[] { null };
        yield return new object[] { "value" };
        yield return new object[] { AnchorStyles.Top };
        yield return new object[] { new() };
    }

    [Theory]
    [MemberData(nameof(EditValue_TestData))]
    public void AnchorEditor_EditValue_ValidProvider_ReturnsValue(object value)
    {
        AnchorEditor editor = new();
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
    public void AnchorEditor_EditValue_InvalidProvider_ReturnsValue(IServiceProvider provider, object value)
    {
        AnchorEditor editor = new();
        Assert.Same(value, editor.EditValue(null, provider, value));
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void AnchorEditor_GetEditStyle_Invoke_ReturnsModal(ITypeDescriptorContext context)
    {
        AnchorEditor editor = new();
        Assert.Equal(UITypeEditorEditStyle.DropDown, editor.GetEditStyle(context));
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void AnchorEditor_GetPaintValueSupported_Invoke_ReturnsFalse(ITypeDescriptorContext context)
    {
        AnchorEditor editor = new();
        Assert.False(editor.GetPaintValueSupported(context));
    }

    [Theory]
    [InlineData("_left")]
    [InlineData("_right")]
    [InlineData("_top")]
    [InlineData("_bottom")]
    public void AnchorEditor_AnchorUI_ControlType_IsCheckButton(string fieldName)
    {
        Type type = typeof(AnchorEditor)
            .GetNestedType("AnchorUI", BindingFlags.NonPublic | BindingFlags.Instance);
        var anchorUI = (Control)Activator.CreateInstance(type);
        var item = (Control)anchorUI.GetType()
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(anchorUI);

        var actual = (UIA_CONTROLTYPE_ID)(int)item.AccessibilityObject.TestAccessor.Dynamic
            .GetPropertyValue(UIA_PROPERTY_ID.UIA_ControlTypePropertyId);

        Assert.Equal(UIA_CONTROLTYPE_ID.UIA_CheckBoxControlTypeId, actual);
    }

    [Fact]
    public void AnchorEditor_HasAnchorUIField()
    {
        FieldInfo field = typeof(AnchorEditor).GetField("_anchorUI", NonPublicInstance);
        Assert.NotNull(field);
    }

    [Fact]
    public void AnchorEditor_AnchorUIField_InitialValue_IsNull()
    {
        AnchorEditor editor = new();
        FieldInfo field = typeof(AnchorEditor).GetField("_anchorUI", NonPublicInstance);
        Assert.Null(field.GetValue(editor));
    }

    [Fact]
    public void AnchorEditor_EditValue_CalledTwice_ReusesAnchorUI()
    {
        AnchorEditor editor = new();
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(mockEditorService.Object);
        mockEditorService
            .Setup(e => e.DropDownControl(It.IsAny<Control>()));

        _ = editor.EditValue(null, mockServiceProvider.Object, AnchorStyles.Top);
        FieldInfo field = typeof(AnchorEditor).GetField("_anchorUI", NonPublicInstance);
        object first = field.GetValue(editor);
        Assert.NotNull(first);

        _ = editor.EditValue(null, mockServiceProvider.Object, AnchorStyles.Top);
        object second = field.GetValue(editor);
        Assert.Same(first, second);
    }

    [Fact]
    public void AnchorEditor_EditValue_InvalidProvider_DoesNotCreateAnchorUI()
    {
        AnchorEditor editor = new();
        _ = editor.EditValue(null, null, AnchorStyles.Top);

        FieldInfo field = typeof(AnchorEditor).GetField("_anchorUI", NonPublicInstance);
        Assert.Null(field.GetValue(editor));
    }

    [Fact]
    public void AnchorUI_Constructor_CreatesInstance()
    {
        object anchorUI = CreateAnchorUI();
        Assert.NotNull(anchorUI);
    }

    [Fact]
    public void AnchorUI_Constructor_Size_IsNonZero()
    {
        object anchorUI = CreateAnchorUI();
        var control = CastToControl(anchorUI);
        Assert.True(control.Width > 0);
        Assert.True(control.Height > 0);
    }

    [Fact]
    public void AnchorUI_Constructor_HasContainer()
    {
        object anchorUI = CreateAnchorUI();
        var control = CastToControl(anchorUI);
        Assert.Single(control.Controls);
    }

    [Fact]
    public void AnchorUI_Constructor_AccessibleName_IsSet()
    {
        object anchorUI = CreateAnchorUI();
        var control = CastToControl(anchorUI);
        Assert.False(string.IsNullOrEmpty(control.AccessibleName));
        Assert.Equal("Anchor Editor", control.AccessibleName);
    }

    [Fact]
    public void AnchorUI_Container_Anchor_IsAllSides()
    {
        object anchorUI = CreateAnchorUI();
        var container = GetField(anchorUI, "_container");
        Assert.Equal(
            AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right,
            container.Anchor);
    }

    [Fact]
    public void AnchorUI_Container_BackColor_IsWindow()
    {
        object anchorUI = CreateAnchorUI();
        var container = GetField(anchorUI, "_container");
        Assert.Equal(SystemColors.Window, container.BackColor);
    }

    [Fact]
    public void AnchorUI_Container_ForeColor_IsWindowText()
    {
        object anchorUI = CreateAnchorUI();
        var container = GetField(anchorUI, "_container");
        Assert.Equal(SystemColors.WindowText, container.ForeColor);
    }

    [Fact]
    public void AnchorUI_Container_TabStop_IsFalse()
    {
        object anchorUI = CreateAnchorUI();
        var container = GetField(anchorUI, "_container");
        Assert.False(container.TabStop);
    }

    [Fact]
    public void AnchorUI_Container_HasControls()
    {
        object anchorUI = CreateAnchorUI();
        var container = GetField(anchorUI, "_container");
        Assert.True(container.Controls.Count >= 5);
    }

    [Fact]
    public void AnchorUI_Control_Anchor_IsAllSides()
    {
        object anchorUI = CreateAnchorUI();
        var inner = GetField(anchorUI, "_control");
        Assert.Equal(
            AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right,
            inner.Anchor);
    }

    [Fact]
    public void AnchorUI_Control_BackColor_IsControl()
    {
        object anchorUI = CreateAnchorUI();
        var inner = GetField(anchorUI, "_control");
        Assert.Equal(SystemColors.Control, inner.BackColor);
    }

    [Fact]
    public void AnchorUI_Control_TabStop_IsFalse()
    {
        object anchorUI = CreateAnchorUI();
        var inner = GetField(anchorUI, "_control");
        Assert.False(inner.TabStop);
    }

    [Fact]
    public void AnchorUI_Left_Anchor_IsLeft()
    {
        object anchorUI = CreateAnchorUI();
        var left = GetField(anchorUI, "_left");
        Assert.Equal(AnchorStyles.Left, left.Anchor);
    }

    [Fact]
    public void AnchorUI_Left_TabIndex_IsZero()
    {
        object anchorUI = CreateAnchorUI();
        var left = GetField(anchorUI, "_left");
        Assert.Equal(0, left.TabIndex);
    }

    [Fact]
    public void AnchorUI_Left_TabStop_IsTrue()
    {
        object anchorUI = CreateAnchorUI();
        var left = GetField(anchorUI, "_left");
        Assert.True(left.TabStop);
    }

    [Fact]
    public void AnchorUI_Left_AccessibleName_IsLeft()
    {
        object anchorUI = CreateAnchorUI();
        var left = GetField(anchorUI, "_left");
        Assert.Equal("Left", left.AccessibleName);
    }

    [Fact]
    public void AnchorUI_Right_Anchor_IsRight()
    {
        object anchorUI = CreateAnchorUI();
        var right = GetField(anchorUI, "_right");
        Assert.Equal(AnchorStyles.Right, right.Anchor);
    }

    [Fact]
    public void AnchorUI_Right_TabIndex_IsTwo()
    {
        object anchorUI = CreateAnchorUI();
        var right = GetField(anchorUI, "_right");
        Assert.Equal(2, right.TabIndex);
    }

    [Fact]
    public void AnchorUI_Right_TabStop_IsTrue()
    {
        object anchorUI = CreateAnchorUI();
        var right = GetField(anchorUI, "_right");
        Assert.True(right.TabStop);
    }

    [Fact]
    public void AnchorUI_Right_AccessibleName_IsRight()
    {
        object anchorUI = CreateAnchorUI();
        var right = GetField(anchorUI, "_right");
        Assert.Equal("Right", right.AccessibleName);
    }

    [Fact]
    public void AnchorUI_Top_Anchor_IsTop()
    {
        object anchorUI = CreateAnchorUI();
        var top = GetField(anchorUI, "_top");
        Assert.Equal(AnchorStyles.Top, top.Anchor);
    }

    [Fact]
    public void AnchorUI_Top_TabIndex_IsOne()
    {
        object anchorUI = CreateAnchorUI();
        var top = GetField(anchorUI, "_top");
        Assert.Equal(1, top.TabIndex);
    }

    [Fact]
    public void AnchorUI_Top_TabStop_IsTrue()
    {
        object anchorUI = CreateAnchorUI();
        var top = GetField(anchorUI, "_top");
        Assert.True(top.TabStop);
    }

    [Fact]
    public void AnchorUI_Top_AccessibleName_IsTop()
    {
        object anchorUI = CreateAnchorUI();
        var top = GetField(anchorUI, "_top");
        Assert.Equal("Top", top.AccessibleName);
    }

    [Fact]
    public void AnchorUI_Bottom_Anchor_IsBottom()
    {
        object anchorUI = CreateAnchorUI();
        var bottom = GetField(anchorUI, "_bottom");
        Assert.Equal(AnchorStyles.Bottom, bottom.Anchor);
    }

    [Fact]
    public void AnchorUI_Bottom_TabIndex_IsThree()
    {
        object anchorUI = CreateAnchorUI();
        var bottom = GetField(anchorUI, "_bottom");
        Assert.Equal(3, bottom.TabIndex);
    }

    [Fact]
    public void AnchorUI_Bottom_TabStop_IsTrue()
    {
        object anchorUI = CreateAnchorUI();
        var bottom = GetField(anchorUI, "_bottom");
        Assert.True(bottom.TabStop);
    }

    [Fact]
    public void AnchorUI_Bottom_AccessibleName_IsBottom()
    {
        object anchorUI = CreateAnchorUI();
        var bottom = GetField(anchorUI, "_bottom");
        Assert.Equal("Bottom", bottom.AccessibleName);
    }

    [Fact]
    public void AnchorUI_AllSpringControls_AccessibleRole_IsCheckButton()
    {
        object anchorUI = CreateAnchorUI();
        var left = GetField(anchorUI, "_left");
        var top = GetField(anchorUI, "_top");
        var right = GetField(anchorUI, "_right");
        var bottom = GetField(anchorUI, "_bottom");

        Assert.Equal(AccessibleRole.CheckButton, left.AccessibleRole);
        Assert.Equal(AccessibleRole.CheckButton, top.AccessibleRole);
        Assert.Equal(AccessibleRole.CheckButton, right.AccessibleRole);
        Assert.Equal(AccessibleRole.CheckButton, bottom.AccessibleRole);
    }

    [Fact]
    public void AnchorUI_Value_Initially_Null()
    {
        object anchorUI = CreateAnchorUI();
        PropertyInfo property = GetAnchorUIType().GetProperty("Value", PublicInstance);
        Assert.Null(property.GetValue(anchorUI));
    }

    [Theory]
    [InlineData(AnchorStyles.None)]
    [InlineData(AnchorStyles.Top)]
    [InlineData(AnchorStyles.Bottom)]
    [InlineData(AnchorStyles.Left)]
    [InlineData(AnchorStyles.Right)]
    [InlineData(AnchorStyles.Top | AnchorStyles.Left)]
    [InlineData(AnchorStyles.Top | AnchorStyles.Right)]
    [InlineData(AnchorStyles.Bottom | AnchorStyles.Left)]
    [InlineData(AnchorStyles.Bottom | AnchorStyles.Right)]
    [InlineData(AnchorStyles.Top | AnchorStyles.Bottom)]
    [InlineData(AnchorStyles.Left | AnchorStyles.Right)]
    [InlineData(AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom)]
    public void AnchorUI_Start_AnchorStyles_SetsIsSolidCorrectly(AnchorStyles value)
    {
        object anchorUI = CreateAnchorUI();
        MethodInfo start = GetAnchorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(anchorUI, [mockEditorService.Object, value]);

        PropertyInfo isSolidProperty = GetSpringControlType().GetProperty("IsSolid", PublicInstance);

        bool leftSolid = (bool)isSolidProperty.GetValue(GetSpringControl(anchorUI, "_left"));
        bool topSolid = (bool)isSolidProperty.GetValue(GetSpringControl(anchorUI, "_top"));
        bool rightSolid = (bool)isSolidProperty.GetValue(GetSpringControl(anchorUI, "_right"));
        bool bottomSolid = (bool)isSolidProperty.GetValue(GetSpringControl(anchorUI, "_bottom"));

        Assert.Equal((value & AnchorStyles.Left) == AnchorStyles.Left, leftSolid);
        Assert.Equal((value & AnchorStyles.Top) == AnchorStyles.Top, topSolid);
        Assert.Equal((value & AnchorStyles.Right) == AnchorStyles.Right, rightSolid);
        Assert.Equal((value & AnchorStyles.Bottom) == AnchorStyles.Bottom, bottomSolid);
    }

    [Theory]
    [InlineData(AnchorStyles.None)]
    [InlineData(AnchorStyles.Top)]
    [InlineData(AnchorStyles.Left)]
    [InlineData(AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom)]
    public void AnchorUI_Start_AnchorStyles_SetsValueToExpected(AnchorStyles value)
    {
        object anchorUI = CreateAnchorUI();
        MethodInfo start = GetAnchorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(anchorUI, [mockEditorService.Object, value]);

        PropertyInfo valueProperty = GetAnchorUIType().GetProperty("Value", PublicInstance);
        Assert.Equal(value, valueProperty.GetValue(anchorUI));
    }

    [Fact]
    public void AnchorUI_Start_NullValue_SetsValueToNull()
    {
        object anchorUI = CreateAnchorUI();
        MethodInfo start = GetAnchorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(anchorUI, [mockEditorService.Object, null]);

        PropertyInfo valueProperty = GetAnchorUIType().GetProperty("Value", PublicInstance);
        Assert.Null(valueProperty.GetValue(anchorUI));
    }

    [Fact]
    public void AnchorUI_Start_NonAnchorValue_SetsValueAndDoesNotThrow()
    {
        object anchorUI = CreateAnchorUI();
        MethodInfo start = GetAnchorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        object nonAnchor = "not-anchor";
        start.Invoke(anchorUI, [mockEditorService.Object, nonAnchor]);

        PropertyInfo valueProperty = GetAnchorUIType().GetProperty("Value", PublicInstance);
        Assert.Equal(nonAnchor, valueProperty.GetValue(anchorUI));
    }

    [Fact]
    public void AnchorUI_End_SetsValueToNull()
    {
        object anchorUI = CreateAnchorUI();
        MethodInfo start = GetAnchorUIType().GetMethod("Start", PublicInstance);
        MethodInfo end = GetAnchorUIType().GetMethod("End", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);

        start.Invoke(anchorUI, [mockEditorService.Object, AnchorStyles.Top]);
        end.Invoke(anchorUI, null);

        PropertyInfo valueProperty = GetAnchorUIType().GetProperty("Value", PublicInstance);
        Assert.Null(valueProperty.GetValue(anchorUI));
    }

    [Fact]
    public void AnchorUI_GetSelectedAnchor_NoneByDefault()
    {
        object anchorUI = CreateAnchorUI();
        MethodInfo getSelectedAnchor = GetAnchorUIType().GetMethod("GetSelectedAnchor", PublicInstance);
        var actual = (AnchorStyles)getSelectedAnchor.Invoke(anchorUI, null);
        Assert.Equal(AnchorStyles.None, actual);
    }

    [Theory]
    [InlineData(AnchorStyles.Top | AnchorStyles.Left)]
    [InlineData(AnchorStyles.Top | AnchorStyles.Right)]
    [InlineData(AnchorStyles.Bottom | AnchorStyles.Left)]
    [InlineData(AnchorStyles.Bottom | AnchorStyles.Right)]
    [InlineData(AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom)]
    public void AnchorUI_GetSelectedAnchor_ReturnsExpected(AnchorStyles expected)
    {
        object anchorUI = CreateAnchorUI();
        MethodInfo start = GetAnchorUIType().GetMethod("Start", PublicInstance);
        MethodInfo getSelectedAnchor = GetAnchorUIType().GetMethod("GetSelectedAnchor", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(anchorUI, [mockEditorService.Object, expected]);

        var actual = (AnchorStyles)getSelectedAnchor.Invoke(anchorUI, null);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AnchorUI_OnGotFocus_DoesNotThrow()
    {
        object anchorUI = CreateAnchorUI();
        MethodInfo onGotFocus = GetAnchorUIType()
            .GetMethod("OnGotFocus", NonPublicInstance);
        onGotFocus.Invoke(anchorUI, [EventArgs.Empty]);
    }

    [Fact]
    public void AnchorUI_InitializeComponent_SetsSize()
    {
        object anchorUI = CreateAnchorUI();
        var control = CastToControl(anchorUI);
        Assert.True(control.Width > 0);
        Assert.True(control.Height > 0);
    }

    [Fact]
    public void AnchorUI_InitializeComponent_LaysOutControls()
    {
        object anchorUI = CreateAnchorUI();
        var container = GetField(anchorUI, "_container");
        Assert.True(container.Width > 0);
        Assert.True(container.Height > 0);
    }

    [Fact]
    public void SpringControl_Constructor_SetsTabStop()
    {
        object anchorUI = CreateAnchorUI();
        object spring = CreateSpringControl(anchorUI);
        var control = CastToControl(spring);
        Assert.True(control.TabStop);
    }

    [Fact]
    public void SpringControl_IsSolid_DefaultIsFalse()
    {
        object anchorUI = CreateAnchorUI();
        object spring = CreateSpringControl(anchorUI);
        PropertyInfo isSolidProperty = GetSpringControlType().GetProperty("IsSolid", PublicInstance);
        Assert.False((bool)isSolidProperty.GetValue(spring));
    }

    [Fact]
    public void SpringControl_IsSolid_Set_True()
    {
        object anchorUI = CreateAnchorUI();
        object spring = CreateSpringControl(anchorUI);
        PropertyInfo isSolidProperty = GetSpringControlType().GetProperty("IsSolid", PublicInstance);
        isSolidProperty.SetValue(spring, true);
        Assert.True((bool)isSolidProperty.GetValue(spring));
    }

    [Fact]
    public void SpringControl_IsSolid_SetSameValue_DoesNotThrow()
    {
        object anchorUI = CreateAnchorUI();
        object spring = CreateSpringControl(anchorUI);
        PropertyInfo isSolidProperty = GetSpringControlType().GetProperty("IsSolid", PublicInstance);

        isSolidProperty.SetValue(spring, true);
        isSolidProperty.SetValue(spring, true);
        Assert.True((bool)isSolidProperty.GetValue(spring));
    }

    [Fact]
    public void SpringControl_IsSolid_Toggle_UpdatesValueOnAnchorUI()
    {
        object anchorUI = CreateAnchorUI();
        MethodInfo start = GetAnchorUIType().GetMethod("Start", PublicInstance);
        MethodInfo getSelectedAnchor = GetAnchorUIType().GetMethod("GetSelectedAnchor", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(anchorUI, [mockEditorService.Object, AnchorStyles.None]);

        var left = GetSpringControl(anchorUI, "_left");
        PropertyInfo isSolidProperty = GetSpringControlType().GetProperty("IsSolid", PublicInstance);
        isSolidProperty.SetValue(left, true);

        var actual = (AnchorStyles)getSelectedAnchor.Invoke(anchorUI, null);
        Assert.Equal(AnchorStyles.Left, actual);
    }

    [Fact]
    public void SpringControl_IsSolid_Toggle_Off_UpdatesValueOnAnchorUI()
    {
        object anchorUI = CreateAnchorUI();
        MethodInfo start = GetAnchorUIType().GetMethod("Start", PublicInstance);
        MethodInfo getSelectedAnchor = GetAnchorUIType().GetMethod("GetSelectedAnchor", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(anchorUI, [mockEditorService.Object, AnchorStyles.Left]);

        var left = GetSpringControl(anchorUI, "_left");
        PropertyInfo isSolidProperty = GetSpringControlType().GetProperty("IsSolid", PublicInstance);
        isSolidProperty.SetValue(left, false);

        var actual = (AnchorStyles)getSelectedAnchor.Invoke(anchorUI, null);
        Assert.Equal(AnchorStyles.None, actual);
    }

    [Fact]
    public void SpringControl_OnGotFocus_DoesNotThrow()
    {
        object anchorUI = CreateAnchorUI();
        object spring = CreateSpringControl(anchorUI);
        MethodInfo onGotFocus = GetSpringControlType()
            .GetMethod("OnGotFocus", NonPublicInstance);
        onGotFocus.Invoke(spring, [EventArgs.Empty]);
    }

    [Fact]
    public void SpringControl_OnLostFocus_DoesNotThrow()
    {
        object anchorUI = CreateAnchorUI();
        object spring = CreateSpringControl(anchorUI);
        MethodInfo onLostFocus = GetSpringControlType()
            .GetMethod("OnLostFocus", NonPublicInstance);
        onLostFocus.Invoke(spring, [EventArgs.Empty]);
    }

    [Fact]
    public void SpringControl_OnMouseDown_TogglesIsSolid()
    {
        object anchorUI = CreateAnchorUI();
        object spring = CreateSpringControl(anchorUI);
        PropertyInfo isSolidProperty = GetSpringControlType().GetProperty("IsSolid", PublicInstance);
        Assert.False((bool)isSolidProperty.GetValue(spring));

        MethodInfo onMouseDown = GetSpringControlType()
            .GetMethod("OnMouseDown", NonPublicInstance);
        onMouseDown.Invoke(spring, [new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0)]);

        Assert.True((bool)isSolidProperty.GetValue(spring));
    }

    [Fact]
    public void SpringControl_OnPaint_DoesNotThrow()
    {
        object anchorUI = CreateAnchorUI();
        object spring = CreateSpringControl(anchorUI);

        using Bitmap bitmap = new(50, 20);
        using Graphics graphics = Graphics.FromImage(bitmap);
        PaintEventArgs e = new(graphics, new Rectangle(0, 0, 50, 20));

        MethodInfo onPaint = GetSpringControlType()
            .GetMethod("OnPaint", NonPublicInstance);
        onPaint.Invoke(spring, [e]);
    }

    [Fact]
    public void SpringControl_OnPaint_SolidState_DoesNotThrow()
    {
        object anchorUI = CreateAnchorUI();
        object spring = CreateSpringControl(anchorUI);
        PropertyInfo isSolidProperty = GetSpringControlType().GetProperty("IsSolid", PublicInstance);
        isSolidProperty.SetValue(spring, true);

        using Bitmap bitmap = new(50, 20);
        using Graphics graphics = Graphics.FromImage(bitmap);
        PaintEventArgs e = new(graphics, new Rectangle(0, 0, 50, 20));

        MethodInfo onPaint = GetSpringControlType()
            .GetMethod("OnPaint", NonPublicInstance);
        onPaint.Invoke(spring, [e]);
    }

    [Fact]
    public void SpringControl_ProcessDialogChar_Space_Toggles()
    {
        object anchorUI = CreateAnchorUI();
        object spring = CreateSpringControl(anchorUI);
        PropertyInfo isSolidProperty = GetSpringControlType().GetProperty("IsSolid", PublicInstance);

        MethodInfo processDialogChar = GetSpringControlType()
            .GetMethod("ProcessDialogChar", NonPublicInstance);

        bool result = (bool)processDialogChar.Invoke(spring, [' ']);
        Assert.True(result);
        Assert.True((bool)isSolidProperty.GetValue(spring));
    }

    [Fact]
    public void SpringControl_ProcessDialogChar_NonSpace_PassesThrough()
    {
        object anchorUI = CreateAnchorUI();
        object spring = CreateSpringControl(anchorUI);
        PropertyInfo isSolidProperty = GetSpringControlType().GetProperty("IsSolid", PublicInstance);

        MethodInfo processDialogChar = GetSpringControlType()
            .GetMethod("ProcessDialogChar", NonPublicInstance);

        bool result = (bool)processDialogChar.Invoke(spring, ['a']);
        Assert.False(result);
        Assert.False((bool)isSolidProperty.GetValue(spring));
    }

    [Fact]
    public void SpringControl_ProcessDialogKey_Return_ClosesDropDown()
    {
        object anchorUI = CreateAnchorUI();
        MethodInfo start = GetAnchorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        mockEditorService
            .Setup(e => e.CloseDropDown())
            .Verifiable();
        start.Invoke(anchorUI, [mockEditorService.Object, AnchorStyles.Top]);

        var top = GetSpringControl(anchorUI, "_top");
        MethodInfo processDialogKey = GetSpringControlType()
            .GetMethod("ProcessDialogKey", NonPublicInstance);

        bool result = (bool)processDialogKey.Invoke(top, [Keys.Return]);
        Assert.True(result);
        mockEditorService.Verify(e => e.CloseDropDown(), Times.Once());
    }

    [Fact]
    public void SpringControl_ProcessDialogKey_Escape_ClosesDropDown_AndRestoresOldAnchor()
    {
        object anchorUI = CreateAnchorUI();
        MethodInfo start = GetAnchorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        mockEditorService
            .Setup(e => e.CloseDropDown())
            .Verifiable();
        start.Invoke(anchorUI, [mockEditorService.Object, AnchorStyles.Top | AnchorStyles.Left]);

        var bottom = GetSpringControl(anchorUI, "_bottom");
        PropertyInfo isSolidProperty = GetSpringControlType().GetProperty("IsSolid", PublicInstance);
        isSolidProperty.SetValue(bottom, true);

        PropertyInfo valueProperty = GetAnchorUIType().GetProperty("Value", PublicInstance);
        var current = (AnchorStyles)valueProperty.GetValue(anchorUI);
        Assert.Equal(AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom, current);

        var top = GetSpringControl(anchorUI, "_top");
        MethodInfo processDialogKey = GetSpringControlType()
            .GetMethod("ProcessDialogKey", NonPublicInstance);

        bool result = (bool)processDialogKey.Invoke(top, [Keys.Escape]);
        Assert.True(result);
        mockEditorService.Verify(e => e.CloseDropDown(), Times.Once());

        var restored = (AnchorStyles)valueProperty.GetValue(anchorUI);
        Assert.Equal(AnchorStyles.Top | AnchorStyles.Left, restored);
    }

    [Fact]
    public void SpringControl_ProcessDialogKey_ReturnWithAlt_PassesThrough()
    {
        object anchorUI = CreateAnchorUI();
        MethodInfo start = GetAnchorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(anchorUI, [mockEditorService.Object, AnchorStyles.Top]);

        var top = GetSpringControl(anchorUI, "_top");
        MethodInfo processDialogKey = GetSpringControlType()
            .GetMethod("ProcessDialogKey", NonPublicInstance);

        bool result = (bool)processDialogKey.Invoke(top, [Keys.Return | Keys.Alt]);
        Assert.False(result);
    }

    [Fact]
    public void SpringControl_ProcessDialogKey_ReturnWithControl_PassesThrough()
    {
        object anchorUI = CreateAnchorUI();
        MethodInfo start = GetAnchorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(anchorUI, [mockEditorService.Object, AnchorStyles.Top]);

        var top = GetSpringControl(anchorUI, "_top");
        MethodInfo processDialogKey = GetSpringControlType()
            .GetMethod("ProcessDialogKey", NonPublicInstance);

        bool result = (bool)processDialogKey.Invoke(top, [Keys.Return | Keys.Control]);
        Assert.False(result);
    }

    [WinFormsTheory]
    [InlineData("_left", Keys.Tab, "_top")]
    [InlineData("_bottom", Keys.Tab, "_left")]
    [InlineData("_top", Keys.Tab | Keys.Shift, "_left")]
    [InlineData("_left", Keys.Tab | Keys.Shift, "_bottom")]
    public void SpringControl_ProcessDialogKey_Tab_MovesFocus(string from, Keys keyData, string destination)
    {
        using Control anchorUI = CastToControl(CreateAnchorUI());
        using Form host = new();
        host.Controls.Add(anchorUI);
        MethodInfo start = GetAnchorUIType().GetMethod("Start", PublicInstance);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        start.Invoke(anchorUI, [mockEditorService.Object, AnchorStyles.Top]);
        host.Show();

        Control source = GetField(anchorUI, from);
        Assert.True(source.Focus());
        Assert.True(source.Focused);
        MethodInfo processDialogKey = GetSpringControlType()
            .GetMethod("ProcessDialogKey", NonPublicInstance);

        bool result = (bool)processDialogKey.Invoke(source, [keyData]);
        Assert.True(result);
        Assert.True(GetField(anchorUI, destination).Focused);
        Assert.False(source.Focused);
    }

    [WinFormsFact]
    public void AnchorEditor_EditValue_MouseSelectionAndEnter_CommitsDifferentValue()
    {
        AnchorEditor editor = new();
        using Form host = new();
        Mock<IWindowsFormsEditorService> service = new(MockBehavior.Strict);
        Mock<IServiceProvider> provider = new(MockBehavior.Strict);
        provider.Setup(p => p.GetService(typeof(IWindowsFormsEditorService))).Returns(service.Object);
        service.Setup(s => s.CloseDropDown());
        service.Setup(s => s.DropDownControl(It.IsAny<Control>())).Callback<Control>(control =>
        {
            host.Controls.Add(control);
            host.Show();
            Control bottom = GetField(control, "_bottom");
            GetSpringControlType().GetMethod("OnMouseDown", NonPublicInstance)
                .Invoke(bottom, [new MouseEventArgs(MouseButtons.Left, 1, 1, 1, 0)]);
            Assert.True(bottom.Focused);
            Assert.True((bool)GetSpringControlType().GetMethod("ProcessDialogKey", NonPublicInstance).Invoke(bottom, [Keys.Enter]));
        });

        Assert.Equal(AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom,
            editor.EditValue(null, provider.Object, AnchorStyles.Top | AnchorStyles.Left));
        service.Verify(s => s.CloseDropDown(), Times.Once());
    }

    [Fact]
    public void SpringControl_ProcessDialogKey_OtherKey_PassesThrough()
    {
        object anchorUI = CreateAnchorUI();
        object spring = CreateSpringControl(anchorUI);
        MethodInfo processDialogKey = GetSpringControlType()
            .GetMethod("ProcessDialogKey", NonPublicInstance);

        bool result = (bool)processDialogKey.Invoke(spring, [Keys.A]);
        Assert.False(result);
    }

    [Fact]
    public void SpringControl_AccessibleObject_DefaultAction_NotSolid_IsCheck()
    {
        object anchorUI = CreateAnchorUI();
        var left = GetField(anchorUI, "_left");
        PropertyInfo isSolidProperty = GetSpringControlType().GetProperty("IsSolid", PublicInstance);
        isSolidProperty.SetValue(left, false);

        string defaultAction = left.AccessibilityObject.DefaultAction;
        Assert.Equal(SR.AccessibleActionCheck, defaultAction);
    }

    [Fact]
    public void SpringControl_AccessibleObject_DefaultAction_Solid_IsUncheck()
    {
        object anchorUI = CreateAnchorUI();
        var left = GetField(anchorUI, "_left");
        PropertyInfo isSolidProperty = GetSpringControlType().GetProperty("IsSolid", PublicInstance);
        isSolidProperty.SetValue(left, true);

        string defaultAction = left.AccessibilityObject.DefaultAction;
        Assert.Equal(SR.AccessibleActionUncheck, defaultAction);
    }

    [Fact]
    public void SpringControl_AccessibleObject_State_NotSolid_NotChecked()
    {
        object anchorUI = CreateAnchorUI();
        var left = GetField(anchorUI, "_left");
        PropertyInfo isSolidProperty = GetSpringControlType().GetProperty("IsSolid", PublicInstance);
        isSolidProperty.SetValue(left, false);

        AccessibleStates state = left.AccessibilityObject.State;
        Assert.False((state & AccessibleStates.Checked) == AccessibleStates.Checked);
    }

    [Fact]
    public void SpringControl_AccessibleObject_State_Solid_IsChecked()
    {
        object anchorUI = CreateAnchorUI();
        var left = GetField(anchorUI, "_left");
        PropertyInfo isSolidProperty = GetSpringControlType().GetProperty("IsSolid", PublicInstance);
        isSolidProperty.SetValue(left, true);

        AccessibleStates state = left.AccessibilityObject.State;
        Assert.True((state & AccessibleStates.Checked) == AccessibleStates.Checked);
    }

    [Fact]
    public void SpringControl_AccessibleObject_IsCreatedOnDemand()
    {
        object anchorUI = CreateAnchorUI();
        var left = GetField(anchorUI, "_left");
        Assert.NotNull(left.AccessibilityObject);
    }

    [Fact]
    public void ContainerPlaceholder_Constructor_SetsDefaults()
    {
        object instance = Activator.CreateInstance(GetContainerPlaceholderType());
        var control = CastToControl(instance);
        Assert.Equal(SystemColors.Window, control.BackColor);
        Assert.Equal(SystemColors.WindowText, control.ForeColor);
        Assert.False(control.TabStop);
    }

    [Fact]
    public void ControlPlaceholder_Constructor_SetsDefaults()
    {
        object instance = Activator.CreateInstance(GetControlPlaceholderType());
        var control = CastToControl(instance);
        Assert.Equal(SystemColors.Control, control.BackColor);
        Assert.False(control.TabStop);
    }
}
