// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Drawing.Design.Tests;

/// <summary>
/// Tests for ColorEditor.ColorPalette.ColorPaletteAccessibleObject nested class
/// Uses reflection to access internal components
/// </summary>
public class ColorEditorAccessibleObjectTests
{
    #region ColorPaletteAccessibleObject Tests

    [Fact]
    public void ColorPaletteAccessibleObject_CanBeCreated()
    {
        Type type = GetColorPaletteAccessibleObjectType();
        Assert.NotNull(type);
    }

    [Fact]
    public void ColorPaletteAccessibleObject_InheritsFromControlAccessibleObject()
    {
        Type type = GetColorPaletteAccessibleObjectType();
        Assert.True(typeof(Control.ControlAccessibleObject).IsAssignableFrom(type));
    }

    [Fact]
    public void ColorPaletteAccessibleObject_HasGetChildCountMethod()
    {
        Type type = GetColorPaletteAccessibleObjectType();
        var method = type.GetMethod("GetChildCount", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);
    }

    [Fact]
    public void ColorPaletteAccessibleObject_HasGetChildMethod()
    {
        Type type = GetColorPaletteAccessibleObjectType();
        var method = type.GetMethod("GetChild", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);
    }

    [Fact]
    public void ColorPaletteAccessibleObject_HasHitTestMethod()
    {
        Type type = GetColorPaletteAccessibleObjectType();
        var method = type.GetMethod("HitTest", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);
    }

    [Fact]
    public void ColorPaletteAccessibleObject_HasColorPaletteProperty()
    {
        Type type = GetColorPaletteAccessibleObjectType();
        var property = type.GetProperty("ColorPalette", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(property);
    }

    [Fact]
    public void ColorPaletteAccessibleObject_Constructor_TakesColorPalette()
    {
        Type type = GetColorPaletteAccessibleObjectType();
        var ctor = type.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [GetColorPaletteType()],
            null);
        Assert.NotNull(ctor);
    }

    [Fact]
    public void ColorPaletteAccessibleObject_GetChildCount_ReturnsExpectedValue()
    {
        // Create an instance of the AccessibleObject
        object instance = CreateColorPaletteAccessibleObjectInstance();
        if (instance is not null)
        {
            int childCount = ((Control.ControlAccessibleObject)instance).GetChildCount();
            // CellsAcross * CellsDown = 8 * 8 = 64
            Assert.Equal(64, childCount);
        }
    }

    [WinFormsFact]
    public void ColorPaletteAccessibleObject_RealOwner_CachesChildrenAndHitTestsCells()
    {
        Type colorUIType = typeof(ColorEditor).GetNestedType("ColorUI", BindingFlags.NonPublic)!;
        using Control colorUI = (Control)Activator.CreateInstance(colorUIType, new ColorEditor())!;
        Control palette = colorUI.TestAccessor.Dynamic._pal;
        Assert.NotEqual(IntPtr.Zero, palette.Handle);
        AccessibleObject accessible = palette.AccessibilityObject;
        Assert.Equal(64, accessible.GetChildCount());
        Assert.Null(accessible.GetChild(-1));
        Assert.Null(accessible.GetChild(64));
        AccessibleObject first = accessible.GetChild(0);
        Assert.NotNull(first);
        Assert.Same(first, accessible.GetChild(0));
        Assert.Same(accessible, first.Parent);
        Assert.Equal(AccessibleRole.Cell, first.Role);
        Rectangle bounds = first.Bounds;
        Assert.False(bounds.IsEmpty);
        Assert.Same(first, accessible.HitTest(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2));
        AccessibleObject second = accessible.GetChild(1);
        Assert.NotSame(first, second);
        Rectangle secondBounds = second.Bounds;
        Assert.Same(second, accessible.HitTest(secondBounds.Left + secondBounds.Width / 2, secondBounds.Top + secondBounds.Height / 2));
    }

    #endregion

    #region ColorCellAccessibleObject Tests

    [Fact]
    public void ColorCellAccessibleObject_CanBeCreated()
    {
        Type type = GetColorCellAccessibleObjectType();
        Assert.NotNull(type);
    }

    [Fact]
    public void ColorCellAccessibleObject_InheritsFromAccessibleObject()
    {
        Type type = GetColorCellAccessibleObjectType();
        Assert.True(typeof(AccessibleObject).IsAssignableFrom(type));
    }

