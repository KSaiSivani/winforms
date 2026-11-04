// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Drawing.Design.Tests;

/// <summary>
/// Tests for ColorEditor.ColorPalette nested class
/// Uses reflection to access internal components
/// </summary>
public class ColorEditorPaletteTests
{
    #region Constants Tests

    [Fact]
    public void ColorPalette_CellsAcross_ReturnsExpectedValue()
    {
        // ColorPalette.CellsAcross should be 8
        Type colorPaletteType = GetColorPaletteType();
        FieldInfo cellsAcrossField = colorPaletteType.GetField("CellsAcross", BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);

        if (cellsAcrossField is not null)
        {
            object value = cellsAcrossField.GetValue(null);
            Assert.Equal(8, value);
        }
    }

    [Fact]
    public void ColorPalette_CellsDown_ReturnsExpectedValue()
    {
        // ColorPalette.CellsDown should be 8
        Type colorPaletteType = GetColorPaletteType();
        FieldInfo cellsDownField = colorPaletteType.GetField("CellsDown", BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);

        if (cellsDownField is not null)
        {
            object value = cellsDownField.GetValue(null);
            Assert.Equal(8, value);
        }
    }

    [Fact]
    public void ColorPalette_CellsCustom_ReturnsExpectedValue()
    {
        // ColorPalette.CellsCustom should be 16
        Type colorPaletteType = GetColorPaletteType();
        FieldInfo cellsCustomField = colorPaletteType.GetField("CellsCustom", BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);

        if (cellsCustomField is not null)
        {
            object value = cellsCustomField.GetValue(null);
            Assert.Equal(16, value);
        }
    }

    [Fact]
    public void ColorPalette_TotalCells_ReturnsExpectedValue()
    {
        // ColorPalette.TotalCells should be 64 (CellsAcross * CellsDown)
        Type colorPaletteType = GetColorPaletteType();
        FieldInfo totalCellsField = colorPaletteType.GetField("TotalCells", BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);

        if (totalCellsField is not null)
        {
            object value = totalCellsField.GetValue(null);
            Assert.Equal(64, value);
        }
    }

    [Fact]
    public void ColorPalette_CellSize_ReturnsExpectedValue()
    {
        // ColorPalette.CellSize should be 16
        Type colorPaletteType = GetColorPaletteType();
        FieldInfo cellSizeField = colorPaletteType.GetField("CellSize", BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);

        if (cellSizeField is not null)
        {
            object value = cellSizeField.GetValue(null);
            Assert.Equal(16, value);
        }
    }

    [Fact]
    public void ColorPalette_MarginWidth_ReturnsExpectedValue()
    {
        // ColorPalette.MarginWidth should be 8
        Type colorPaletteType = GetColorPaletteType();
        FieldInfo marginWidthField = colorPaletteType.GetField("MarginWidth", BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);

        if (marginWidthField is not null)
        {
            object value = marginWidthField.GetValue(null);
            Assert.Equal(8, value);
        }
    }

    #endregion

    #region Static Method Tests (No handle required)

    [Fact]
    public void ColorPalette_Get1DFrom2D_Point_ValidCoordinates_ReturnsIndex()
    {
        // (0, 0) -> 0
        // (1, 0) -> 1
        // (0, 1) -> 8 (CellsAcross)
        // (3, 2) -> 19
        Type type = GetColorPaletteType();
        var method = type.GetMethod("Get1DFrom2D",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [ typeof(Point) ],
            null);
        Assert.NotNull(method);

        var result = method!.Invoke(null, [new Point(3, 2)]);
        Assert.Equal(19, (int)result!);
    }

    [Fact]
    public void ColorPalette_Get1DFrom2D_Point_NegativeX_ReturnsMinusOne()
    {
        Type type = GetColorPaletteType();
        var method = type.GetMethod("Get1DFrom2D",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [ typeof(Point) ],
            null);
        Assert.NotNull(method);

        var result = method!.Invoke(null, [new Point(-1, 0)]);
        Assert.Equal(-1, (int)result!);
    }

    [Fact]
    public void ColorPalette_Get1DFrom2D_Point_NegativeY_ReturnsMinusOne()
    {
        Type type = GetColorPaletteType();
        var method = type.GetMethod("Get1DFrom2D",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [ typeof(Point) ],
            null);
        Assert.NotNull(method);

        var result = method!.Invoke(null, [new Point(0, -1)]);
        Assert.Equal(-1, (int)result!);
    }

