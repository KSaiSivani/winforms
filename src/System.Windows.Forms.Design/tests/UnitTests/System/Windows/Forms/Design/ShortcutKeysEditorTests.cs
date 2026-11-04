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

public class ShortcutKeysEditorTests
{
    private const BindingFlags NonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
    private const BindingFlags NonPublicStatic = BindingFlags.NonPublic | BindingFlags.Static;

    private static Type GetShortcutKeysUIType()
    {
        return typeof(ShortcutKeysEditor).GetNestedType("ShortcutKeysUI", NonPublicInstance);
    }

    private static object CreateShortcutKeysUI()
    {
        return Activator.CreateInstance(GetShortcutKeysUIType());
    }

    private static Control CastToControl(object instance) => (Control)instance;

    private static object GetValue(object shortcutKeysUI)
        => GetShortcutKeysUIType().GetProperty("Value", PublicInstance).GetValue(shortcutKeysUI);

    private static void Start(object shortcutKeysUI, Keys value)
        => GetShortcutKeysUIType().GetMethod("Start", PublicInstance).Invoke(shortcutKeysUI, [value]);

    private static void End(object shortcutKeysUI)
        => GetShortcutKeysUIType().GetMethod("End", PublicInstance).Invoke(shortcutKeysUI, null);

    private static CheckBox GetCheckBox(object shortcutKeysUI, string fieldName)
    {
        return (CheckBox)GetShortcutKeysUIType()
            .GetField(fieldName, NonPublicInstance)
            .GetValue(shortcutKeysUI);
    }

    private static ComboBox GetComboBox(object shortcutKeysUI, string fieldName)
    {
        return (ComboBox)GetShortcutKeysUIType()
            .GetField(fieldName, NonPublicInstance)
            .GetValue(shortcutKeysUI);
    }

    private static Button GetButton(object shortcutKeysUI, string fieldName)
    {
        return (Button)GetShortcutKeysUIType()
            .GetField(fieldName, NonPublicInstance)
            .GetValue(shortcutKeysUI);
    }

    private static Label GetLabel(object shortcutKeysUI, string fieldName)
    {
        return (Label)GetShortcutKeysUIType()
            .GetField(fieldName, NonPublicInstance)
            .GetValue(shortcutKeysUI);
    }

    private static TableLayoutPanel GetTableLayoutPanel(object shortcutKeysUI, string fieldName)
    {
        return (TableLayoutPanel)GetShortcutKeysUIType()
            .GetField(fieldName, NonPublicInstance)
            .GetValue(shortcutKeysUI);
    }

    private static object GetFieldValue(object shortcutKeysUI, string fieldName)
    {
        return GetShortcutKeysUIType()
            .GetField(fieldName, NonPublicInstance)
            .GetValue(shortcutKeysUI);
    }

    private static void SetFieldValue(object shortcutKeysUI, string fieldName, object value)
    {
        GetShortcutKeysUIType()
            .GetField(fieldName, NonPublicInstance)
            .SetValue(shortcutKeysUI, value);
    }

    private static MethodInfo GetMethod(Type type, string name, BindingFlags flags)
    {
        return type.GetMethod(name, flags);
    }

    [Fact]
    public void ShortcutKeysEditor_Ctor_Default()
    {
        ShortcutKeysEditor editor = new();
        Assert.False(editor.IsDropDownResizable);
    }

    public static IEnumerable<object[]> EditValue_TestData()
    {
        yield return new object[] { null };
        yield return new object[] { "value" };
        yield return new object[] { Shortcut.CtrlA };
        yield return new object[] { new() };
    }