    [Fact]
    public void ColorCellAccessibleObject_HasBoundsProperty()
    {
        Type type = GetColorCellAccessibleObjectType();
        var property = type.GetProperty("Bounds", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
    }

    [Fact]
    public void ColorCellAccessibleObject_HasNameProperty()
    {
        Type type = GetColorCellAccessibleObjectType();
        var property = type.GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
    }

    [Fact]
    public void ColorCellAccessibleObject_HasParentProperty()
    {
        Type type = GetColorCellAccessibleObjectType();
        var property = type.GetProperty("Parent", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
    }

    [Fact]
    public void ColorCellAccessibleObject_HasRoleProperty()
    {
        Type type = GetColorCellAccessibleObjectType();
        var property = type.GetProperty("Role", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
    }

    [Fact]
    public void ColorCellAccessibleObject_HasStateProperty()
    {
        Type type = GetColorCellAccessibleObjectType();
        var property = type.GetProperty("State", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
    }

    [Fact]
    public void ColorCellAccessibleObject_HasValueProperty()
    {
        Type type = GetColorCellAccessibleObjectType();
        var property = type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
    }

    [Fact]
    public void ColorCellAccessibleObject_Constructor_TakesExpectedParameters()
    {
        Type type = GetColorCellAccessibleObjectType();
        Type parentType = GetColorPaletteAccessibleObjectType();
        var ctor = type.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [parentType, typeof(Color), typeof(int)],
            null);
        Assert.NotNull(ctor);
    }

    [Fact]
    public void ColorCellAccessibleObject_Role_ReturnsCell()
    {
        object instance = CreateColorCellAccessibleObjectInstance();
        if (instance is not null)
        {
            var roleProperty = instance.GetType().GetProperty("Role", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(roleProperty);
            if (roleProperty is not null)
            {
                object role = roleProperty.GetValue(instance);
                Assert.Equal(AccessibleRole.Cell, role);
            }
        }
    }

    [Fact]
    public void ColorCellAccessibleObject_Parent_ReturnsParentAccessibleObject()
    {
        object parentInstance = CreateColorPaletteAccessibleObjectInstance();
        object cellInstance = CreateColorCellAccessibleObjectInstance(parentInstance);
        if (cellInstance is not null && parentInstance is not null)
        {
            var parentProperty = cellInstance.GetType().GetProperty("Parent", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(parentProperty);
            if (parentProperty is not null)
            {
                object parent = parentProperty.GetValue(cellInstance);
                Assert.Same(parentInstance, parent);
            }
        }
    }

    [Fact]
    public void ColorCellAccessibleObject_Name_ReturnsColorString()
    {
        object cellInstance = CreateColorCellAccessibleObjectInstance();
        if (cellInstance is not null)
        {
            var nameProperty = cellInstance.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(nameProperty);
            if (nameProperty is not null)
            {
                object name = nameProperty.GetValue(cellInstance);
                Assert.NotNull(name);
                Assert.IsType<string>(name);
                Assert.NotEmpty((string)name);
            }
        }
    }

    [Fact]
    public void ColorCellAccessibleObject_Value_ReturnsColorString()
    {
        object cellInstance = CreateColorCellAccessibleObjectInstance();
        if (cellInstance is not null)
        {
            var valueProperty = cellInstance.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(valueProperty);
            if (valueProperty is not null)
            {
                object value = valueProperty.GetValue(cellInstance);
                Assert.NotNull(value);
                Assert.IsType<string>(value);
                Assert.NotEmpty((string)value);
            }
        }
    }

    #endregion

    #region Helper Methods

    private static Type GetColorPaletteType()
    {
        Type colorEditorType = typeof(ColorEditor);
        Type colorPaletteType = colorEditorType.GetNestedType("ColorPalette", BindingFlags.NonPublic);
        Assert.NotNull(colorPaletteType);
        return colorPaletteType!;
    }

    private static Type GetColorPaletteAccessibleObjectType()
    {
        Type colorPaletteType = GetColorPaletteType();
        Type accessibleObjectType = colorPaletteType.GetNestedType("ColorPaletteAccessibleObject", BindingFlags.Public);
        Assert.NotNull(accessibleObjectType);
        return accessibleObjectType!;
    }

    private static Type GetColorCellAccessibleObjectType()
    {
        Type accessibleObjectType = GetColorPaletteAccessibleObjectType();
        Type cellType = accessibleObjectType.GetNestedType("ColorCellAccessibleObject", BindingFlags.Public);
        Assert.NotNull(cellType);
        return cellType!;
    }

    private static object CreateColorPaletteAccessibleObjectInstance()
    {
        // Use RuntimeHelpers.GetUninitializedObject to bypass constructor that requires a ColorPalette
        Type accessibleObjectType = GetColorPaletteAccessibleObjectType();
        return RuntimeHelpers.GetUninitializedObject(accessibleObjectType);
    }

    private static object CreateColorCellAccessibleObjectInstance(object parentInstance = null)
    {
        Type cellType = GetColorCellAccessibleObjectType();
        Type accessibleObjectType = GetColorPaletteAccessibleObjectType();

        // Try to find a parent
        object parent = parentInstance;
        parent ??= CreateColorPaletteAccessibleObjectInstance();

        Assert.NotNull(parent);

        var ctor = cellType.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [accessibleObjectType, typeof(Color), typeof(int)],
            null);

        Assert.NotNull(ctor);
        return ctor!.Invoke([parent!, Color.Red, 0]);
    }

    #endregion
}