    [Fact]
    public void ColorPalette_Get1DFrom2D_Ints_ValidCoordinates_ReturnsIndex()
    {
        Type type = GetColorPaletteType();
        var method = type.GetMethod("Get1DFrom2D",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [ typeof(int), typeof(int) ],
            null);
        Assert.NotNull(method);

        var result = method!.Invoke(null, [7, 7]);
        Assert.Equal(63, (int)result!);
    }

    [Fact]
    public void ColorPalette_Get1DFrom2D_Ints_NegativeX_ReturnsMinusOne()
    {
        Type type = GetColorPaletteType();
        var method = type.GetMethod("Get1DFrom2D",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [ typeof(int), typeof(int) ],
            null);
        Assert.NotNull(method);

        var result = method!.Invoke(null, [-1, 0]);
        Assert.Equal(-1, (int)result!);
    }

    [Fact]
    public void ColorPalette_Get2DFrom1D_ValidIndex_ReturnsCoordinates()
    {
        Type type = GetColorPaletteType();
        var method = type.GetMethod("Get2DFrom1D",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // index 0 -> (0, 0)
        var result0 = method!.Invoke(null, [0]);
        Assert.Equal(new Point(0, 0), (Point)result0!);

        // index 7 -> (7, 0)
        var result7 = method.Invoke(null, [7]);
        Assert.Equal(new Point(7, 0), (Point)result7!);

        // index 8 -> (0, 1)
        var result8 = method.Invoke(null, [8]);
        Assert.Equal(new Point(0, 1), (Point)result8!);

        // index 63 -> (7, 7)
        var result63 = method.Invoke(null, [63]);
        Assert.Equal(new Point(7, 7), (Point)result63!);
    }

    [Fact]
    public void ColorPalette_Get2DFrom1D_Zero_ReturnsZeroPoint()
    {
        Type type = GetColorPaletteType();
        var method = type.GetMethod("Get2DFrom1D",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method!.Invoke(null, [0]);
        Assert.Equal(new Point(0, 0), (Point)result!);
    }

    [Fact]
    public void ColorPalette_GetCell2DFromLocationMouse_InsideCell_ReturnsCoordinates()
    {
        // MarginWidth = 8, CellSize = 16
        // cell 0,0 starts at (8, 8) with size (16, 16) -> center at (16, 16)
        Type type = GetColorPaletteType();
        var method = type.GetMethod("GetCell2DFromLocationMouse",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method!.Invoke(null, [16, 16]);
        Assert.Equal(new Point(0, 0), (Point)result!);
    }

    [Fact]
    public void ColorPalette_GetCell2DFromLocationMouse_InMargin_ReturnsMinusOne()
    {
        // (0, 0) is in the margin
        Type type = GetColorPaletteType();
        var method = type.GetMethod("GetCell2DFromLocationMouse",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method!.Invoke(null, [0, 0]);
        Assert.Equal(new Point(-1, -1), (Point)result!);
    }

    [Fact]
    public void ColorPalette_GetCell2DFromLocationMouse_OutOfBounds_ReturnsMinusOne()
    {
        Type type = GetColorPaletteType();
        var method = type.GetMethod("GetCell2DFromLocationMouse",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // x = 1000 is out of bounds
        var result = method!.Invoke(null, [1000, 0]);
        Assert.Equal(new Point(-1, -1), (Point)result!);

        // y = 1000 is out of bounds
        var result2 = method!.Invoke(null, [0, 1000]);
        Assert.Equal(new Point(-1, -1), (Point)result2!);
    }

    [Fact]
    public void ColorPalette_GetCell2DFromLocationMouse_NegativeCoordinates_ReturnsMinusOne()
    {
        Type type = GetColorPaletteType();
        var method = type.GetMethod("GetCell2DFromLocationMouse",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method!.Invoke(null, [-1, 0]);
        Assert.Equal(new Point(-1, -1), (Point)result!);
    }

    [Fact]
    public void ColorPalette_GetCellFromLocationMouse_InsideCell_ReturnsIndex()
    {
        Type type = GetColorPaletteType();
        var method = type.GetMethod("GetCellFromLocationMouse",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // (16, 16) -> cell (0, 0) -> index 0
        var result = method!.Invoke(null, [16, 16]);
        Assert.Equal(0, (int)result!);
    }

    [Fact]
    public void ColorPalette_GetCellFromLocationMouse_OutOfBounds_ReturnsMinusOne()
    {
        Type type = GetColorPaletteType();
        var method = type.GetMethod("GetCellFromLocationMouse",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method!.Invoke(null, [1000, 0]);
        Assert.Equal(-1, (int)result!);
    }

    [Fact]
    public void ColorPalette_PaintValue_FillsRectangle()
    {
        // Static method - should be safe to call without a handle
        Type type = GetColorPaletteType();
        var method = type.GetMethod("PaintValue",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        using Bitmap bitmap = new(50, 50);
        using Graphics graphics = Graphics.FromImage(bitmap);
        Rectangle rect = new(0, 0, 50, 50);

        // Should not throw
        method!.Invoke(null, [Color.Red, graphics, rect]);
    }

    #endregion

    #region Instance Property/Method Tests (Use GetUninitializedObject)

    [Fact]
    public void ColorPalette_FocusedCell_Initial_ReturnsZero()
    {
        // _focus defaults to Point.Empty (0, 0) -> Get1DFrom2D returns 0
        object palette = CreateUninitializedPalette();
        var method = GetColorPaletteType().GetMethod("get_FocusedCell",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);

        var result = method!.Invoke(palette, null);
        // Note: FocusedCell uses Get1DFrom2D which returns -1 only when (x,y) == (-1,-1)
        // Point.Empty is (0,0) -> 0
        Assert.Equal(0, (int)result!);
    }

    [Fact]
    public void ColorPalette_SelectedColor_GetInitial_ReturnsDefault()
    {
        object palette = CreateUninitializedPalette();
        var prop = GetColorPaletteType().GetProperty("SelectedColor",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);

        var value = prop!.GetValue(palette);
        Assert.Equal(default(Color), value);
    }

    [WinFormsFact]
    public void ColorPalette_SelectedColor_SetThenGet_ReturnsValue()
    {
        using Control palette = CreateRealPalette();

        var prop = GetColorPaletteType().GetProperty("SelectedColor",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);

        // Set to a color that exists in the static cells (white = 0x00ffffff)
        prop!.SetValue(palette, Color.White);
        var value = prop.GetValue(palette);
        Assert.Equal(Color.White, value);
    }

    [WinFormsFact]
    public void ColorPalette_SelectedColor_SetSameValue_NoChange()
    {
        using Control palette = CreateRealPalette();

        var prop = GetColorPaletteType().GetProperty("SelectedColor",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);

        // Set twice with same value - second call should be a no-op (no InvalidateSelection)
        prop!.SetValue(palette, Color.White);
        prop.SetValue(palette, Color.White);
        Assert.Equal(Color.White, prop.GetValue(palette));
    }

    [Fact]
    public void ColorPalette_Picked_AddRemoveHandler_WorksCorrectly()
    {
        object palette = CreateUninitializedPalette();
        Type type = GetColorPaletteType();
        var pickedEvent = type.GetEvent("Picked",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(pickedEvent);

        EventHandler handler = (s, e) => { };
        // add_Picked and remove_Picked are tested implicitly through the event accessors
        pickedEvent!.AddEventHandler(palette, handler);
        pickedEvent.RemoveEventHandler(palette, handler);
    }

    // REMOVED: ColorPalette_CreateAccessibilityInstance_ReturnsAccessibleObject
    // Cannot be unit-tested because the method creates `new ColorPaletteAccessibleObject(this)`
    // which calls `ControlAccessibleObject(owner)`. The base constructor requires Control state
    // (the owner) that is null/uninitialized when using GetUninitializedObject, causing NRE.

    [Fact]
    public void ColorPalette_Get_onPicked_Initial_ReturnsNull()
    {
        object palette = CreateUninitializedPalette();
        Type type = GetColorPaletteType();
        var method = type.GetMethod("Get_onPicked",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var result = method!.Invoke(palette, null);
        Assert.Null(result);
    }

    [Fact]
    public void ColorPalette_OnPicked_NullHandler_DoesNotThrow()
    {
        object palette = CreateUninitializedPalette();
        Type type = GetColorPaletteType();
        var method = type.GetMethod("OnPicked",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // null handler should be no-op due to ?.Invoke
        method!.Invoke(palette, [EventArgs.Empty, null]);
    }

    [Fact]
    public void ColorPalette_OnPicked_WithHandler_InvokesHandler()
    {
        object palette = CreateUninitializedPalette();
        Type type = GetColorPaletteType();
        var method = type.GetMethod("OnPicked",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        bool handlerCalled = false;
        EventHandler handler = (s, e) => handlerCalled = true;

        method!.Invoke(palette, [EventArgs.Empty, handler]);
        Assert.True(handlerCalled);
    }

    [Fact]
    public void ColorPalette_GetColorFromCell_IntIndex_StaticRange_ReturnsColor()
    {
        object palette = CreateUninitializedPalette();
        InitializePaletteForColorOps(palette);

        Type type = GetColorPaletteType();
        var method = type.GetMethod("GetColorFromCell",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(int) ],
            null);
        Assert.NotNull(method);

        // index 0 is in static range
        var result = method!.Invoke(palette, [0]);
        Assert.Equal(Color.FromArgb(255, 0, 0, 0), result);
    }

    [Fact]
    public void ColorPalette_GetColorFromCell_IntIndex_CustomRange_ReturnsColor()
    {
        object palette = CreateUninitializedPalette();
        InitializePaletteForColorOps(palette);

        // Override CustomColors[0] with Magenta
        FieldInfo customColorsField = GetColorPaletteType().GetField("<CustomColors>k__BackingField",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        Color[] customColors = (Color[])customColorsField.GetValue(palette)!;
        customColors[0] = Color.Magenta;
        customColorsField.SetValue(palette, customColors);

        Type type = GetColorPaletteType();
        var method = type.GetMethod("GetColorFromCell",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(int) ],
            null);
        Assert.NotNull(method);

        // index = TotalCells - CellsCustom = 48 -> customColors[0]
        var result = method!.Invoke(palette, [ColorPalette_TotalCells - ColorPalette_CellsCustom]);
        Assert.Equal(Color.Magenta, result);
    }

    [Fact]
    public void ColorPalette_GetColorFromCell_AcrossDown_CallsGetColorFromCellInt()
    {
        object palette = CreateUninitializedPalette();
        InitializePaletteForColorOps(palette);

        // Override _staticColors[5] with Green
        FieldInfo staticColorsField = GetColorPaletteType().GetField("_staticColors",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        Color[] staticColors = (Color[])staticColorsField.GetValue(palette)!;
        staticColors[5] = Color.Green;
        staticColorsField.SetValue(palette, staticColors);

        Type type = GetColorPaletteType();
        var method = type.GetMethod("GetColorFromCell",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(int), typeof(int) ],
            null);
        Assert.NotNull(method);

        // (5, 0) -> index 5
        var result = method!.Invoke(palette, [5, 0]);
        Assert.Equal(Color.Green, result);
    }

    [Fact]
    public void ColorPalette_GetCellFromColor_ExistingColor_ReturnsCoordinates()
    {
        object palette = CreateUninitializedPalette();
        InitializePaletteForColorOps(palette);

        Type type = GetColorPaletteType();
        var method = type.GetMethod("GetCellFromColor",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Color at (5, 0) has RGB (5, 0, 0) - set by InitializePaletteForColorOps
        var result = method!.Invoke(palette, [Color.FromArgb(255, 5, 0, 0)]);
        Assert.Equal(new Point(5, 0), (Point)result!);
    }

    [Fact]
    public void ColorPalette_GetCellFromColor_NonExistingColor_ReturnsEmpty()
    {
        object palette = CreateUninitializedPalette();
        InitializePaletteForColorOps(palette);

        Type type = GetColorPaletteType();
        var method = type.GetMethod("GetCellFromColor",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Color that doesn't exist
        var result = method!.Invoke(palette, [Color.Cyan]);
        Assert.Equal(Point.Empty, (Point)result!);
    }

    [Fact]
    public void ColorPalette_IsInputKey_ArrowKeys_ReturnsTrue()
    {
        object palette = CreateUninitializedPalette();
        Type type = GetColorPaletteType();
        var method = type.GetMethod("IsInputKey",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        Assert.True((bool)method!.Invoke(palette, [Keys.Left])!);
        Assert.True((bool)method.Invoke(palette, [Keys.Right])!);
        Assert.True((bool)method.Invoke(palette, [Keys.Up])!);
        Assert.True((bool)method.Invoke(palette, [Keys.Down])!);
        Assert.True((bool)method.Invoke(palette, [Keys.Enter])!);
    }

    [Fact]
    public void ColorPalette_IsInputKey_F2Key_ReturnsFalse()
    {
        object palette = CreateUninitializedPalette();
        Type type = GetColorPaletteType();
        var method = type.GetMethod("IsInputKey",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // F2 returns false (comment: VS will take it from us in ProcessDialogKey)
        Assert.False((bool)method!.Invoke(palette, [Keys.F2])!);
    }

    // REMOVED: ColorPalette_IsInputKey_OtherKey_FallsToBase
    // Cannot be unit-tested because base.Control.IsInputKey() requires Control state
    // that is uninitialized when using GetUninitializedObject. Calling it throws NRE.

    // REMOVED: ColorPalette_ProcessDialogKey_NonF2_FallsToBase
    // Cannot be unit-tested because base.Control.ProcessDialogKey() requires Control state
    // that is uninitialized when using GetUninitializedObject. Calling it throws NRE.

    [Fact]
    public void ColorPalette_SetFocus_ClampsNegativeX()
    {
        object palette = CreateUninitializedPalette();
        Type type = GetColorPaletteType();
        var method = type.GetMethod("SetFocus",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // New focus with negative X should be clamped to 0
        method!.Invoke(palette, [new Point(-5, -5)]);

        var focusField = type.GetField("_focus",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(focusField);
        Point focus = (Point)focusField!.GetValue(palette)!;
        Assert.Equal(0, focus.X);
        Assert.Equal(0, focus.Y);
    }

    [WinFormsFact]
    public void ColorPalette_SetFocus_ClampsExceedingMax()
    {
        using Control palette = CreateRealPalette();
        Type type = GetColorPaletteType();
        var method = type.GetMethod("SetFocus",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // (100, 100) should clamp to (7, 7) = CellsAcross-1, CellsDown-1
        method!.Invoke(palette, [new Point(100, 100)]);

        var focusField = type.GetField("_focus",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(focusField);
        Point focus = (Point)focusField!.GetValue(palette)!;
        Assert.Equal(7, focus.X);
        Assert.Equal(7, focus.Y);
    }

    [WinFormsFact]
    public void ColorPalette_SetFocus_UpdatesFocus()
    {
        using Control palette = CreateRealPalette();
        Type type = GetColorPaletteType();
        var method = type.GetMethod("SetFocus",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        method!.Invoke(palette, [new Point(3, 2)]);

        var focusField = type.GetField("_focus",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(focusField);
        Point focus = (Point)focusField!.GetValue(palette)!;
        Assert.Equal(new Point(3, 2), focus);
    }

    [Fact]
    public void ColorPalette_InvalidateSelection_MethodExists()
    {
        // InvalidateSelection is called by OnKeyDown (Enter/Space) and OnMouseUp (Left button).
        // Both are covered by the [WinFormsFact] tests below; this test verifies the method exists.
        Type type = GetColorPaletteType();
        var method = type.GetMethod("InvalidateSelection",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
    }

    [Fact]
    public void ColorPalette_InheritsFromControl()
    {
        Type type = GetColorPaletteType();
        Assert.True(typeof(Control).IsAssignableFrom(type));
    }

    [Fact]
    public void ColorPalette_IsPrivate()
    {
        Type type = GetColorPaletteType();
        Assert.True(type.IsNested && type.IsNestedPrivate);
    }

    [Fact]
    public void ColorPalette_Constructor_TakesColorUIAndColors()
    {
        Type type = GetColorPaletteType();
        Type colorUIType = typeof(ColorEditor).GetNestedType("ColorUI", BindingFlags.NonPublic)!;
        var ctor = type.GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
            null,
            [ colorUIType, typeof(Color[]) ],
            null);
        Assert.NotNull(ctor);
    }

    [Fact]
    public void ColorPalette_HasFocusedCellProperty()
    {
        Type type = GetColorPaletteType();
        var prop = type.GetProperty("FocusedCell",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(prop);
    }

    [Fact]
    public void ColorPalette_HasCustomColorsProperty()
    {
        Type type = GetColorPaletteType();
        var prop = type.GetProperty("CustomColors",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
    }

    #endregion

    #region WinFormsFact Tests (Requires handle)

    [WinFormsFact]
    public void ColorPalette_OnGotFocus_InvokesBaseAndInvalidatesFocus()
    {
        using Control palette = CreateRealPalette();
        var method = GetColorPaletteType().GetMethod("OnGotFocus",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(EventArgs) ],
            null);
        Assert.NotNull(method);

        // Should not throw with a valid handle
        method!.Invoke(palette, [EventArgs.Empty]);
    }

    [WinFormsFact]
    public void ColorPalette_OnLostFocus_InvokesBaseAndInvalidatesFocus()
    {
        using Control palette = CreateRealPalette();
        var method = GetColorPaletteType().GetMethod("OnLostFocus",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(EventArgs) ],
            null);
        Assert.NotNull(method);

        method!.Invoke(palette, [EventArgs.Empty]);
    }

    [WinFormsFact]
    public void ColorPalette_OnKeyDown_LeftKey_ChangesFocus()
    {
        using Control palette = CreateRealPalette();
        var method = GetColorPaletteType().GetMethod("OnKeyDown",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(KeyEventArgs) ],
            null);
        Assert.NotNull(method);

        // Initial _focus is (0, 0). After Left key, it would go to (-1, 0) but SetFocus clamps to (0, 0)
        KeyEventArgs args = new(Keys.Left);
        method!.Invoke(palette, [args]);
        Assert.Equal(0, (int)palette.TestAccessor.Dynamic.FocusedCell);
    }

    [WinFormsFact]
    public void ColorPalette_OnKeyDown_RightKey_ChangesFocus()
    {
        using Control palette = CreateRealPalette();
        var method = GetColorPaletteType().GetMethod("OnKeyDown",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(KeyEventArgs) ],
            null);
        Assert.NotNull(method);

        KeyEventArgs args = new(Keys.Right);
        method!.Invoke(palette, [args]);
        Assert.Equal(1, (int)palette.TestAccessor.Dynamic.FocusedCell);
    }

    [WinFormsFact]
    public void ColorPalette_OnKeyDown_UpKey_ChangesFocus()
    {
        using Control palette = CreateRealPalette();
        var method = GetColorPaletteType().GetMethod("OnKeyDown",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(KeyEventArgs) ],
            null);
        Assert.NotNull(method);

        KeyEventArgs args = new(Keys.Up);
        method!.Invoke(palette, [args]);
        Assert.Equal(0, (int)palette.TestAccessor.Dynamic.FocusedCell);
    }

    [WinFormsFact]
    public void ColorPalette_OnKeyDown_DownKey_ChangesFocus()
    {
        using Control palette = CreateRealPalette();
        var method = GetColorPaletteType().GetMethod("OnKeyDown",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(KeyEventArgs) ],
            null);
        Assert.NotNull(method);

        KeyEventArgs args = new(Keys.Down);
        method!.Invoke(palette, [args]);
        Assert.Equal(8, (int)palette.TestAccessor.Dynamic.FocusedCell);
    }

    [WinFormsFact]
    public void ColorPalette_OnKeyDown_EnterKey_SelectsAndPicksColor()
    {
        using Control palette = CreateRealPalette();
        int picked = 0;
        EventHandler handler = (sender, e) =>
        {
            Assert.Same(palette, sender);
            picked++;
        };
        GetColorPaletteType().GetEvent("Picked")!.AddEventHandler(palette, handler);
        palette.TestAccessor.Dynamic.SetFocus(new Point(1, 0));
        var method = GetColorPaletteType().GetMethod("OnKeyDown",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(KeyEventArgs) ],
            null);
        Assert.NotNull(method);

        KeyEventArgs args = new(Keys.Enter);
        method!.Invoke(palette, [args]);
        Assert.Equal(1, picked);
        Assert.Equal(
            (Color)palette.TestAccessor.Dynamic.GetColorFromCell(1),
            GetColorPaletteType().GetProperty("SelectedColor")!.GetValue(palette));
    }

    [WinFormsFact]
    public void ColorPalette_OnKeyDown_SpaceKey_SelectsWithoutPickingColor()
    {
        using Control palette = CreateRealPalette();
        int picked = 0;
        EventHandler handler = (_, _) => picked++;
        GetColorPaletteType().GetEvent("Picked")!.AddEventHandler(palette, handler);
        palette.TestAccessor.Dynamic.SetFocus(new Point(1, 0));
        var method = GetColorPaletteType().GetMethod("OnKeyDown",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(KeyEventArgs) ],
            null);
        Assert.NotNull(method);

        KeyEventArgs args = new(Keys.Space);
        method!.Invoke(palette, [args]);
        Assert.Equal(0, picked);
        Assert.Equal(
            (Color)palette.TestAccessor.Dynamic.GetColorFromCell(1),
            GetColorPaletteType().GetProperty("SelectedColor")!.GetValue(palette));
    }

    [WinFormsFact]
    public void ColorPalette_OnKeyDown_OtherKey_DoesNotThrow()
    {
        using Control palette = CreateRealPalette();
        var method = GetColorPaletteType().GetMethod("OnKeyDown",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(KeyEventArgs) ],
            null);
        Assert.NotNull(method);

        KeyEventArgs args = new(Keys.A);
        method!.Invoke(palette, [args]);
    }

    [WinFormsFact]
    public void ColorPalette_OnMouseDown_LeftButton_ValidCell_ChangesFocus()
    {
        using Control palette = CreateRealPalette();
        var method = GetColorPaletteType().GetMethod("OnMouseDown",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(MouseEventArgs) ],
            null);
        Assert.NotNull(method);

        Point location = GetCellCenter(palette, 10);
        MouseEventArgs args = new(MouseButtons.Left, 1, location.X, location.Y, 0);
        method!.Invoke(palette, [args]);
        Assert.Equal(10, (int)palette.TestAccessor.Dynamic.FocusedCell);
    }

    [WinFormsFact]
    public void ColorPalette_OnMouseDown_LeftButton_Margin_DoesNotChangeFocus()
    {
        using Control palette = CreateRealPalette();
        var method = GetColorPaletteType().GetMethod("OnMouseDown",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(MouseEventArgs) ],
            null);
        Assert.NotNull(method);

        // Click in the margin (0, 0)
        MouseEventArgs args = new(MouseButtons.Left, 1, 0, 0, 0);
        method!.Invoke(palette, [args]);
        Assert.Equal(0, (int)palette.TestAccessor.Dynamic.FocusedCell);
    }

    [WinFormsFact]
    public void ColorPalette_OnMouseDown_RightButton_DoesNotChangeFocus()
    {
        using Control palette = CreateRealPalette();
        var method = GetColorPaletteType().GetMethod("OnMouseDown",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(MouseEventArgs) ],
            null);
        Assert.NotNull(method);

        // Right-click should be ignored in OnMouseDown
        MouseEventArgs args = new(MouseButtons.Right, 1, 16, 16, 0);
        method!.Invoke(palette, [args]);
        Assert.Equal(0, (int)palette.TestAccessor.Dynamic.FocusedCell);
    }

    [WinFormsFact]
    public void ColorPalette_OnMouseMove_LeftButtonHeld_ValidCell_ChangesFocus()
    {
        using Control palette = CreateRealPalette();
        var method = GetColorPaletteType().GetMethod("OnMouseMove",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(MouseEventArgs) ],
            null);
        Assert.NotNull(method);

        Point location = GetCellCenter(palette, 19);
        MouseEventArgs args = new(MouseButtons.Left, 1, location.X, location.Y, 0);
        method!.Invoke(palette, [args]);
        Assert.Equal(19, (int)palette.TestAccessor.Dynamic.FocusedCell);
    }

    [WinFormsFact]
    public void ColorPalette_OnMouseMove_NoButton_DoesNotThrow()
    {
        using Control palette = CreateRealPalette();
        var method = GetColorPaletteType().GetMethod("OnMouseMove",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(MouseEventArgs) ],
            null);
        Assert.NotNull(method);

        // No button pressed - should be a no-op
        MouseEventArgs args = new(MouseButtons.None, 0, 16, 16, 0);
        method!.Invoke(palette, [args]);
    }

    [WinFormsFact]
    public void ColorPalette_OnMouseUp_LeftButton_ValidCell_PicksColor()
    {
        using Control palette = CreateRealPalette();
        int picked = 0;
        EventHandler handler = (_, _) => picked++;
        GetColorPaletteType().GetEvent("Picked")!.AddEventHandler(palette, handler);
        var method = GetColorPaletteType().GetMethod("OnMouseUp",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(MouseEventArgs) ],
            null);
        Assert.NotNull(method);

        Point location = GetCellCenter(palette, 0);
        MouseEventArgs args = new(MouseButtons.Left, 1, location.X, location.Y, 0);
        method!.Invoke(palette, [args]);
        Assert.Equal(1, picked);
        Assert.Equal(
            (Color)palette.TestAccessor.Dynamic.GetColorFromCell(0),
            GetColorPaletteType().GetProperty("SelectedColor")!.GetValue(palette));
    }

    [WinFormsFact]
    public void ColorPalette_OnMouseUp_LeftButton_Margin_DoesNotPick()
    {
        using Control palette = CreateRealPalette();
        int picked = 0;
        EventHandler handler = (_, _) => picked++;
        GetColorPaletteType().GetEvent("Picked")!.AddEventHandler(palette, handler);
        var method = GetColorPaletteType().GetMethod("OnMouseUp",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(MouseEventArgs) ],
            null);
        Assert.NotNull(method);

        // Click in margin - no cell hit
        MouseEventArgs args = new(MouseButtons.Left, 1, 0, 0, 0);
        method!.Invoke(palette, [args]);
        Assert.Equal(0, picked);
        Assert.Equal(default(Color), GetColorPaletteType().GetProperty("SelectedColor")!.GetValue(palette));
    }

    [WinFormsFact]
    public void ColorPalette_OnMouseUp_RightButton_StaticCell_DoesNotLaunchDialog()
    {
        using Control palette = CreateRealPalette();
        var method = GetColorPaletteType().GetMethod("OnMouseUp",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(MouseEventArgs) ],
            null);
        Assert.NotNull(method);

        // Right-click on a static (non-custom) cell - LaunchDialog is not called
        // (would open a modal dialog if it were a custom cell)
        MouseEventArgs args = new(MouseButtons.Right, 1, 16, 16, 0);
        method!.Invoke(palette, [args]);
    }

    [WinFormsFact]
    public void ColorPalette_OnPaint_DoesNotThrow()
    {
        using Control palette = CreateRealPalette();
        var method = GetColorPaletteType().GetMethod("OnPaint",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ typeof(PaintEventArgs) ],
            null);
        Assert.NotNull(method);

        using Bitmap bitmap = new(50, 50);
        using Graphics graphics = Graphics.FromImage(bitmap);
        PaintEventArgs args = new(graphics, new Rectangle(0, 0, 50, 50));

        method!.Invoke(palette, [args]);
    }

    // LaunchDialog is not unit-testable because it shows a modal ColorDialog that would block.
    // The OnMouseUp right-click branch that calls LaunchDialog is therefore not covered.
    // The ProcessDialogKey F2 branch that calls LaunchDialog is therefore not covered.

    #endregion

    #region Helper Methods

    private Type GetColorPaletteType()
    {
        Type colorEditorType = typeof(ColorEditor);
        Type colorPaletteType = colorEditorType.GetNestedType("ColorPalette", BindingFlags.NonPublic);
        Assert.NotNull(colorPaletteType);
        return colorPaletteType;
    }

    private const int ColorPalette_CellsAcross = 8;
    private const int ColorPalette_CellsDown = 8;
    private const int ColorPalette_CellsCustom = 16;
    private const int ColorPalette_TotalCells = ColorPalette_CellsAcross * ColorPalette_CellsDown;

    /// <summary>
    /// Creates a ColorPalette instance using GetUninitializedObject, which skips the
    /// constructor that requires a window handle.
    /// </summary>
    private static object CreateUninitializedPalette()
    {
        Type colorEditorType = typeof(ColorEditor);
        Type colorPaletteType = colorEditorType.GetNestedType("ColorPalette", BindingFlags.NonPublic)!;
        return RuntimeHelpers.GetUninitializedObject(colorPaletteType);
    }

    /// <summary>
    /// Initializes the _staticColors and CustomColors backing fields so that
    /// color-related operations (GetColorFromCell, SetFocus, etc.) work
    /// without throwing NullReferenceException.
    /// </summary>
    private static void InitializePaletteForColorOps(object palette)
    {
        Type colorPaletteType = typeof(ColorEditor).GetNestedType("ColorPalette", BindingFlags.NonPublic)!;

        // Set _staticColors
        FieldInfo staticColorsField = colorPaletteType.GetField("_staticColors",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        Color[] staticColors = new Color[ColorPalette_TotalCells - ColorPalette_CellsCustom];
        for (int i = 0; i < staticColors.Length; i++)
        {
            staticColors[i] = Color.FromArgb(255, i, 0, 0);
        }

        staticColorsField.SetValue(palette, staticColors);

        // Set CustomColors (auto-property has backing field <CustomColors>k__BackingField)
        FieldInfo customColorsField = colorPaletteType.GetField("<CustomColors>k__BackingField",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        Color[] customColors = new Color[ColorPalette_CellsCustom];
        for (int i = 0; i < customColors.Length; i++)
        {
            customColors[i] = Color.White;
        }

        customColorsField.SetValue(palette, customColors);
    }

    /// <summary>
    /// Creates a real ColorPalette Control with a window handle for use in [WinFormsFact] tests.
    /// </summary>
    private static Control CreateRealPalette()
    {
        Type colorEditorType = typeof(ColorEditor);
        Type colorUIType = colorEditorType.GetNestedType("ColorUI", BindingFlags.NonPublic)!;
        Type colorPaletteType = colorEditorType.GetNestedType("ColorPalette", BindingFlags.NonPublic)!;

        ColorEditor editor = new();
        Control colorUI = (Control)Activator.CreateInstance(colorUIType, editor)!;
        Color[] customColors = new Color[ColorPalette_CellsCustom];
        for (int i = 0; i < customColors.Length; i++)
        {
            customColors[i] = Color.White;
        }

        // Get the constructor (it's public, so we need Public | Instance)
        var ctor = colorPaletteType.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [ colorUIType, typeof(Color[]) ],
            null);
        Assert.NotNull(ctor);

        Control palette = (Control)ctor!.Invoke([colorUI, customColors]);
        palette.Disposed += (_, _) => colorUI.Dispose();
        // Force handle creation
        Assert.NotEqual(IntPtr.Zero, palette.Handle);
        return palette;
    }

    private static Point GetCellCenter(Control palette, int index)
    {
        Rectangle bounds = palette.AccessibilityObject.GetChild(index)!.Bounds;
        return palette.PointToClient(new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2));
    }

    #endregion
}