    [Theory]
    [MemberData(nameof(EditValue_TestData))]
    public void ShortcutKeysEditor_EditValue_ValidProvider_ReturnsValue(object value)
    {
        ShortcutKeysEditor editor = new();
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
    public void ShortcutKeysEditor_EditValue_InvalidProvider_ReturnsValue(IServiceProvider provider, object value)
    {
        ShortcutKeysEditor editor = new();
        Assert.Same(value, editor.EditValue(null, provider, value));
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void ShortcutKeysEditor_GetEditStyle_Invoke_ReturnsModal(ITypeDescriptorContext context)
    {
        ShortcutKeysEditor editor = new();
        Assert.Equal(UITypeEditorEditStyle.DropDown, editor.GetEditStyle(context));
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void ShortcutKeysEditor_GetPaintValueSupported_Invoke_ReturnsFalse(ITypeDescriptorContext context)
    {
        ShortcutKeysEditor editor = new();
        Assert.False(editor.GetPaintValueSupported(context));
    }

    [Fact]
    public void ShortcutKeysEditor_HasShortcutKeysUIField()
    {
        FieldInfo field = typeof(ShortcutKeysEditor).GetField("_shortcutKeysUI", NonPublicInstance);
        Assert.NotNull(field);
    }

    [Fact]
    public void ShortcutKeysEditor_ShortcutKeysUIField_InitialValue_IsNull()
    {
        ShortcutKeysEditor editor = new();
        FieldInfo field = typeof(ShortcutKeysEditor).GetField("_shortcutKeysUI", NonPublicInstance);
        Assert.Null(field.GetValue(editor));
    }

    [Fact]
    public void ShortcutKeysEditor_EditValue_CalledTwice_ReusesShortcutKeysUI()
    {
        ShortcutKeysEditor editor = new();
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(mockEditorService.Object);
        mockEditorService
            .Setup(e => e.DropDownControl(It.IsAny<Control>()));

        _ = editor.EditValue(null, mockServiceProvider.Object, Shortcut.CtrlA);
        FieldInfo field = typeof(ShortcutKeysEditor).GetField("_shortcutKeysUI", NonPublicInstance);
        object first = field.GetValue(editor);
        Assert.NotNull(first);

        _ = editor.EditValue(null, mockServiceProvider.Object, Shortcut.CtrlA);
        object second = field.GetValue(editor);
        Assert.Same(first, second);
    }

    [Fact]
    public void ShortcutKeysEditor_EditValue_InvalidProvider_DoesNotCreateShortcutKeysUI()
    {
        ShortcutKeysEditor editor = new();
        _ = editor.EditValue(null, null, Shortcut.CtrlA);

        FieldInfo field = typeof(ShortcutKeysEditor).GetField("_shortcutKeysUI", NonPublicInstance);
        Assert.Null(field.GetValue(editor));
    }

    [Fact]
    public void ShortcutKeysUI_Constructor_CreatesInstance()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        Assert.NotNull(shortcutKeysUI);
    }

    [Fact]
    public void ShortcutKeysUI_Constructor_Size_IsNonZero()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        var control = CastToControl(shortcutKeysUI);
        Assert.True(control.Width > 0);
        Assert.True(control.Height > 0);
    }

    [Fact]
    public void ShortcutKeysUI_Constructor_BackColor_IsControl()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        var control = CastToControl(shortcutKeysUI);
        Assert.Equal(SystemColors.Control, control.BackColor);
    }

