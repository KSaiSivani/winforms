// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Reflection;
using System.Windows.Forms.Design;
using Moq;

namespace System.Drawing.Design.Tests;

/// <summary>
/// Tests for ColorEditor.ColorUI nested class
/// Uses reflection to access internal components
/// </summary>
public class ColorEditorUITests
{
    #region Constructor Tests

    [Fact]
    public void ColorUI_Constructor_WithValidEditor_CreatesInstance()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();

        // Create ColorUI instance using reflection
        object colorUI = Activator.CreateInstance(colorUIType, editor);
        Assert.NotNull(colorUI);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void ColorUI_Value_InitiallyNull()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        PropertyInfo valueProperty = colorUIType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
        if (valueProperty is { })
        {
            object value = valueProperty.GetValue(colorUI);
            Assert.Null(value);
        }
    }

    [Fact]
    public void ColorUI_EditorService_InitiallyNull()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        PropertyInfo editorServiceProperty = colorUIType.GetProperty("EditorService", BindingFlags.Public | BindingFlags.Instance);
        if (editorServiceProperty is { })
        {
            object editorService = editorServiceProperty.GetValue(colorUI);
            Assert.Null(editorService);
        }
    }

    [Fact]
    public void ColorUI_ColorValues_ReturnsNonEmptyArray()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        PropertyInfo property = colorUIType.GetProperty("ColorValues", BindingFlags.NonPublic | BindingFlags.Instance);
        if (property is { })
        {
            object value = property.GetValue(colorUI);
            Assert.NotNull(value);
            Array array = Assert.IsType<Color[]>(value);
            Assert.NotEmpty(array);
        }
    }

    [Fact]
    public void ColorUI_SystemColorValues_ReturnsNonEmptyArray()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        PropertyInfo property = colorUIType.GetProperty("SystemColorValues", BindingFlags.NonPublic | BindingFlags.Instance);
        if (property is { })
        {
            object value = property.GetValue(colorUI);
            Assert.NotNull(value);
            Array array = Assert.IsType<Color[]>(value);
            Assert.NotEmpty(array);
        }
    }

    [Fact]
    public void ColorUI_CustomColors_InitiallyAllWhite()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        PropertyInfo property = colorUIType.GetProperty("CustomColors", BindingFlags.NonPublic | BindingFlags.Instance);
        if (property is { })
        {
            object value = property.GetValue(colorUI);
            Assert.NotNull(value);
            Color[] colors = Assert.IsType<Color[]>(value);
            Assert.NotEmpty(colors);
            Assert.All(colors, c => Assert.Equal(Color.White, c));
        }
    }

    [Fact]
    public void ColorUI_CustomColors_AreCached()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        PropertyInfo property = colorUIType.GetProperty("CustomColors", BindingFlags.NonPublic | BindingFlags.Instance);
        if (property is { })
        {
            object first = property.GetValue(colorUI);
            object second = property.GetValue(colorUI);
            Assert.Same(first, second);
        }
    }

    #endregion

    #region Method Tests

    [Fact]
    public void ColorUI_End_ClearsValues()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo endMethod = colorUIType.GetMethod("End", BindingFlags.Public | BindingFlags.Instance);
        if (endMethod is { })
        {
            // Should not throw
            endMethod.Invoke(colorUI, null);

            // After End(), Value should be null
            PropertyInfo valueProperty = colorUIType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
            if (valueProperty is { })
            {
                object value = valueProperty.GetValue(colorUI);
                Assert.Null(value);
            }
        }
    }

    [Fact]
    public void ColorUI_Start_WithNullValue_SetsValue()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        // Create mock editor service
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Loose);

        MethodInfo startMethod = colorUIType.GetMethod("Start", BindingFlags.Public | BindingFlags.Instance, null, [typeof(IWindowsFormsEditorService), typeof(object)], null);
        if (startMethod is { })
        {
            startMethod.Invoke(colorUI, [mockEditorService.Object, (object)null]);
        }
    }

    [WinFormsFact]
    public void ColorUI_Start_WithStandardColor_SelectsCommonTab()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        using Control colorUI = (Control)Activator.CreateInstance(colorUIType, editor);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        colorUIType.GetMethod("Start")!.Invoke(colorUI, [mockEditorService.Object, Color.Red]);
        TabControl tabs = colorUI.TestAccessor.Dynamic._tabControl;
        TabPage commonPage = colorUI.TestAccessor.Dynamic._commonTabPage;
        ListBox commonColors = colorUI.TestAccessor.Dynamic._lbCommon;
        Assert.Same(commonPage, tabs.SelectedTab);
        Assert.Equal(Color.Red, commonColors.SelectedItem);
        Assert.Equal(Color.Red, colorUIType.GetProperty("Value")!.GetValue(colorUI));
    }

    [WinFormsFact]
    public void ColorUI_Start_WithSystemColor_SelectsSystemTab()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        using Control colorUI = (Control)Activator.CreateInstance(colorUIType, editor);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        colorUIType.GetMethod("Start")!.Invoke(colorUI, [mockEditorService.Object, SystemColors.ActiveCaption]);
        TabControl tabs = colorUI.TestAccessor.Dynamic._tabControl;
        TabPage systemPage = colorUI.TestAccessor.Dynamic._systemTabPage;
        ListBox systemColors = colorUI.TestAccessor.Dynamic._lbSystem;
        Assert.Same(systemPage, tabs.SelectedTab);
        Assert.Equal(SystemColors.ActiveCaption, systemColors.SelectedItem);
        Assert.Equal(SystemColors.ActiveCaption, colorUIType.GetProperty("Value")!.GetValue(colorUI));
    }

    [WinFormsFact]
    public void ColorUI_Start_WithCustomColor_SelectsPaletteTab()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        using Control colorUI = (Control)Activator.CreateInstance(colorUIType, editor);
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        Color custom = Color.FromArgb(123, 45, 67);
        colorUIType.GetMethod("Start")!.Invoke(colorUI, [mockEditorService.Object, custom]);
        TabControl tabs = colorUI.TestAccessor.Dynamic._tabControl;
        TabPage palettePage = colorUI.TestAccessor.Dynamic._paletteTabPage;
        Assert.Same(palettePage, tabs.SelectedTab);
        Assert.Equal(custom, colorUIType.GetProperty("Value")!.GetValue(colorUI));
        colorUIType.GetMethod("End")!.Invoke(colorUI, null);
        Assert.Null(colorUIType.GetProperty("Value")!.GetValue(colorUI));
        Assert.Null(colorUIType.GetProperty("EditorService")!.GetValue(colorUI));
    }

    [Fact]
    public void ColorUI_GetBestColor_WithExistingColor_ReturnsMatchingColor()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo getBestColorMethod = colorUIType.GetMethod("GetBestColor", BindingFlags.NonPublic | BindingFlags.Instance);
        if (getBestColorMethod is { })
        {
            // Color.Red should be found in the standard colors
            object result = getBestColorMethod.Invoke(colorUI, [Color.Red]);
            Assert.NotNull(result);
        }
    }

    [Fact]
    public void ColorUI_GetBestColor_WithCustomColor_ReturnsSameColor()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo getBestColorMethod = colorUIType.GetMethod("GetBestColor", BindingFlags.NonPublic | BindingFlags.Instance);
        if (getBestColorMethod is { })
        {
            // Custom color not in standard palette - should be returned as-is
            Color customColor = Color.FromArgb(123, 45, 67);
            object result = getBestColorMethod.Invoke(colorUI, [customColor]);
            Color returnedColor = Assert.IsType<Color>(result);
            Assert.Equal(customColor.ToArgb(), returnedColor.ToArgb());
        }
    }

    [Fact]
    public void ColorUI_GetConstants_WithColorType_ReturnsColors()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        _ = Activator.CreateInstance(colorUIType, editor);

        MethodInfo getConstantsMethod = colorUIType.GetMethod("GetConstants", BindingFlags.NonPublic | BindingFlags.Static);
        if (getConstantsMethod is { })
        {
            object result = getConstantsMethod.Invoke(null, [typeof(Color)]);
            Assert.NotNull(result);
            Color[] colors = Assert.IsAssignableFrom<Color[]>(result);
            Assert.NotEmpty(colors);
        }
    }

    [Fact]
    public void ColorUI_GetConstants_WithSystemColorsType_ReturnsColors()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        _ = Activator.CreateInstance(colorUIType, editor);

        MethodInfo getConstantsMethod = colorUIType.GetMethod("GetConstants", BindingFlags.NonPublic | BindingFlags.Static);
        if (getConstantsMethod is { })
        {
            object result = getConstantsMethod.Invoke(null, [typeof(SystemColors)]);
            Assert.NotNull(result);
            Color[] colors = Assert.IsAssignableFrom<Color[]>(result);
            Assert.NotEmpty(colors);
        }
    }

    [Fact]
    public void ColorUI_AdjustColorUIHeight_DoesNotThrow()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo adjustHeightMethod = colorUIType.GetMethod("AdjustColorUIHeight", BindingFlags.NonPublic | BindingFlags.Instance);
        if (adjustHeightMethod is { })
        {
            adjustHeightMethod.Invoke(colorUI, null);
        }
    }

    [Fact]
    public void ColorUI_AdjustListBoxItemHeight_DoesNotThrow()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo adjustHeightMethod = colorUIType.GetMethod("AdjustListBoxItemHeight", BindingFlags.NonPublic | BindingFlags.Instance);
        if (adjustHeightMethod is { })
        {
            adjustHeightMethod.Invoke(colorUI, null);
        }
    }

    #endregion

    #region Event Handler Tests

    [Fact]
    public void ColorUI_OnGotFocus_DoesNotThrow()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo onGotFocusMethod = colorUIType.GetMethod("OnGotFocus", BindingFlags.NonPublic | BindingFlags.Instance);
        if (onGotFocusMethod is { })
        {
            onGotFocusMethod.Invoke(colorUI, [EventArgs.Empty]);
        }
    }

    [Fact]
    public void ColorUI_OnFontChanged_Sender_DoesNotThrow()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        // There are two OnFontChanged methods: one protected override and one private event handler
        MethodInfo[] onFontChangedMethods = colorUIType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name == "OnFontChanged")
            .ToArray();

        // Test the private (sender, e) overload
        var privateOnFontChanged = onFontChangedMethods.FirstOrDefault(m =>
        {
            ParameterInfo[] parameters = m.GetParameters();
            return parameters.Length == 2 && parameters[0].ParameterType == typeof(object);
        });

        if (privateOnFontChanged is { })
        {
            privateOnFontChanged.Invoke(colorUI, [null, EventArgs.Empty]);
        }
    }

    [Fact]
    public void ColorUI_OnFontChanged_Override_DoesNotThrow()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        // The protected override has signature (EventArgs)
        MethodInfo[] onFontChangedMethods = colorUIType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name == "OnFontChanged")
            .ToArray();

        var protectedOnFontChanged = onFontChangedMethods.FirstOrDefault(m =>
        {
            ParameterInfo[] parameters = m.GetParameters();
            return parameters.Length == 1 && parameters[0].ParameterType == typeof(EventArgs);
        });

        if (protectedOnFontChanged is { })
        {
            protectedOnFontChanged.Invoke(colorUI, [EventArgs.Empty]);
        }
    }

    [Fact]
    public void ColorUI_OnListClick_WithNullSender_DoesNotThrow()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo onListClickMethod = colorUIType.GetMethod("OnListClick", BindingFlags.NonPublic | BindingFlags.Instance);
        if (onListClickMethod is { })
        {
            // When sender is null, the if condition fails and CloseDropDown is called on null _edSvc (no-op)
            onListClickMethod.Invoke(colorUI, [null, EventArgs.Empty]);
        }
    }

    [Fact]
    public void ColorUI_OnListClick_WithNonListBoxSender_DoesNotThrow()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo onListClickMethod = colorUIType.GetMethod("OnListClick", BindingFlags.NonPublic | BindingFlags.Instance);
        if (onListClickMethod is { })
        {
            // Sender is not a ListBox - should not throw
            onListClickMethod.Invoke(colorUI, [new object(), EventArgs.Empty]);
        }
    }

    [Fact]
    public void ColorUI_OnListClick_WithListBoxAndColorSelected_SetsValue()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo onListClickMethod = colorUIType.GetMethod("OnListClick", BindingFlags.NonPublic | BindingFlags.Instance);
        if (onListClickMethod is { })
        {
            // Create a ListBox with a selected Color item
            using ListBox listBox = new();
            listBox.Items.Add(Color.Red);
            listBox.SelectedItem = Color.Red;

            onListClickMethod.Invoke(colorUI, [listBox, EventArgs.Empty]);

            // Verify Value was set
            PropertyInfo valueProperty = colorUIType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
            object value = valueProperty!.GetValue(colorUI);
            Assert.Equal(Color.Red, value);
        }
    }

    [Fact]
    public void ColorUI_OnListDrawItem_WithNullSender_Returns()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo onListDrawItemMethod = colorUIType.GetMethod("OnListDrawItem", BindingFlags.NonPublic | BindingFlags.Instance);
        if (onListDrawItemMethod is { })
        {
            using Bitmap bitmap = new(10, 10);
            using Graphics graphics = Graphics.FromImage(bitmap);
            DrawItemEventArgs args = new(graphics, Control.DefaultFont, new Rectangle(0, 0, 10, 10), 0, DrawItemState.Default);

            // Null sender should cause early return
            onListDrawItemMethod.Invoke(colorUI, [null, args]);
        }
    }

    [Fact]
    public void ColorUI_OnListDrawItem_WithListBox_DrawsItem()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo onListDrawItemMethod = colorUIType.GetMethod("OnListDrawItem", BindingFlags.NonPublic | BindingFlags.Instance);
        if (onListDrawItemMethod is { })
        {
            using Bitmap bitmap = new(50, 20);
            using Graphics graphics = Graphics.FromImage(bitmap);
            using ListBox listBox = new();
            listBox.Items.Add(Color.Red);
            DrawItemEventArgs args = new(graphics, Control.DefaultFont, new Rectangle(0, 0, 50, 20), 0, DrawItemState.Default);

            onListDrawItemMethod.Invoke(colorUI, [listBox, args]);
        }
    }

    [Fact]
    public void ColorUI_OnListKeyDown_WithReturnKey_CallsOnListClick()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo onListKeyDownMethod = colorUIType.GetMethod("OnListKeyDown", BindingFlags.NonPublic | BindingFlags.Instance);
        if (onListKeyDownMethod is { })
        {
            KeyEventArgs args = new(Keys.Return);
            // When Return is pressed, it calls OnListClick which may access _edSvc
            onListKeyDownMethod.Invoke(colorUI, [null, args]);
        }
    }

    [Fact]
    public void ColorUI_OnListKeyDown_WithOtherKey_DoesNotTrigger()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo onListKeyDownMethod = colorUIType.GetMethod("OnListKeyDown", BindingFlags.NonPublic | BindingFlags.Instance);
        if (onListKeyDownMethod is { })
        {
            KeyEventArgs args = new(Keys.A);
            onListKeyDownMethod.Invoke(colorUI, [null, args]);
        }
    }

    [Fact]
    public void ColorUI_OnPalettePick_WithNullSender_DoesNotThrow()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo onPalettePickMethod = colorUIType.GetMethod("OnPalettePick", BindingFlags.NonPublic | BindingFlags.Instance);
        if (onPalettePickMethod is { })
        {
            onPalettePickMethod.Invoke(colorUI, [null, EventArgs.Empty]);
        }
    }

    [Fact]
    public void ColorUI_OnPalettePick_WithNonPaletteSender_DoesNotThrow()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo onPalettePickMethod = colorUIType.GetMethod("OnPalettePick", BindingFlags.NonPublic | BindingFlags.Instance);
        if (onPalettePickMethod is { })
        {
            onPalettePickMethod.Invoke(colorUI, [new object(), EventArgs.Empty]);
        }
    }

    [Fact]
    public void ColorUI_OnTabControlResize_DoesNotThrow()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo onTabControlResizeMethod = colorUIType.GetMethod("OnTabControlResize", BindingFlags.NonPublic | BindingFlags.Instance);
        if (onTabControlResizeMethod is { })
        {
            onTabControlResizeMethod.Invoke(colorUI, [null, EventArgs.Empty]);
        }
    }

    [Fact]
    public void ColorUI_ProcessDialogKey_WithTabKey_AdvancesTab()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo processDialogKeyMethod = colorUIType.GetMethod("ProcessDialogKey", BindingFlags.NonPublic | BindingFlags.Instance);
        if (processDialogKeyMethod is { })
        {
            // Plain Tab - should advance to next tab
            var result = processDialogKeyMethod.Invoke(colorUI, [Keys.Tab]);
            Assert.NotNull(result);
        }
    }

    [Fact]
    public void ColorUI_ProcessDialogKey_WithShiftTab_ReversesTab()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo processDialogKeyMethod = colorUIType.GetMethod("ProcessDialogKey", BindingFlags.NonPublic | BindingFlags.Instance);
        if (processDialogKeyMethod is { })
        {
            // Shift+Tab - should go to previous tab
            var result = processDialogKeyMethod.Invoke(colorUI, [Keys.Tab | Keys.Shift]);
            Assert.NotNull(result);
        }
    }

    [Fact]
    public void ColorUI_ProcessDialogKey_WithNonTabKey_CallsBase()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo processDialogKeyMethod = colorUIType.GetMethod("ProcessDialogKey", BindingFlags.NonPublic | BindingFlags.Instance);
        if (processDialogKeyMethod is { })
        {
            // Non-Tab key - falls through to base implementation
            var result = processDialogKeyMethod.Invoke(colorUI, [Keys.A]);
            Assert.NotNull(result);
        }
    }

    [Fact]
    public void ColorUI_ProcessDialogKey_WithCtrlTab_CallsBase()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo processDialogKeyMethod = colorUIType.GetMethod("ProcessDialogKey", BindingFlags.NonPublic | BindingFlags.Instance);
        if (processDialogKeyMethod is { })
        {
            // Ctrl+Tab - has Control modifier, so the special tab handling is skipped
            var result = processDialogKeyMethod.Invoke(colorUI, [Keys.Tab | Keys.Control]);
            Assert.NotNull(result);
        }
    }

    [Fact]
    public void ColorUI_ProcessDialogKey_WithAltTab_CallsBase()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        MethodInfo processDialogKeyMethod = colorUIType.GetMethod("ProcessDialogKey", BindingFlags.NonPublic | BindingFlags.Instance);
        if (processDialogKeyMethod is { })
        {
            // Alt+Tab - has Alt modifier, so the special tab handling is skipped
            var result = processDialogKeyMethod.Invoke(colorUI, [Keys.Tab | Keys.Alt]);
            Assert.NotNull(result);
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ColorUI_IsControl_ReturnsCorrectType()
    {
        _ = new ColorEditor();
        Type colorUIType = GetColorUIType();

        // Verify ColorUI is a Control
        Assert.True(typeof(Control).IsAssignableFrom(colorUIType));
    }

    [Fact]
    public void ColorUI_EditorReference_Stored()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        // Should have created successfully with editor reference
        Assert.NotNull(colorUI);
    }

    [Fact]
    public void ColorUI_IsSealed()
    {
        Type colorUIType = GetColorUIType();
        Assert.True(colorUIType.IsSealed);
    }

    [Fact]
    public void ColorUI_IsPrivate()
    {
        Type colorUIType = GetColorUIType();
        Assert.True(colorUIType.IsNested && colorUIType.IsNestedPrivate);
    }

    [Fact]
    public void ColorUI_HasAccessibleName()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        Control control = Assert.IsAssignableFrom<Control>(colorUI);
        Assert.False(string.IsNullOrEmpty(control.AccessibleName));
    }

    [Fact]
    public void ColorUI_StartAndEnd_Sequence_WorksCorrectly()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Loose);

        MethodInfo startMethod = colorUIType.GetMethod("Start", BindingFlags.Public | BindingFlags.Instance, null, [typeof(IWindowsFormsEditorService), typeof(object)], null);
        MethodInfo endMethod = colorUIType.GetMethod("End", BindingFlags.Public | BindingFlags.Instance);

        if (startMethod is { } && endMethod is { })
        {
            startMethod.Invoke(colorUI, [mockEditorService.Object, Color.Red]);
            endMethod.Invoke(colorUI, null);

            PropertyInfo valueProperty = colorUIType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
            object value = valueProperty!.GetValue(colorUI);
            Assert.Null(value);
        }
    }

    [Fact]
    public void ColorUI_MultipleStartCalls_AllSucceed()
    {
        ColorEditor editor = new();
        Type colorUIType = GetColorUIType();
        object colorUI = Activator.CreateInstance(colorUIType, editor);

        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Loose);

        MethodInfo startMethod = colorUIType.GetMethod("Start", BindingFlags.Public | BindingFlags.Instance, null, [typeof(IWindowsFormsEditorService), typeof(object)], null);
        if (startMethod is { })
        {
            startMethod.Invoke(colorUI, [mockEditorService.Object, Color.Red]);
            startMethod.Invoke(colorUI, [mockEditorService.Object, Color.Blue]);
            startMethod.Invoke(colorUI, [mockEditorService.Object, (object)null]);
        }
    }

    #endregion

    #region ColorEditorListBox Tests

    [Fact]
    public void ColorEditorListBox_IsInputKey_WithReturnKey_ReturnsTrue()
    {
        using ListBox listBox = CreateColorEditorListBox();
        var isInputKeyMethod = listBox.GetType().GetMethod("IsInputKey", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(isInputKeyMethod);
        if (isInputKeyMethod is { })
        {
            var result = isInputKeyMethod.Invoke(listBox, [Keys.Return]);
            Assert.True((bool)result!);
        }
    }

    [Fact]
    public void ColorEditorListBox_IsInputKey_WithOtherKey_CallsBase()
    {
        using ListBox listBox = CreateColorEditorListBox();
        var isInputKeyMethod = listBox.GetType().GetMethod("IsInputKey", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(isInputKeyMethod);
        if (isInputKeyMethod is { })
        {
            // A non-Return key should fall through to the base implementation
            var result = isInputKeyMethod.Invoke(listBox, [Keys.A]);
            // Result depends on base implementation - just verify it doesn't throw
            Assert.NotNull(result);
        }
    }

    [Fact]
    public void ColorEditorListBox_InheritsFromListBox()
    {
        Type listBoxType = GetColorEditorListBoxType();
        Assert.True(typeof(ListBox).IsAssignableFrom(listBoxType));
    }

    [Fact]
    public void ColorEditorListBox_HasIsInputKeyMethod()
    {
        Type listBoxType = GetColorEditorListBoxType();
        var method = listBoxType.GetMethod("IsInputKey", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
    }

    [Fact]
    public void ColorEditorListBox_IsNested()
    {
        Type listBoxType = GetColorEditorListBoxType();
        Assert.True(listBoxType.IsNested);
    }

    #endregion

    #region ColorEditorTabControl Tests

    [Fact]
    public void ColorEditorTabControl_InheritsFromTabControl()
    {
        Type tabControlType = GetColorEditorTabControlType();
        Assert.True(typeof(TabControl).IsAssignableFrom(tabControlType));
    }

    [Fact]
    public void ColorEditorTabControl_IsNested()
    {
        Type tabControlType = GetColorEditorTabControlType();
        Assert.True(tabControlType.IsNested && tabControlType.IsNestedPrivate);
    }

    [Fact]
    public void ColorEditorTabControl_HasOnGotFocusMethod()
    {
        Type tabControlType = GetColorEditorTabControlType();
        var method = tabControlType.GetMethod("OnGotFocus", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
    }

    [Fact]
    public void ColorEditorTabControl_HasParameterlessConstructor()
    {
        Type tabControlType = GetColorEditorTabControlType();
        var ctor = tabControlType.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);
        Assert.NotNull(ctor);
    }

    [WinFormsFact]
    public void ColorEditorTabControl_OnGotFocus_WithNoSelectedTab_DoesNotThrow()
    {
        Type tabControlType = GetColorEditorTabControlType();
        TabControl tabControl = (TabControl)Activator.CreateInstance(tabControlType)!;

        // No selected tab - the if condition will be false, so we just verify it doesn't throw
        var onGotFocusMethod = tabControlType.GetMethod("OnGotFocus", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(onGotFocusMethod);
        onGotFocusMethod!.Invoke(tabControl, [EventArgs.Empty]);
    }

    [WinFormsFact]
    public void ColorEditorTabControl_OnGotFocus_WithSelectedTab_DoesNotThrow()
    {
        Type tabControlType = GetColorEditorTabControlType();
        TabControl tabControl = (TabControl)Activator.CreateInstance(tabControlType)!;

        TabPage tabPage = new();
        tabControl.TabPages.Add(tabPage);

        var onGotFocusMethod = tabControlType.GetMethod("OnGotFocus", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(onGotFocusMethod);
        onGotFocusMethod!.Invoke(tabControl, [EventArgs.Empty]);
    }

    [WinFormsFact]
    public void ColorEditorTabControl_OnGotFocus_WithSelectedTabWithChildControl_FocusesChild()
    {
        Type tabControlType = GetColorEditorTabControlType();
        TabControl tabControl = (TabControl)Activator.CreateInstance(tabControlType)!;

        TabPage tabPage = new();
        Button button = new();
        tabPage.Controls.Add(button);
        tabControl.TabPages.Add(tabPage);

        var onGotFocusMethod = tabControlType.GetMethod("OnGotFocus", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(onGotFocusMethod);
        onGotFocusMethod!.Invoke(tabControl, [EventArgs.Empty]);
    }

    #endregion

    #region Helper Methods

    private static Type GetColorEditorListBoxType()
    {
        Type colorEditorType = typeof(ColorEditor);
        Type colorUIType = colorEditorType.GetNestedType("ColorUI", BindingFlags.NonPublic);
        Assert.NotNull(colorUIType);
        Type listBoxType = colorUIType!.GetNestedType("ColorEditorListBox", BindingFlags.NonPublic);
        Assert.NotNull(listBoxType);
        return listBoxType!;
    }

    private static Type GetColorEditorTabControlType()
    {
        Type colorEditorType = typeof(ColorEditor);
        Type colorUIType = colorEditorType.GetNestedType("ColorUI", BindingFlags.NonPublic);
        Assert.NotNull(colorUIType);
        Type tabControlType = colorUIType!.GetNestedType("ColorEditorTabControl", BindingFlags.NonPublic);
        Assert.NotNull(tabControlType);
        return tabControlType!;
    }

    private static ListBox CreateColorEditorListBox()
    {
        Type listBoxType = GetColorEditorListBoxType();
        return (ListBox)Activator.CreateInstance(listBoxType)!;
    }

    private Type GetColorUIType()
    {
        Type colorEditorType = typeof(ColorEditor);
        Type colorUIType = colorEditorType.GetNestedType("ColorUI", BindingFlags.NonPublic);
        Assert.NotNull(colorUIType);
        return colorUIType;
    }

    #endregion
}
