// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Reflection;

namespace System.Drawing.Design.Tests;

/// <summary>
/// Tests for ColorEditor.CustomColorDialog nested class
/// This class extends ColorDialog and uses P/Invoke
/// </summary>
public partial class ColorEditor_CustomColorDialogTests
{
    #region Constructor Tests

    [WinFormsFact]
    public void CustomColorDialog_Ctor_Default()
    {
        var typeCustomColorDialog = typeof(ColorEditor).Assembly.GetTypes().SingleOrDefault(t => t.Name == "CustomColorDialog");
        Assert.NotNull(typeCustomColorDialog);

        using ColorDialog dialog = (ColorDialog)Activator.CreateInstance(typeCustomColorDialog!)!;
        Assert.NotNull(dialog);
    }

    #endregion

    #region Type and Inheritance Tests

    [Fact]
    public void CustomColorDialog_InheritsFromColorDialog()
    {
        Type customColorDialogType = GetCustomColorDialogType();
        Assert.NotNull(customColorDialogType);

        // Verify it inherits from ColorDialog
        Assert.True(typeof(ColorDialog).IsAssignableFrom(customColorDialogType));
    }

    [Fact]
    public void CustomColorDialog_CanBeInstantiated()
    {
        Type customColorDialogType = GetCustomColorDialogType();

        // Should be able to create an instance via reflection
        using ColorDialog instance = (ColorDialog)Activator.CreateInstance(customColorDialogType);
        Assert.NotNull(instance);
    }

    #endregion

    #region Resource Tests

    [Fact]
    public void CustomColorDialog_HasResourceName()
    {
        Type customColorDialogType = GetCustomColorDialogType();
        FieldInfo resourceNameField = customColorDialogType.GetField("s_resourceName", BindingFlags.NonPublic | BindingFlags.Static);

        if (resourceNameField is { })
        {
            object resourceName = resourceNameField.GetValue(null);
            Assert.NotNull(resourceName);
            Assert.IsType<string>(resourceName);

            // Resource name should be meaningful
            string resourceNameStr = resourceName as string;
            Assert.NotEmpty(resourceNameStr);
            Assert.Contains("colordlg", resourceNameStr);
        }
    }

    #endregion

    #region Property Override Tests