    [Fact]
    public void ShortcutKeysUI_Constructor_Name_IsShortcutKeysUI()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        var control = CastToControl(shortcutKeysUI);
        Assert.Equal("ShortcutKeysUI", control.Name);
    }

    [Fact]
    public void ShortcutKeysUI_Constructor_HasExpectedControls()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        var control = CastToControl(shortcutKeysUI);
        Assert.Equal(2, control.Controls.Count);
    }

    [Fact]
    public void ShortcutKeysUI_Constructor_HasResetButton()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        Button resetButton = GetButton(shortcutKeysUI, "_resetButton");
        Assert.NotNull(resetButton);
        Assert.Equal("btnReset", resetButton.Name);
    }

    [Fact]
    public void ShortcutKeysUI_Constructor_HasModifiersCheckBoxes()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        CheckBox ctrl = GetCheckBox(shortcutKeysUI, "_ctrlCheckBox");
        CheckBox alt = GetCheckBox(shortcutKeysUI, "_altCheckBox");
        CheckBox shift = GetCheckBox(shortcutKeysUI, "_shiftCheckBox");

        Assert.NotNull(ctrl);
        Assert.NotNull(alt);
        Assert.NotNull(shift);

        Assert.Equal("chkCtrl", ctrl.Name);
        Assert.Equal("chkAlt", alt.Name);
        Assert.Equal("chkShift", shift.Name);
    }

    [Fact]
    public void ShortcutKeysUI_Constructor_HasKeyComboBox()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        ComboBox keyComboBox = GetComboBox(shortcutKeysUI, "_keyComboBox");
        Assert.NotNull(keyComboBox);
        Assert.Equal("cmbKey", keyComboBox.Name);
        Assert.Equal(ComboBoxStyle.DropDownList, keyComboBox.DropDownStyle);
    }

    [Fact]
    public void ShortcutKeysUI_Constructor_KeyComboBox_HasValidKeys()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        ComboBox keyComboBox = GetComboBox(shortcutKeysUI, "_keyComboBox");
        Assert.True(keyComboBox.Items.Count > 0);
        Assert.NotNull(keyComboBox.Items[0]);
    }

    [Fact]
    public void ShortcutKeysUI_Constructor_HasLabels()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        Label modifiersLabel = GetLabel(shortcutKeysUI, "_modifiersLabel");
        Label keyLabel = GetLabel(shortcutKeysUI, "_keyLabel");

        Assert.NotNull(modifiersLabel);
        Assert.NotNull(keyLabel);

        Assert.Equal("lblModifiers", modifiersLabel.Name);
        Assert.Equal("lblKey", keyLabel.Name);
    }

    [Fact]
    public void ShortcutKeysUI_Constructor_HasTableLayoutPanels()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        TableLayoutPanel outer = GetTableLayoutPanel(shortcutKeysUI, "_outerPanel");
        TableLayoutPanel inner = GetTableLayoutPanel(shortcutKeysUI, "_innerPanel");

        Assert.NotNull(outer);
        Assert.NotNull(inner);

        Assert.Equal("tlpOuter", outer.Name);
        Assert.Equal("tlpInner", inner.Name);
    }

    [Fact]
    public void ShortcutKeysUI_Constructor_OuterPanel_ColumnCount_IsThree()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        TableLayoutPanel outer = GetTableLayoutPanel(shortcutKeysUI, "_outerPanel");
        Assert.Equal(3, outer.ColumnCount);
    }

    [Fact]
    public void ShortcutKeysUI_Constructor_OuterPanel_RowCount_IsTwo()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        TableLayoutPanel outer = GetTableLayoutPanel(shortcutKeysUI, "_outerPanel");
        Assert.Equal(2, outer.RowCount);
    }

    [Fact]
    public void ShortcutKeysUI_Constructor_InnerPanel_ColumnCount_IsTwo()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        TableLayoutPanel inner = GetTableLayoutPanel(shortcutKeysUI, "_innerPanel");
        Assert.Equal(2, inner.ColumnCount);
    }

    [Fact]
    public void ShortcutKeysUI_Constructor_InnerPanel_RowCount_IsTwo()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        TableLayoutPanel inner = GetTableLayoutPanel(shortcutKeysUI, "_innerPanel");
        Assert.Equal(2, inner.RowCount);
    }

    [Fact]
    public void ShortcutKeysUI_Start_NullValue_SetsCheckBoxesToFalse()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        start.Invoke(shortcutKeysUI, [null]);

        CheckBox ctrl = GetCheckBox(shortcutKeysUI, "_ctrlCheckBox");
        CheckBox alt = GetCheckBox(shortcutKeysUI, "_altCheckBox");
        CheckBox shift = GetCheckBox(shortcutKeysUI, "_shiftCheckBox");

        Assert.False(ctrl.Checked);
        Assert.False(alt.Checked);
        Assert.False(shift.Checked);
    }

    [Fact]
    public void ShortcutKeysUI_Start_NullValue_SetsComboBoxIndexToMinusOne()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        start.Invoke(shortcutKeysUI, [null]);

        ComboBox keyComboBox = GetComboBox(shortcutKeysUI, "_keyComboBox");
        Assert.Equal(-1, keyComboBox.SelectedIndex);
    }

    [Fact]
    public void ShortcutKeysUI_Start_NullValue_SetsOriginalAndCurrentValue()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        start.Invoke(shortcutKeysUI, [null]);

        Assert.Null(GetFieldValue(shortcutKeysUI, "_originalValue"));
        Assert.Null(GetFieldValue(shortcutKeysUI, "_currentValue"));
    }

    [Fact]
    public void ShortcutKeysUI_Start_NullValue_SetsUpdateCurrentValueToTrue()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        start.Invoke(shortcutKeysUI, [null]);

        Assert.True((bool)GetFieldValue(shortcutKeysUI, "_updateCurrentValue"));
    }

    [Fact]
    public void ShortcutKeysUI_Start_NullValue_SetsUnknownKeyCodeToNone()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        start.Invoke(shortcutKeysUI, [null]);

        Assert.Equal(Keys.None, (Keys)GetFieldValue(shortcutKeysUI, "_unknownKeyCode"));
    }

    [Fact]
    public void ShortcutKeysUI_Start_KeysNone_SetsCheckBoxesToFalse()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        start.Invoke(shortcutKeysUI, [(object)Keys.None]);

        CheckBox ctrl = GetCheckBox(shortcutKeysUI, "_ctrlCheckBox");
        CheckBox alt = GetCheckBox(shortcutKeysUI, "_altCheckBox");
        CheckBox shift = GetCheckBox(shortcutKeysUI, "_shiftCheckBox");

        Assert.False(ctrl.Checked);
        Assert.False(alt.Checked);
        Assert.False(shift.Checked);
    }

    [Fact]
    public void ShortcutKeysUI_Start_KeysNone_SetsComboBoxIndexToMinusOne()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        start.Invoke(shortcutKeysUI, [(object)Keys.None]);

        ComboBox keyComboBox = GetComboBox(shortcutKeysUI, "_keyComboBox");
        Assert.Equal(-1, keyComboBox.SelectedIndex);
    }

    [Fact]
    public void ShortcutKeysUI_Start_KeysControl_SetsCtrlChecked()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        start.Invoke(shortcutKeysUI, [(object)Keys.Control]);

        CheckBox ctrl = GetCheckBox(shortcutKeysUI, "_ctrlCheckBox");
        CheckBox alt = GetCheckBox(shortcutKeysUI, "_altCheckBox");
        CheckBox shift = GetCheckBox(shortcutKeysUI, "_shiftCheckBox");

        Assert.True(ctrl.Checked);
        Assert.False(alt.Checked);
        Assert.False(shift.Checked);
    }

    [Fact]
    public void ShortcutKeysUI_Start_KeysAlt_SetsAltChecked()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        start.Invoke(shortcutKeysUI, [(object)Keys.Alt]);

        CheckBox ctrl = GetCheckBox(shortcutKeysUI, "_ctrlCheckBox");
        CheckBox alt = GetCheckBox(shortcutKeysUI, "_altCheckBox");
        CheckBox shift = GetCheckBox(shortcutKeysUI, "_shiftCheckBox");

        Assert.False(ctrl.Checked);
        Assert.True(alt.Checked);
        Assert.False(shift.Checked);
    }

    [Fact]
    public void ShortcutKeysUI_Start_KeysShift_SetsShiftChecked()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        start.Invoke(shortcutKeysUI, [(object)Keys.Shift]);

        CheckBox ctrl = GetCheckBox(shortcutKeysUI, "_ctrlCheckBox");
        CheckBox alt = GetCheckBox(shortcutKeysUI, "_altCheckBox");
        CheckBox shift = GetCheckBox(shortcutKeysUI, "_shiftCheckBox");

        Assert.False(ctrl.Checked);
        Assert.False(alt.Checked);
        Assert.True(shift.Checked);
    }

    [Fact]
    public void ShortcutKeysUI_Start_ValidKey_SelectsKeyInComboBox()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        start.Invoke(shortcutKeysUI, [(object)Keys.A]);

        ComboBox keyComboBox = GetComboBox(shortcutKeysUI, "_keyComboBox");
        Assert.NotEqual(-1, keyComboBox.SelectedIndex);
        Assert.Equal("A", keyComboBox.SelectedItem);
    }

    [Fact]
    public void ShortcutKeysUI_Start_ValidKeyWithCtrl_SetsCtrlAndSelectsKey()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        start.Invoke(shortcutKeysUI, [(object)(Keys.Control | Keys.A)]);

        CheckBox ctrl = GetCheckBox(shortcutKeysUI, "_ctrlCheckBox");
        CheckBox alt = GetCheckBox(shortcutKeysUI, "_altCheckBox");
        CheckBox shift = GetCheckBox(shortcutKeysUI, "_shiftCheckBox");
        ComboBox keyComboBox = GetComboBox(shortcutKeysUI, "_keyComboBox");

        Assert.True(ctrl.Checked);
        Assert.False(alt.Checked);
        Assert.False(shift.Checked);
        Assert.Equal("A", keyComboBox.SelectedItem);
    }

    [Fact]
    public void ShortcutKeysUI_Start_ValidKeyWithAllModifiers_SetsAllCheckBoxesAndSelectsKey()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        start.Invoke(shortcutKeysUI, [(object)(Keys.Control | Keys.Alt | Keys.Shift | Keys.B)]);

        CheckBox ctrl = GetCheckBox(shortcutKeysUI, "_ctrlCheckBox");
        CheckBox alt = GetCheckBox(shortcutKeysUI, "_altCheckBox");
        CheckBox shift = GetCheckBox(shortcutKeysUI, "_shiftCheckBox");
        ComboBox keyComboBox = GetComboBox(shortcutKeysUI, "_keyComboBox");

        Assert.True(ctrl.Checked);
        Assert.True(alt.Checked);
        Assert.True(shift.Checked);
        Assert.Equal("B", keyComboBox.SelectedItem);
    }

    [Fact]
    public void ShortcutKeysUI_Start_NumericKey_SelectsDigitInComboBox()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        start.Invoke(shortcutKeysUI, [(object)Keys.D5]);

        ComboBox keyComboBox = GetComboBox(shortcutKeysUI, "_keyComboBox");
        Assert.Equal("5", keyComboBox.SelectedItem);
    }

    [Fact]
    public void ShortcutKeysUI_Start_FunctionKey_SelectsFunctionKeyInComboBox()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        start.Invoke(shortcutKeysUI, [(object)Keys.F1]);

        ComboBox keyComboBox = GetComboBox(shortcutKeysUI, "_keyComboBox");
        Assert.Equal("F1", keyComboBox.SelectedItem);
    }

    [Fact]
    public void ShortcutKeysUI_Start_NonKeysValue_DefaultsToKeysNone()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        start.Invoke(shortcutKeysUI, ["nonKeysValue"]);

        CheckBox ctrl = GetCheckBox(shortcutKeysUI, "_ctrlCheckBox");
        CheckBox alt = GetCheckBox(shortcutKeysUI, "_altCheckBox");
        CheckBox shift = GetCheckBox(shortcutKeysUI, "_shiftCheckBox");
        ComboBox keyComboBox = GetComboBox(shortcutKeysUI, "_keyComboBox");

        Assert.False(ctrl.Checked);
        Assert.False(alt.Checked);
        Assert.False(shift.Checked);
        Assert.Equal(-1, keyComboBox.SelectedIndex);
    }

    [Fact]
    public void ShortcutKeysUI_Start_NonKeysValue_SetsCurrentValueToOriginal()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        object value = "nonKeysValue";
        start.Invoke(shortcutKeysUI, [value]);

        Assert.Equal(value, GetFieldValue(shortcutKeysUI, "_originalValue"));
        Assert.Equal(value, GetFieldValue(shortcutKeysUI, "_currentValue"));
    }

    [Fact]
    public void ShortcutKeysUI_Start_ResetUnknownKeyCode_ToNone()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        start.Invoke(shortcutKeysUI, [(object)Keys.A]);

        Assert.Equal(Keys.None, (Keys)GetFieldValue(shortcutKeysUI, "_unknownKeyCode"));
    }

    [Fact]
    public void ShortcutKeysUI_End_SetsCurrentValueToNull()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        MethodInfo end = GetShortcutKeysUIType().GetMethod("End", PublicInstance);

        start.Invoke(shortcutKeysUI, [(object)Keys.A]);
        end.Invoke(shortcutKeysUI, null);

        Assert.Null(GetFieldValue(shortcutKeysUI, "_currentValue"));
    }

    [Fact]
    public void ShortcutKeysUI_End_SetsOriginalValueToNull()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        MethodInfo end = GetShortcutKeysUIType().GetMethod("End", PublicInstance);

        start.Invoke(shortcutKeysUI, [(object)Keys.A]);
        end.Invoke(shortcutKeysUI, null);

        Assert.Null(GetFieldValue(shortcutKeysUI, "_originalValue"));
    }

    [Fact]
    public void ShortcutKeysUI_End_SetsUpdateCurrentValueToFalse()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        MethodInfo end = GetShortcutKeysUIType().GetMethod("End", PublicInstance);

        start.Invoke(shortcutKeysUI, [(object)Keys.A]);
        end.Invoke(shortcutKeysUI, null);

        Assert.False((bool)GetFieldValue(shortcutKeysUI, "_updateCurrentValue"));
    }

    [WinFormsFact]
    public void ShortcutKeysUI_UnsupportedKey_InsertsMapsAndRemovesPlaceholderBeforeReuse()
    {
        using Control shortcutKeysUI = CastToControl(CreateShortcutKeysUI());
        ComboBox combo = GetComboBox(shortcutKeysUI, "_keyComboBox");
        object[] originalItems = combo.Items.Cast<object>().ToArray();
        Start(shortcutKeysUI, Keys.Control | Keys.Back);

        Assert.Equal(originalItems.Length + 1, combo.Items.Count);
        Assert.Equal(SR.ShortcutKeys_InvalidKey, combo.Items[0]);
        Assert.Equal(0, combo.SelectedIndex);
        Assert.Equal(Keys.Back, (Keys)shortcutKeysUI.TestAccessor.Dynamic._unknownKeyCode);
        Assert.Equal(Keys.Control | Keys.Back, GetValue(shortcutKeysUI));

        GetCheckBox(shortcutKeysUI, "_ctrlCheckBox").Checked = false;
        Assert.Equal(Keys.Back, GetValue(shortcutKeysUI));
        combo.SelectedIndex = 1;
        Assert.Equal(Keys.A, GetValue(shortcutKeysUI));
        combo.SelectedIndex = 0;
        Assert.Equal(Keys.Back, GetValue(shortcutKeysUI));

        End(shortcutKeysUI);

        Assert.Equal(Keys.None, (Keys)GetFieldValue(shortcutKeysUI, "_unknownKeyCode"));
        Assert.Equal(originalItems, combo.Items.Cast<object>().ToArray());
        Assert.Null(GetValue(shortcutKeysUI));

        Start(shortcutKeysUI, Keys.B);
        Assert.Equal(1, combo.SelectedIndex);
        Assert.Equal(Keys.B, GetValue(shortcutKeysUI));
        GetCheckBox(shortcutKeysUI, "_altCheckBox").Checked = true;
        Assert.Equal(Keys.Alt | Keys.B, GetValue(shortcutKeysUI));
        End(shortcutKeysUI);
        Assert.Equal(originalItems, combo.Items.Cast<object>().ToArray());
    }

    [Fact]
    public void ShortcutKeysUI_Value_AfterEnd_IsNull()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        PropertyInfo valueProperty = GetShortcutKeysUIType().GetProperty("Value", PublicInstance);
        Assert.Null(valueProperty.GetValue(shortcutKeysUI));
    }

    [Fact]
    public void ShortcutKeysUI_Value_AfterStartWithNull_IsNull()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        PropertyInfo valueProperty = GetShortcutKeysUIType().GetProperty("Value", PublicInstance);

        start.Invoke(shortcutKeysUI, [null]);
        Assert.Null(valueProperty.GetValue(shortcutKeysUI));
    }

    [Fact]
    public void ShortcutKeysUI_Value_AfterStartWithControlOnly_ReturnsNone()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        PropertyInfo valueProperty = GetShortcutKeysUIType().GetProperty("Value", PublicInstance);

        start.Invoke(shortcutKeysUI, [(object)Keys.Control]);
        Assert.Equal(Keys.None, valueProperty.GetValue(shortcutKeysUI));
    }

    [Fact]
    public void ShortcutKeysUI_Value_AfterStartWithAltOnly_ReturnsNone()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        PropertyInfo valueProperty = GetShortcutKeysUIType().GetProperty("Value", PublicInstance);

        start.Invoke(shortcutKeysUI, [(object)Keys.Alt]);
        Assert.Equal(Keys.None, valueProperty.GetValue(shortcutKeysUI));
    }

    [Fact]
    public void ShortcutKeysUI_Value_AfterStartWithShiftOnly_ReturnsNone()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        PropertyInfo valueProperty = GetShortcutKeysUIType().GetProperty("Value", PublicInstance);

        start.Invoke(shortcutKeysUI, [(object)Keys.Shift]);
        Assert.Equal(Keys.None, valueProperty.GetValue(shortcutKeysUI));
    }

    [Fact]
    public void ShortcutKeysUI_Value_AfterStartWithControlAndAlt_ReturnsNone()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        PropertyInfo valueProperty = GetShortcutKeysUIType().GetProperty("Value", PublicInstance);

        start.Invoke(shortcutKeysUI, [(object)(Keys.Control | Keys.Alt)]);
        Assert.Equal(Keys.None, valueProperty.GetValue(shortcutKeysUI));
    }

    [Fact]
    public void ShortcutKeysUI_Value_AfterStartWithKeyCode_ReturnsKeyValue()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        PropertyInfo valueProperty = GetShortcutKeysUIType().GetProperty("Value", PublicInstance);

        start.Invoke(shortcutKeysUI, [(object)Keys.A]);
        Assert.Equal(Keys.A, valueProperty.GetValue(shortcutKeysUI));
    }

    [Fact]
    public void ShortcutKeysUI_Value_AfterStartWithCtrlA_ReturnsCtrlA()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        PropertyInfo valueProperty = GetShortcutKeysUIType().GetProperty("Value", PublicInstance);

        start.Invoke(shortcutKeysUI, [(object)(Keys.Control | Keys.A)]);
        Assert.Equal(Keys.Control | Keys.A, valueProperty.GetValue(shortcutKeysUI));
    }

    [Fact]
    public void ShortcutKeysUI_Value_AfterStartWithAllModifiersAndKey_ReturnsCombinedValue()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        PropertyInfo valueProperty = GetShortcutKeysUIType().GetProperty("Value", PublicInstance);

        Keys expected = Keys.Control | Keys.Alt | Keys.Shift | Keys.B;
        start.Invoke(shortcutKeysUI, [(object)expected]);
        Assert.Equal(expected, valueProperty.GetValue(shortcutKeysUI));
    }

    [Fact]
    public void ShortcutKeysUI_KeysConverter_AfterAccess_IsNotNull()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo keysConverterGetter = GetShortcutKeysUIType()
            .GetProperty("KeysConverter", NonPublicInstance)
            .GetGetMethod(nonPublic: true);
        Assert.NotNull(keysConverterGetter.Invoke(shortcutKeysUI, null));
    }

    [Fact]
    public void ShortcutKeysUI_IsValidKey_ValidKey_ReturnsTrue()
    {
        MethodInfo isValidKey = GetShortcutKeysUIType().GetMethod("IsValidKey", NonPublicStatic);
        Assert.True((bool)isValidKey.Invoke(null, [(Keys)Keys.A]));
    }

    [Fact]
    public void ShortcutKeysUI_IsValidKey_FunctionKey_ReturnsTrue()
    {
        MethodInfo isValidKey = GetShortcutKeysUIType().GetMethod("IsValidKey", NonPublicStatic);
        Assert.True((bool)isValidKey.Invoke(null, [(Keys)Keys.F12]));
    }

    [Fact]
    public void ShortcutKeysUI_OnResetButtonClick_ClearsCtrlCheckBox()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        MethodInfo onResetButtonClick = GetShortcutKeysUIType()
            .GetMethod("OnResetButtonClick", NonPublicInstance);

        start.Invoke(shortcutKeysUI, [(object)(Keys.Control | Keys.A)]);
        onResetButtonClick.Invoke(shortcutKeysUI, [null, EventArgs.Empty]);

        CheckBox ctrl = GetCheckBox(shortcutKeysUI, "_ctrlCheckBox");
        Assert.False(ctrl.Checked);
    }

    [Fact]
    public void ShortcutKeysUI_OnResetButtonClick_ClearsAltCheckBox()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        MethodInfo onResetButtonClick = GetShortcutKeysUIType()
            .GetMethod("OnResetButtonClick", NonPublicInstance);

        start.Invoke(shortcutKeysUI, [(object)(Keys.Alt | Keys.B)]);
        onResetButtonClick.Invoke(shortcutKeysUI, [null, EventArgs.Empty]);

        CheckBox alt = GetCheckBox(shortcutKeysUI, "_altCheckBox");
        Assert.False(alt.Checked);
    }

    [Fact]
    public void ShortcutKeysUI_OnResetButtonClick_ClearsShiftCheckBox()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        MethodInfo onResetButtonClick = GetShortcutKeysUIType()
            .GetMethod("OnResetButtonClick", NonPublicInstance);

        start.Invoke(shortcutKeysUI, [(object)(Keys.Shift | Keys.C)]);
        onResetButtonClick.Invoke(shortcutKeysUI, [null, EventArgs.Empty]);

        CheckBox shift = GetCheckBox(shortcutKeysUI, "_shiftCheckBox");
        Assert.False(shift.Checked);
    }

    [Fact]
    public void ShortcutKeysUI_OnResetButtonClick_ClearsKeyComboBoxSelection()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        MethodInfo onResetButtonClick = GetShortcutKeysUIType()
            .GetMethod("OnResetButtonClick", NonPublicInstance);

        start.Invoke(shortcutKeysUI, [(object)Keys.D]);
        onResetButtonClick.Invoke(shortcutKeysUI, [null, EventArgs.Empty]);

        ComboBox keyComboBox = GetComboBox(shortcutKeysUI, "_keyComboBox");
        Assert.Equal(-1, keyComboBox.SelectedIndex);
    }

    [WinFormsFact]
    public void ShortcutKeysUI_OnResetButtonClick_ClearsAllCheckBoxesAtOnce()
    {
        using Control shortcutKeysUI = CastToControl(CreateShortcutKeysUI());
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);

        start.Invoke(shortcutKeysUI, [(object)(Keys.Control | Keys.Alt | Keys.Shift | Keys.E)]);
        GetButton(shortcutKeysUI, "_resetButton").TestAccessor.Dynamic.OnClick(EventArgs.Empty);

        CheckBox ctrl = GetCheckBox(shortcutKeysUI, "_ctrlCheckBox");
        CheckBox alt = GetCheckBox(shortcutKeysUI, "_altCheckBox");
        CheckBox shift = GetCheckBox(shortcutKeysUI, "_shiftCheckBox");
        ComboBox keyComboBox = GetComboBox(shortcutKeysUI, "_keyComboBox");

        Assert.False(ctrl.Checked);
        Assert.False(alt.Checked);
        Assert.False(shift.Checked);
        Assert.Equal(-1, keyComboBox.SelectedIndex);
        Assert.Equal(Keys.None, GetValue(shortcutKeysUI));
    }

    [Fact]
    public void ShortcutKeysUI_OnGotFocus_DoesNotThrow()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo onGotFocus = GetShortcutKeysUIType()
            .GetMethod("OnGotFocus", NonPublicInstance);
        onGotFocus.Invoke(shortcutKeysUI, [EventArgs.Empty]);
    }

    [WinFormsFact]
    public void ShortcutKeysUI_OnGotFocus_SetsFocusToCtrlCheckBox()
    {
        using Control shortcutKeysUI = CastToControl(CreateShortcutKeysUI());
        using Form host = new();
        host.Controls.Add(shortcutKeysUI);
        host.Show();
        Button reset = GetButton(shortcutKeysUI, "_resetButton");
        Assert.True(reset.Focus());
        Assert.True(reset.Focused);

        shortcutKeysUI.TestAccessor.Dynamic.OnGotFocus(EventArgs.Empty);
        Assert.True(GetCheckBox(shortcutKeysUI, "_ctrlCheckBox").Focused);
        Assert.False(reset.Focused);
    }

    [WinFormsTheory]
    [InlineData("_ctrlCheckBox", Keys.Tab | Keys.Shift, "_resetButton")]
    [InlineData("_ctrlCheckBox", Keys.Left, "_resetButton")]
    [InlineData("_altCheckBox", Keys.Right, "_keyComboBox")]
    [InlineData("_resetButton", Keys.Right, "_ctrlCheckBox")]
    public void ShortcutKeysUI_ProcessDialogKey_Navigation_MovesFocus(string from, Keys keyData, string destination)
    {
        using Control shortcutKeysUI = CastToControl(CreateShortcutKeysUI());
        using Form host = new();
        host.Controls.Add(shortcutKeysUI);
        host.Show();

        var source = (Control)GetFieldValue(shortcutKeysUI, from);
        Assert.True(source.Focus());
        Assert.True(source.Focused);

        bool result = shortcutKeysUI.TestAccessor.Dynamic.ProcessDialogKey(keyData);
        Assert.True(result);
        Assert.True(((Control)GetFieldValue(shortcutKeysUI, destination)).Focused);
        Assert.False(source.Focused);
    }

    [WinFormsTheory]
    [InlineData(false, false, Keys.Escape, true)]
    [InlineData(true, false, Keys.Escape, true)]
    [InlineData(true, true, Keys.Escape, false)]
    [InlineData(true, true, Keys.Escape | Keys.Control, true)]
    [InlineData(true, true, Keys.Escape | Keys.Alt, true)]
    public void ShortcutKeysUI_ProcessDialogKey_Escape_RestoresExceptOpenFocusedCombo(
        bool focusCombo, bool droppedDown, Keys keyData, bool restoresOriginal)
    {
        using Control shortcutKeysUI = CastToControl(CreateShortcutKeysUI());
        using Form host = new();
        host.Controls.Add(shortcutKeysUI);
        const Keys Original = Keys.Control | Keys.Z;
        Start(shortcutKeysUI, Original);
        host.Show();
        CheckBox ctrl = GetCheckBox(shortcutKeysUI, "_ctrlCheckBox");
        ctrl.Checked = false;
        ComboBox combo = GetComboBox(shortcutKeysUI, "_keyComboBox");
        combo.SelectedIndex = 0;
        Assert.Equal(Keys.A, GetValue(shortcutKeysUI));

        Assert.True((focusCombo ? (Control)combo : ctrl).Focus());
        combo.DroppedDown = droppedDown;
        Assert.Equal(focusCombo, combo.Focused);
        Assert.Equal(droppedDown, combo.DroppedDown);
        shortcutKeysUI.TestAccessor.Dynamic.ProcessDialogKey(keyData);

        Assert.Equal(restoresOriginal ? Original : Keys.A, GetValue(shortcutKeysUI));
        combo.DroppedDown = false;
    }

    [WinFormsFact]
    public void ShortcutKeysUI_EditValue_FirstCall_SetsBackColor()
    {
        ShortcutKeysEditor editor = new();
        using Form host = new();
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(mockEditorService.Object);
        mockEditorService
            .Setup(e => e.DropDownControl(It.IsAny<Control>()))
            .Callback<Control>(control =>
            {
                host.Controls.Add(control);
                Assert.Equal(SystemColors.Control, control.BackColor);
            });

        _ = editor.EditValue(null, mockServiceProvider.Object, Keys.A);
    }

    [WinFormsFact]
    public void ShortcutKeysEditor_EditValue_ClickSelection_ReturnsDifferentValueAndReusesEditor()
    {
        ShortcutKeysEditor editor = new();
        using Form host = new();
        Mock<IWindowsFormsEditorService> service = new(MockBehavior.Strict);
        Mock<IServiceProvider> provider = new(MockBehavior.Strict);
        provider.Setup(p => p.GetService(typeof(IWindowsFormsEditorService))).Returns(service.Object);
        Control firstUI = null;
        int calls = 0;
        service.Setup(s => s.DropDownControl(It.IsAny<Control>())).Callback<Control>(control =>
        {
            if (firstUI is null)
            {
                firstUI = control;
                host.Controls.Add(control);
                host.Show();
            }
            else
            {
                Assert.Same(firstUI, control);
            }

            calls++;
            GetCheckBox(control, "_altCheckBox").TestAccessor.Dynamic.OnClick(EventArgs.Empty);
            ComboBox combo = GetComboBox(control, "_keyComboBox");
            combo.SelectedItem = TypeDescriptor.GetConverter(typeof(Keys)).ConvertToString(Keys.B);
        });

        Assert.Equal(Keys.Alt | Keys.B, editor.EditValue(null, provider.Object, Keys.Back));
        Assert.Equal(Keys.Control | Keys.Alt | Keys.B, editor.EditValue(null, provider.Object, Keys.Control | Keys.A));
        Assert.Equal(2, calls);
    }

    [Fact]
    public void ShortcutKeysUI_Start_KeyValue_PreservesOriginalValue()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);

        Keys input = Keys.Control | Keys.A;
        start.Invoke(shortcutKeysUI, [(object)input]);

        Assert.Equal(input, (Keys)GetFieldValue(shortcutKeysUI, "_originalValue"));
        Assert.Equal(input, (Keys)GetFieldValue(shortcutKeysUI, "_currentValue"));
    }

    [Fact]
    public void ShortcutKeysUI_Start_SetsValueTypeAsKeys()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        MethodInfo start = GetShortcutKeysUIType().GetMethod("Start", PublicInstance);
        PropertyInfo valueProperty = GetShortcutKeysUIType().GetProperty("Value", PublicInstance);

        start.Invoke(shortcutKeysUI, [(object)Keys.F]);
        object value = valueProperty.GetValue(shortcutKeysUI);

        Assert.IsType<Keys>(value);
    }

    [Fact]
    public void ShortcutKeysUI_AdjustSize_ResetsButtonSize()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        var control = CastToControl(shortcutKeysUI);
        Assert.True(control.Width > 0);
    }

    [Fact]
    public void ShortcutKeysUI_Constructor_KeyComboBox_HasAtLeastOneHundredValidKeys()
    {
        object shortcutKeysUI = CreateShortcutKeysUI();
        ComboBox keyComboBox = GetComboBox(shortcutKeysUI, "_keyComboBox");
        Assert.True(keyComboBox.Items.Count >= 50);
    }
}
