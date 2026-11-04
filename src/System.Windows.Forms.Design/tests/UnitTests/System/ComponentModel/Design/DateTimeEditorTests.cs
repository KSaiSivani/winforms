// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Drawing.Design;
using System.Reflection;
using System.Windows.Forms.Design;
using System.Windows.Forms.TestUtilities;
using Moq;

namespace System.ComponentModel.Design.Tests;

/// <summary>
/// Tests for <see cref="DateTimeEditor"/> and its private nested types
/// (<c>DateTimeEditor.DateTimeUI</c> and <c>DateTimeEditor.DateTimeUI.DateTimeMonthCalendar</c>).
/// </summary>
public class DateTimeEditorTests
{
    #region DateTimeEditor Public API Tests

    [Fact]
    public void DateTimeEditor_Ctor_Default_CreatesInstance()
    {
        DateTimeEditor editor = new();
        Assert.NotNull(editor);
    }

    [Fact]
    public void DateTimeEditor_IsDropDownResizable_ReturnsFalse()
    {
        DateTimeEditor editor = new();
        Assert.False(editor.IsDropDownResizable);
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetITypeDescriptorContextTestData))]
    public void DateTimeEditor_GetEditStyle_Invoke_ReturnsDropDown(ITypeDescriptorContext context)
    {
        DateTimeEditor editor = new();
        Assert.Equal(UITypeEditorEditStyle.DropDown, editor.GetEditStyle(context));
    }

    public static IEnumerable<object[]> EditValue_ValidValue_TestData()
    {
        yield return new object[] { null };
        yield return new object[] { DateTime.Today };
        yield return new object[] { new DateTime(2024, 1, 15) };
        yield return new object[] { DateTime.MinValue };
        yield return new object[] { DateTime.MaxValue };
    }

    [Theory]
    [MemberData(nameof(EditValue_ValidValue_TestData))]
    public void DateTimeEditor_EditValue_ValidProvider_ReturnsValue(object value)
    {
        DateTimeEditor editor = new();
        Mock<IWindowsFormsEditorService> mockEditorService = new(MockBehavior.Strict);
        Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
        mockServiceProvider
            .Setup(p => p.GetService(typeof(IWindowsFormsEditorService)))
            .Returns(mockEditorService.Object)
            .Verifiable();
        mockEditorService
            .Setup(e => e.DropDownControl(It.IsAny<Control>()))
            .Verifiable();
        // The editor's DateTimeUI is wrapped in a `using`, so it is disposed before
        // the value is read back. We use Assert.Equal (value semantics) instead of
        // Assert.Same because for value-type inputs the boxed return value is not
        // reference-equal to the input.
        object result = editor.EditValue(null, mockServiceProvider.Object, value);
        Assert.Equal(value, result);
        mockServiceProvider.Verify(p => p.GetService(typeof(IWindowsFormsEditorService)), Times.Once());
        mockEditorService.Verify(e => e.DropDownControl(It.IsAny<Control>()), Times.Once());
    }

    [Theory]
    [CommonMemberData(typeof(CommonTestHelperEx), nameof(CommonTestHelperEx.GetEditValueInvalidProviderTestData))]
    public void DateTimeEditor_EditValue_InvalidProvider_ReturnsValue(IServiceProvider provider, object value)
    {
        DateTimeEditor editor = new();
        Assert.Same(value, editor.EditValue(null, provider, value));
    }

    [Fact]
    public void DateTimeEditor_EditValue_NullProvider_ReturnsValue()
    {
        DateTimeEditor editor = new();
        DateTime value = new(2024, 6, 15);
        Assert.Equal(value, editor.EditValue(null, null, value));
    }

    #endregion

    #region DateTimeEditor.DateTimeUI Type Tests

    [Fact]
    public void DateTimeUI_CanBeResolved()
    {
        Type type = GetDateTimeUIType();
        Assert.NotNull(type);
    }

    [Fact]
    public void DateTimeUI_IsPrivate()
    {
        Type type = GetDateTimeUIType();
        Assert.True(type.IsNested && type.IsNestedPrivate);
    }

    [Fact]
    public void DateTimeUI_InheritsFromControl()
    {
        Type type = GetDateTimeUIType();
        Assert.True(typeof(Control).IsAssignableFrom(type));
    }

    [Fact]
    public void DateTimeUI_Constructor_TakesEditorServiceAndValue()
    {
        Type type = GetDateTimeUIType();
        var ctor = type.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(IWindowsFormsEditorService), typeof(object)],
            null);
        Assert.NotNull(ctor);
    }

    [Fact]
    public void DateTimeUI_HasValueProperty()
    {
        Type type = GetDateTimeUIType();
        var prop = type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        // Setter should be private (not public)
        Assert.Null(prop!.GetSetMethod());
    }

    [Fact]
    public void DateTimeUI_HasDisposeMethod()
    {
        Type type = GetDateTimeUIType();
        var method = type.GetMethod("Dispose",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(bool)],
            null);
        Assert.NotNull(method);
    }

    [Fact]
    public void DateTimeUI_HasOnGotFocusOverride()
    {
        Type type = GetDateTimeUIType();
        var method = type.GetMethod("OnGotFocus",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(EventArgs)],
            null);
        Assert.NotNull(method);
    }

    [Fact]
    public void DateTimeUI_HasRescaleConstantsForDpiOverride()
    {
        Type type = GetDateTimeUIType();
        var method = type.GetMethod("RescaleConstantsForDpi",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(int), typeof(int)],
            null);
        Assert.NotNull(method);
    }

    [Fact]
    public void DateTimeUI_HasMonthCalendarField()
    {
        Type type = GetDateTimeUIType();
        var field = type.GetField("_monthCalendar", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        Assert.Equal(typeof(MonthCalendar), field!.FieldType);
    }

    [Fact]
    public void DateTimeUI_HasEditorServiceField()
    {
        Type type = GetDateTimeUIType();
        var field = type.GetField("_editorService", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        Assert.Equal(typeof(IWindowsFormsEditorService), field!.FieldType);
    }

    [Fact]
    public void DateTimeUI_HasInitializeComponentMethod()
    {
        Type type = GetDateTimeUIType();
        var method = type.GetMethod("InitializeComponent", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
    }

    [Fact]
    public void DateTimeUI_HasMonthCalResizeMethod()
    {
        Type type = GetDateTimeUIType();
        var method = type.GetMethod("MonthCalResize", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
    }

    [Fact]
    public void DateTimeUI_HasOnDateSelectedMethod()
    {
        Type type = GetDateTimeUIType();
        var method = type.GetMethod("OnDateSelected", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
    }

    [Fact]
    public void DateTimeUI_HasMonthCalKeyDownMethod()
    {
        Type type = GetDateTimeUIType();
        var method = type.GetMethod("MonthCalKeyDown", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
    }

    #endregion

    #region DateTimeEditor.DateTimeUI DateTimeMonthCalendar Type Tests

    [Fact]
    public void DateTimeMonthCalendar_CanBeResolved()
    {
        Type type = GetDateTimeMonthCalendarType();
        Assert.NotNull(type);
    }

    [Fact]
    public void DateTimeMonthCalendar_IsNestedPrivate()
    {
        Type type = GetDateTimeMonthCalendarType();
        Assert.True(type.IsNested && type.IsNestedPrivate);
    }

    [Fact]
    public void DateTimeMonthCalendar_InheritsFromMonthCalendar()
    {
        Type type = GetDateTimeMonthCalendarType();
        Assert.True(typeof(MonthCalendar).IsAssignableFrom(type));
    }

    [Fact]
    public void DateTimeMonthCalendar_HasIsInputKeyOverride()
    {
        Type type = GetDateTimeMonthCalendarType();
        var method = type.GetMethod("IsInputKey",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(Keys)],
            null);
        Assert.NotNull(method);
    }

    [Fact]
    public void DateTimeMonthCalendar_IsInputKey_Enter_ReturnsTrue()
    {
        // Use a real instance (Activator.CreateInstance) so the Control constructor
        // runs and initializes the private _window field. GetUninitializedObject
        // would leave _window null and cause NRE when the base Control.IsInputKey
        // is consulted.
        using MonthCalendar calendar = CreateDateTimeMonthCalendar();
        Type type = GetDateTimeMonthCalendarType();
        var method = type.GetMethod("IsInputKey",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(Keys)],
            null);
        Assert.NotNull(method);

        bool result = (bool)method!.Invoke(calendar, [Keys.Enter])!;
        Assert.True(result);
    }

    [Theory]
    [InlineData(Keys.A)]
    [InlineData(Keys.Left)]
    [InlineData(Keys.Right)]
    [InlineData(Keys.F2)]
    [InlineData(Keys.Space)]
    [InlineData(Keys.Escape)]
    public void DateTimeMonthCalendar_IsInputKey_NonEnter_DoesNotThrow(Keys key)
    {
        using MonthCalendar calendar = CreateDateTimeMonthCalendar();
        Type type = GetDateTimeMonthCalendarType();
        var method = type.GetMethod("IsInputKey",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(Keys)],
            null);
        Assert.NotNull(method);

        // For non-Enter keys the override falls through to the base MonthCalendar /
        // Control implementation. The control has no handle, so the base IsInputKey
        // short-circuits to false without throwing. We only assert the call is safe.
        bool _ = (bool)method!.Invoke(calendar, [key])!;
    }

    #endregion

    #region DateTimeEditor.DateTimeUI Behavioral Tests (Real Instance)

    [Fact]
    public void DateTimeUI_Construct_WithNullValue_Succeeds()
    {
        Type type = GetDateTimeUIType();
        Mock<IWindowsFormsEditorService> mockService = new(MockBehavior.Loose);
        object instance = Activator.CreateInstance(type, mockService.Object, (object)null)!;
        Assert.NotNull(instance);

        // Verify Value was assigned
        PropertyInfo valueProperty = type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)!;
        Assert.Null(valueProperty.GetValue(instance));

        // Dispose the instance to release resources
        ((IDisposable)instance).Dispose();
    }

    [Fact]
    public void DateTimeUI_Construct_WithDateTimeValue_Succeeds()
    {
        Type type = GetDateTimeUIType();
        Mock<IWindowsFormsEditorService> mockService = new(MockBehavior.Loose);
        DateTime input = new(2024, 6, 15);
        object instance = Activator.CreateInstance(type, mockService.Object, (object)input)!;
        Assert.NotNull(instance);

        PropertyInfo valueProperty = type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)!;
        Assert.Equal(input, valueProperty.GetValue(instance));

        ((IDisposable)instance).Dispose();
    }

    [Fact]
    public void DateTimeUI_OnGotFocus_DoesNotThrow()
    {
        Type type = GetDateTimeUIType();
        Mock<IWindowsFormsEditorService> mockService = new(MockBehavior.Loose);
        object instance = Activator.CreateInstance(type, mockService.Object, (object)null);

        try
        {
            MethodInfo method = type.GetMethod("OnGotFocus",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                [typeof(EventArgs)],
                null)!;
            // base.OnGotFocus uses _parent / Events which are initialized by the
            // Control ctor. _monthCalendar.Focus() is a no-op when the control has
            // no handle, so the override should not throw on a real instance.
            method.Invoke(instance, [EventArgs.Empty]);
        }
        finally
        {
            ((IDisposable)instance).Dispose();
        }
    }

    [Fact]
    public void DateTimeUI_RescaleConstantsForDpi_DoesNotThrow()
    {
        Type type = GetDateTimeUIType();
        Mock<IWindowsFormsEditorService> mockService = new(MockBehavior.Loose);
        object instance = Activator.CreateInstance(type, mockService.Object, (object)null);

        try
        {
            MethodInfo method = type.GetMethod("RescaleConstantsForDpi",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                [typeof(int), typeof(int)],
                null)!;
            // The base Control implementation is empty; the override then sets
            // Size from _monthCalendar.SingleMonthSize (returns default with no
            // handle). Both steps are safe.
            method.Invoke(instance, [96, 120]);
        }
        finally
        {
            ((IDisposable)instance).Dispose();
        }
    }

    [Fact]
    public void DateTimeUI_MonthCalResize_DoesNotThrow()
    {
        Type type = GetDateTimeUIType();
        Mock<IWindowsFormsEditorService> mockService = new(MockBehavior.Loose);
        object instance = Activator.CreateInstance(type, mockService.Object, (object)null);

        try
        {
            MethodInfo method = type.GetMethod("MonthCalResize",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                [typeof(object), typeof(EventArgs)],
                null)!;
            // Sets Size = _monthCalendar.Size. Both are simple property accesses
            // backed by fields, so the call is safe.
            method.Invoke(instance, [null, EventArgs.Empty]);
        }
        finally
        {
            ((IDisposable)instance).Dispose();
        }
    }

    [WinFormsFact]
    public void DateTimeUI_OnDateSelected_SetsValueAndClosesDropDown()
    {
        DateTimeEditor editor = new();
        Mock<IWindowsFormsEditorService> mockService = new(MockBehavior.Strict);
        Mock<IServiceProvider> provider = new(MockBehavior.Strict);
        provider.Setup(p => p.GetService(typeof(IWindowsFormsEditorService))).Returns(mockService.Object);
        mockService
            .Setup(e => e.CloseDropDown())
            .Verifiable();
        DateTime input = new(2024, 6, 15);
        DateTime selected = new(2024, 6, 16);
        Control dropdown = null;
        mockService
            .Setup(e => e.DropDownControl(It.IsAny<Control>()))
            .Callback<Control>(control =>
            {
                dropdown = control;
                MonthCalendar calendar = control.TestAccessor.Dynamic._monthCalendar;
                Assert.Equal(input, calendar.SelectionStart);
                calendar.SetDate(selected);
                calendar.TestAccessor.Dynamic.OnDateSelected(new DateRangeEventArgs(selected, selected));
                Assert.Equal(selected, control.GetType().GetProperty("Value")!.GetValue(control));
            });

        Assert.Equal(selected, editor.EditValue(null, provider.Object, input));
        mockService.Verify(e => e.CloseDropDown(), Times.Once());
        mockService.Verify(e => e.DropDownControl(It.IsAny<Control>()), Times.Once());
        Assert.True(dropdown.IsDisposed);
    }

    [Fact]
    public void DateTimeUI_MonthCalKeyDown_Enter_CallsOnDateSelected()
    {
        Type type = GetDateTimeUIType();
        Mock<IWindowsFormsEditorService> mockService = new(MockBehavior.Strict);
        mockService
            .Setup(e => e.CloseDropDown())
            .Verifiable();
        object instance = Activator.CreateInstance(type, mockService.Object, (object)null);

        try
        {
            MethodInfo method = type.GetMethod("MonthCalKeyDown",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                [typeof(object), typeof(KeyEventArgs)],
                null)!;
            KeyEventArgs args = new(Keys.Enter);
            method.Invoke(instance, [null, args]);

            // OnDateSelected was called, which closes the drop-down.
            mockService.Verify(e => e.CloseDropDown(), Times.Once());
        }
        finally
        {
            ((IDisposable)instance).Dispose();
        }
    }

    [Theory]
    [InlineData(Keys.A)]
    [InlineData(Keys.Space)]
    [InlineData(Keys.Left)]
    [InlineData(Keys.F2)]
    public void DateTimeUI_MonthCalKeyDown_NonEnter_DoesNotCallOnDateSelected(Keys key)
    {
        Type type = GetDateTimeUIType();
        // Use a strict mock - if CloseDropDown is called for a non-Enter key, the
        // strict mock will throw, indicating the switch is broken.
        Mock<IWindowsFormsEditorService> mockService = new(MockBehavior.Strict);
        object instance = Activator.CreateInstance(type, mockService.Object, (object)null);

        try
        {
            MethodInfo method = type.GetMethod("MonthCalKeyDown",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                [typeof(object), typeof(KeyEventArgs)],
                null)!;
            KeyEventArgs args = new(key);
            method.Invoke(instance, [null, args]);
        }
        finally
        {
            ((IDisposable)instance).Dispose();
        }
    }

    [Fact]
    public void DateTimeUI_Dispose_NullsEditorServiceAndValue()
    {
        Type type = GetDateTimeUIType();
        Mock<IWindowsFormsEditorService> mockService = new(MockBehavior.Loose);
        DateTime input = new(2024, 6, 15);
        object instance = Activator.CreateInstance(type, mockService.Object, (object)input);

        // Dispose via the IDisposable interface.
        ((IDisposable)instance).Dispose();

        // After Dispose, Value should be null.
        PropertyInfo valueProperty = type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)!;
        Assert.Null(valueProperty.GetValue(instance));
    }

    #endregion

    #region Helper Methods

    private static Type GetDateTimeUIType()
    {
        Type type = typeof(DateTimeEditor).GetNestedType("DateTimeUI", BindingFlags.NonPublic);
        Assert.NotNull(type);
        return type!;
    }

    private static Type GetDateTimeMonthCalendarType()
    {
        Type dateTimeUIType = GetDateTimeUIType();
        Type type = dateTimeUIType.GetNestedType("DateTimeMonthCalendar", BindingFlags.NonPublic);
        Assert.NotNull(type);
        return type!;
    }

    /// <summary>
    /// Creates a real <c>DateTimeMonthCalendar</c> instance using the public parameterless
    /// constructor (inherited from <see cref="MonthCalendar"/>). Using a real instance
    /// ensures the Control base constructor runs, initializing the private <c>_window</c>
    /// field that the base <c>IsInputKey</c> implementation reads.
    /// </summary>
    private static MonthCalendar CreateDateTimeMonthCalendar()
    {
        Type type = GetDateTimeMonthCalendarType();
        return (MonthCalendar)Activator.CreateInstance(type)!;
    }

    #endregion
}
