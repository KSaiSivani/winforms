// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Reflection;
using Moq;

namespace System.Windows.Forms.Design.Tests;

/// <summary>
///  Tests for <see cref="StringCollectionEditor"/>.
/// </summary>
/// <remarks>
///  <para>The production type is small — it overrides <c>CreateCollectionForm</c> to
///  return a <c>StringCollectionForm</c>, and overrides <c>HelpTopic</c>. The
///  tests below cover both members, the constructor, and the type hierarchy
///  (so the base collection-editor plumbing stays implicit).</para>
/// </remarks>
public class StringCollectionEditorTests
{
    [Fact]
    public void StringCollectionEditor_Ctor_SetsCollectionType()
    {
        StringCollectionEditor editor = new(typeof(string[]));

        // CollectionType is the protected property from the base. The value
        // passed to the constructor must round-trip.
        PropertyInfo collectionType = typeof(CollectionEditor)
            .GetProperty("CollectionType", BindingFlags.NonPublic | BindingFlags.Instance)!;
        collectionType.GetValue(editor).Should().Be(typeof(string[]));
    }

    [Fact]
    public void StringCollectionEditor_Ctor_NullType_Throws()
    {
        // CollectionEditor ctor throws ArgumentNullException on null type.
        ((Action)(() => new StringCollectionEditor(null!))).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void StringCollectionEditor_CreateCollectionForm_ReturnsStringCollectionForm()
    {
        // The override must return a private StringCollectionForm. The form
        // is private nested, so we resolve it via reflection and compare types.
        StringCollectionEditor editor = new(typeof(string[]));
        MethodInfo createCollectionForm = typeof(StringCollectionEditor)
            .GetMethod("CreateCollectionForm", BindingFlags.NonPublic | BindingFlags.Instance)!;

        using Form form = (Form)createCollectionForm.Invoke(editor, null)!;
        form.Should().NotBeNull();

        Type formType = form.GetType();
        formType.Name.Should().Be("StringCollectionForm");
        formType.IsNestedPrivate.Should().BeTrue();
        formType.DeclaringType.Should().Be(typeof(StringCollectionEditor));
    }

    [Fact]
    public void StringCollectionEditor_CreateCollectionForm_ReturnsFreshInstance()
    {
        // Two calls must return two different instances.
        StringCollectionEditor editor = new(typeof(string[]));
        MethodInfo createCollectionForm = typeof(StringCollectionEditor)
            .GetMethod("CreateCollectionForm", BindingFlags.NonPublic | BindingFlags.Instance)!;

        using Form form1 = (Form)createCollectionForm.Invoke(editor, null)!;
        using Form form2 = (Form)createCollectionForm.Invoke(editor, null)!;

        form1.Should().NotBeSameAs(form2);
    }

    [Fact]
    public void StringCollectionEditor_HelpTopic_ReturnsExpectedValue()
    {
        // HelpTopic is a protected virtual property. The override returns
        // a specific keyword recognized by the design-time help system.
        StringCollectionEditor editor = new(typeof(string[]));
        MethodInfo getHelpTopic = typeof(StringCollectionEditor)
            .GetMethod("get_HelpTopic", BindingFlags.NonPublic | BindingFlags.Instance)!;

        string helpTopic = (string)getHelpTopic.Invoke(editor, null)!;
        helpTopic.Should().Be("net.ComponentModel.StringCollectionEditor");
    }

    [Fact]
    public void StringCollectionEditor_CollectionItemType_ForCollectionWithStringIndexer_ResolvesToString()
    {
        // CollectionItemType is the type of each item in the collection. The base
        // CollectionEditor.CreateCollectionItemType resolves this by looking for an
        // `Item` or `Items` property on the collection type. For raw arrays like
        // `string[]` this returns `object` (no Item property), so we exercise the
        // path with `List<string>` instead, which exposes an `Item` indexer whose
        // type is `string`.
        StringCollectionEditor editor = new(typeof(List<string>));
        MethodInfo getCollectionItemType = typeof(CollectionEditor)
            .GetMethod("get_CollectionItemType", BindingFlags.NonPublic | BindingFlags.Instance)!;

        Type itemType = (Type)getCollectionItemType.Invoke(editor, null)!;
        itemType.Should().Be(typeof(string));
    }

    [Fact]
    public void StringCollectionEditor_NewItemTypes_ForCollectionWithStringIndexer_ContainsString()
    {
        // NewItemTypes defaults to [CollectionItemType] in the base implementation.
        // For `List<string>`, that resolves to `string` (see the comment in
        // StringCollectionEditor_CollectionItemType_ForCollectionWithStringIndexer_ResolvesToString).
        StringCollectionEditor editor = new(typeof(List<string>));
        MethodInfo getNewItemTypes = typeof(CollectionEditor)
            .GetMethod("get_NewItemTypes", BindingFlags.NonPublic | BindingFlags.Instance)!;

        Type[] newItemTypes = (Type[])getNewItemTypes.Invoke(editor, null)!;
        newItemTypes.Should().Contain(typeof(string));
    }

    [Fact]
    public void StringCollectionEditor_GetEditStyle_ReturnsModal()
    {
        // The base UITypeEditor.GetEditStyle returns Modal by default.
        // CollectionEditor is a UITypeEditor that does not override this,
        // so the value flows through. Document the contract explicitly.
        StringCollectionEditor editor = new(typeof(string[]));
        UITypeEditorEditStyle style = ((UITypeEditor)editor).GetEditStyle(null);
        style.Should().Be(UITypeEditorEditStyle.Modal);
    }

    [Fact]
    public void StringCollectionEditor_EditValue_WithoutEditorService_ReturnsValue()
    {
        // When no IWindowsFormsEditorService is available, the base returns
        // the value unchanged.
        StringCollectionEditor editor = new(typeof(string[]));
        IServiceProvider provider = new Mock<IServiceProvider>().Object;
        object? value = new object();

        object? result = editor.EditValue(null, provider, value);

        result.Should().BeSameAs(value);
    }

    [Fact]
    public void StringCollectionEditor_EditValue_NullProvider_ReturnsValue()
    {
        // When the provider is null, the base returns the value unchanged.
        StringCollectionEditor editor = new(typeof(string[]));
        object? value = new object();

        // EditValue's `provider` parameter is non-nullable, so we suppress the
        // nullable warning with `null!`. This is intentional: we are proving
        // the base implementation handles a null provider gracefully.
        object? result = editor.EditValue(null, null!, value: value);

        result.Should().BeSameAs(value);
    }
}