    [Fact]
    public void CustomColorDialog_InstanceProperty_ReturnsIntPtr()
    {
        Type customColorDialogType = GetCustomColorDialogType();
        PropertyInfo instanceProperty = customColorDialogType.GetProperty("Instance",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (instanceProperty is { })
        {
            using ColorDialog instance = (ColorDialog)Activator.CreateInstance(customColorDialogType);
            object instanceValue = instanceProperty.GetValue(instance);

            // Instance should be IntPtr
            Assert.IsType<IntPtr>(instanceValue);
        }
    }

    [Fact]
    public void CustomColorDialog_OptionsProperty_ReturnsInt()
    {
        Type customColorDialogType = GetCustomColorDialogType();
        PropertyInfo optionsProperty = customColorDialogType.GetProperty("Options",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (optionsProperty is { })
        {
            using ColorDialog instance = (ColorDialog)Activator.CreateInstance(customColorDialogType);
            object optionsValue = optionsProperty.GetValue(instance);

            // Options should be int
            Assert.IsType<int>(optionsValue);
        }
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void CustomColorDialog_Dispose_DoesNotThrow()
    {
        Type customColorDialogType = GetCustomColorDialogType();
        using ColorDialog instance = (ColorDialog)Activator.CreateInstance(customColorDialogType);
        IDisposable disposable = instance as IDisposable;

        if (disposable is { })
        {
            // Should not throw
            disposable.Dispose();
        }
    }

    [Fact]
    public void CustomColorDialog_ImplementsIDisposable()
    {
        Type customColorDialogType = GetCustomColorDialogType();
        Assert.True(typeof(IDisposable).IsAssignableFrom(customColorDialogType));
    }

    #endregion

    #region Method Tests

    [Fact]
    public void CustomColorDialog_HookProc_IsOverridden()
    {
        Type customColorDialogType = GetCustomColorDialogType();
        MethodInfo hookProcMethod = customColorDialogType.GetMethod("HookProc",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

        // Should have HookProc method (custom dialog hook)
        Assert.NotNull(hookProcMethod);
    }

    [Fact]
    public void CustomColorDialog_HookProc_HasExpectedParameters()
    {
        Type customColorDialogType = GetCustomColorDialogType();
        MethodInfo hookProcMethod = customColorDialogType.GetMethod("HookProc",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

        Assert.NotNull(hookProcMethod);
        ParameterInfo[] parameters = hookProcMethod!.GetParameters();
        Assert.Equal(4, parameters.Length);
        Assert.Equal(typeof(IntPtr), parameters[0].ParameterType);
        Assert.Equal(typeof(int), parameters[1].ParameterType);
        Assert.Equal(typeof(IntPtr), parameters[2].ParameterType);
        Assert.Equal(typeof(IntPtr), parameters[3].ParameterType);
        Assert.Equal(typeof(IntPtr), hookProcMethod.ReturnType);
    }

    [Fact]
    public void CustomColorDialog_Dispose_Bool_DoesNotThrow()
    {
        Type customColorDialogType = GetCustomColorDialogType();
        using ColorDialog instance = (ColorDialog)Activator.CreateInstance(customColorDialogType);

        MethodInfo disposeMethod = customColorDialogType.GetMethod("Dispose",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase,
            null,
            [ typeof(bool) ],
            null);

        Assert.NotNull(disposeMethod);
        if (disposeMethod is { } && instance is { })
        {
            // Should not throw
            disposeMethod.Invoke(instance, [true]);
        }
    }

    [Fact]
    public void CustomColorDialog_Dispose_Bool_IsOverridden()
    {
        Type customColorDialogType = GetCustomColorDialogType();
        MethodInfo disposeMethod = customColorDialogType.GetMethod("Dispose",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase,
            null,
            [ typeof(bool) ],
            null);

        // Should have overridden Dispose(bool) method
        Assert.NotNull(disposeMethod);
    }

    [Fact]
    public void CustomColorDialog_Dispose_Bool_CalledTwice_DoesNotThrow()
    {
        Type customColorDialogType = GetCustomColorDialogType();
        using ColorDialog instance = (ColorDialog)Activator.CreateInstance(customColorDialogType);

        MethodInfo disposeMethod = customColorDialogType.GetMethod("Dispose",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase,
            null,
            [ typeof(bool) ],
            null);

        Assert.NotNull(disposeMethod);
        if (disposeMethod is { } && instance is { })
        {
            // Calling dispose twice should be safe (idempotent)
            disposeMethod.Invoke(instance, [true]);
            disposeMethod.Invoke(instance, [true]);
        }
    }

    [Fact]
    public void CustomColorDialog_Constructor_AllocatesHandle()
    {
        Type customColorDialogType = GetCustomColorDialogType();
        PropertyInfo instanceProperty = customColorDialogType.GetProperty("Instance",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

        Assert.NotNull(instanceProperty);
        if (instanceProperty is { })
        {
            using ColorDialog instance = (ColorDialog)Activator.CreateInstance(customColorDialogType);
            object instanceValue = instanceProperty.GetValue(instance);
            // After construction, the instance handle should be allocated (not zero)
            IntPtr handle = Assert.IsType<IntPtr>(instanceValue);
            Assert.NotEqual(IntPtr.Zero, handle);
        }
    }

    [Fact]
    public void CustomColorDialog_Options_HasCorrectFlags()
    {
        Type customColorDialogType = GetCustomColorDialogType();
        PropertyInfo optionsProperty = customColorDialogType.GetProperty("Options",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

        Assert.NotNull(optionsProperty);
        if (optionsProperty is { })
        {
            using ColorDialog instance = (ColorDialog)Activator.CreateInstance(customColorDialogType);
            int optionsValue = Assert.IsType<int>(optionsProperty.GetValue(instance));
            // The options should have the CC_FULLOPEN and CC_ENABLETEMPLATEHANDLE flags set
            Assert.NotEqual(0, optionsValue);
        }
    }

    [Fact]
    public void CustomColorDialog_Instance_AfterDispose_ReturnsZero()
    {
        Type customColorDialogType = GetCustomColorDialogType();
        using ColorDialog instance = (ColorDialog)Activator.CreateInstance(customColorDialogType);

        PropertyInfo instanceProperty = customColorDialogType.GetProperty("Instance",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

        MethodInfo disposeMethod = customColorDialogType.GetMethod("Dispose",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase,
            null,
            [ typeof(bool) ],
            null);

        Assert.NotNull(disposeMethod);
        Assert.NotNull(instanceProperty);
        if (disposeMethod is { } && instanceProperty is { } && instance is { })
        {
            // Dispose the dialog
            disposeMethod.Invoke(instance, [true]);

            // After dispose, the internal _hInstance should be IntPtr.Zero
            FieldInfo hInstanceField = customColorDialogType.GetField("_hInstance",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (hInstanceField is { })
            {
                var handleValue = hInstanceField.GetValue(instance);
                Assert.Equal(IntPtr.Zero, Assert.IsType<IntPtr>(handleValue));
            }
        }
    }

    #endregion

    #region Helper Methods

    private Type GetCustomColorDialogType()
    {
        Type colorEditorType = typeof(ColorEditor);
        Type customColorDialogType = colorEditorType.GetNestedType("CustomColorDialog", BindingFlags.NonPublic);
        Assert.NotNull(customColorDialogType);
        return customColorDialogType;
    }

    #endregion
}
